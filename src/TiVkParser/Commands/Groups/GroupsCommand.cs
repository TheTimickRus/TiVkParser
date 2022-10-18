// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable RedundantNullableFlowAttribute
// ReSharper disable ClassNeverInstantiated.Global

using System.Diagnostics.CodeAnalysis;
using Ardalis.GuardClauses;
using Spectre.Console;
using Spectre.Console.Cli;
using TiVkParser.Core;
using TiVkParser.Exports;
using TiVkParser.Helpers;
using TiVkParser.Logging;
using TiVkParser.Models.Exports;
using TiVkParser.Models.Main.ConfigurationModels;
using TiVkParser.Services;
using Tomlyn;

namespace TiVkParser.Commands.Groups;

public class GroupsCommand : Command<GroupsSettings>
{
    private MainConfiguration? _conf;

    private (List<OutLikeModel>, List<OutCommentModel>) _outputData;
    
    public override ValidationResult Validate([NotNull] CommandContext context, [NotNull] GroupsSettings settings)
    {
        try
        {
            Console.Title = Constants.Titles.VeryShortTitle;
            
            if (settings.IsExtractConfigFile)
            {
                AnyHelpers.ExtractResourceToFile("TiVkParser", "Assets", "TiVkParser.toml", Environment.CurrentDirectory);
            
                AnsiConsoleLib.ShowHeader();
                AnsiConsoleLib.ShowRule(
                    "Конфигурационный файл сохранен! Пожалуйста, заполните его и запустите программу заного", 
                    Justify.Center, 
                    Constants.Colors.SuccessColor
                );
                AnsiConsole.Console.Input.ReadKey(true);
                
                // Завершаем работу программы
                Environment.Exit(0);
            }
            
            if (settings.ConfigFile is  null || File.Exists(settings.ConfigFile) is false)
                throw new Exception("Не найден файл конфигурации!");
            
            var mainConf = Toml.ToModel<MainConfiguration>(File.ReadAllText(settings.ConfigFile));
            
            _conf = mainConf;
            
            return ValidationResult.Success();
        }
        catch (Exception ex)
        {
            SerilogLib.Error(ex);
            return ValidationResult.Error(ex.Message);
        }
    }

    public override int Execute([NotNull] CommandContext context, [NotNull] GroupsSettings settings)
    {
        Console.Title = Constants.Titles.VeryShortTitle;
        AnsiConsoleLib.ShowHeader();
        
        SerilogLib.IsLogging = true;

        // Проверки
        Guard.Against.Null(_conf);
        Guard.Against.Null(_conf.AccessToken);
        Guard.Against.Null(_conf.Groups);
        // Проверки
        
        var vkLib = new VkServiceLib(_conf.AccessToken, settings.TotalItemsForApi);

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
                new SpinnerColumn
                {
                    Spinner = Spinner.Known.Hamburger, 
                    Style = new Style(foreground: Constants.Colors.MainColor),
                    CompletedStyle = new Style(Constants.Colors.SuccessColor)
                }
            )
            .Start(ctx =>
            {
                _outputData = _conf.Groups.IsGroupsFromUser switch
                {
                    true => GroupsFromUser.Execute(ctx, settings, _conf.Groups, vkLib),
                    _ => GroupsFromConfig.Execute(ctx, settings, _conf.Groups, vkLib)
                };
            });
        
        /* Сохранение данных */
        ExportData.ToExcel(new ExportsDataModel { Likes = _outputData.Item1, Comments = _outputData.Item2 });
        
        /* Завершение работы */
        AnsiConsoleLib.ShowHeader();
        AnsiConsoleLib.ShowRule(
            "Работа программы завершена! Для завершения нажмите любою кнопку", 
            Justify.Center, 
            Constants.Colors.SuccessColor
        );
        AnsiConsole.Console.Input.ReadKey(true);
        
        return 0;
    }
}