// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable RedundantNullableFlowAttribute
// ReSharper disable ClassNeverInstantiated.Global

using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using Ardalis.GuardClauses;
using Spectre.Console;
using Spectre.Console.Cli;
using TiVkParser.Helpers;
using TiVkParser.Models.ConfModels;
using TiVkParser.Services;
using Tomlyn;

namespace TiVkParser.Commands;

public class FriendsCommand : Command<FriendsCommand.Settings>
{
    public class Settings : CommandSettings
    {
        [Description("Путь до файла конфигурации (Default = TiVkParser.toml)")]
        [CommandOption("-c|--cfg")]
        [DefaultValue("TiVkParser.toml")]
        public string? ConfigFile { get; init; }
    }

    private FriendsConfiguration? _conf;
    private string _accessToken = "";
    
    public override ValidationResult Validate([NotNull] CommandContext context, [NotNull] Settings settings)
    {
        Guard.Against.Null(settings.ConfigFile);
        
        if (!File.Exists(settings.ConfigFile))
        {
            AnyHelpers.ExtractResourceToFile("TiVkParser", "Assets", "TiVkParser.toml", Environment.CurrentDirectory);
            throw new Exception("Не найден файл конфигурации! Стандартный файл конфигурации создан - TiVkParser.toml. Заполните его и перезапустите программу...");
        }
        
        var mainConf = Toml.ToModel<MainConfiguration>(File.ReadAllText(settings.ConfigFile));
        _accessToken = mainConf.AccessToken ?? "";
        _conf = mainConf.Friends;
        
        return ValidationResult.Success();
    }

    public override int Execute([NotNull] CommandContext context, [NotNull] Settings settings)
    {
        Guard.Against.Null(_conf);
        
        SerilogLib.IsLogging = true;
        Console.Title = Constants.Titles.VeryShortTitle;
        
        AnsiConsoleLib.ShowFiglet(Constants.Titles.VeryShortTitle, Justify.Center, Constants.Colors.MainColor);
        AnsiConsoleLib.ShowRule(Constants.Titles.FullTitle, Justify.Right, Constants.Colors.MainColor);
        
        var vkService = new VkServiceLib(_accessToken, 1000);
        
        AnsiConsole.Progress()
            .HideCompleted(true)
            .Columns(
                new TaskDescriptionColumn(), 
                new ProgressBarColumn
                {
                    IndeterminateStyle = new Style(foreground: Constants.Colors.MainColor),
                    RemainingStyle = new Style(foreground: Constants.Colors.MainColor),
                    CompletedStyle = new Style(Constants.Colors.SuccessColor)
                }, 
                new PercentageColumn
                {
                    Style = new Style(foreground: Constants.Colors.MainColor),
                    CompletedStyle = new Style(Constants.Colors.SuccessColor)
                }, 
                new RemainingTimeColumn
                {
                    Style = new Style(Constants.Colors.MainColor)
                }, 
                new SpinnerColumn
                {
                    Spinner = Spinner.Known.Hamburger, 
                    Style = new Style(foreground: Constants.Colors.MainColor),
                    CompletedStyle = new Style(Constants.Colors.SuccessColor)
                }
            )
            .Start(ctx =>
            {
                var tsk = ctx.AddTask($"[bold]Пользователь:[/] [underline]{_conf.UserId}[/]");
                tsk.IsIndeterminate = true;
                
                tsk.StartTask();
                
                var friends = vkService.FetchFriendsFromUser(_conf.UserId);
                SaveDataService.FriendsToExcel("TiVkParser.xlsx", friends.ToList());
                
                tsk.StopTask();
                
                return Task.CompletedTask;
            });
        
        return 0;
    }
}