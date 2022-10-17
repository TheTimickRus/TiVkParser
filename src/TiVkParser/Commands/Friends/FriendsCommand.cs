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
using TiVkParser.Models.ConfigurationModels;
using TiVkParser.Services;
using Tomlyn;
using VkNet.Model;

namespace TiVkParser.Commands.Friends;

public class FriendsCommand : Command<FriendsSettings>
{
    private MainConfiguration? _conf;
    private VkServiceLib? _vkServiceLib;

    private User? _user;
    private IEnumerable<long> _friends = new List<long>();

    public override ValidationResult Validate([NotNull] CommandContext context, [NotNull] FriendsSettings settings)
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
                throw new Exception("Пожалуйста, перезапустите программу...");
            }
            
            if (settings.ConfigFile is  null || File.Exists(settings.ConfigFile) is false)
                throw new Exception("Не найден файл конфигурации!");
            
            var mainConf = Toml.ToModel<MainConfiguration>(File.ReadAllText(settings.ConfigFile));
            if (mainConf.AccessToken is null or "")
                throw new Exception("Не указан AccessToken!");
            
            _conf = mainConf;
            
            return ValidationResult.Success();
        }
        catch (Exception ex)
        {
            return ValidationResult.Error(ex.Message);
        }
    }

    public override int Execute([NotNull] CommandContext context, [NotNull] FriendsSettings settings)
    {
        /* Подготовка */
        SerilogLib.IsLogging = settings.IsLogging;
        Guard.Against.Null(_conf);
        _vkServiceLib = new VkServiceLib(_conf.AccessToken!, (ulong)settings.TotalItemsForApi);
        AnsiConsoleLib.ShowHeader();
        
        /* Основная работа */
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
                new SpinnerColumn
                {
                    Spinner = Spinner.Known.Hamburger, 
                    Style = new Style(foreground: Constants.Colors.MainColor),
                    CompletedStyle = new Style(Constants.Colors.SuccessColor)
                }
            )
            .Start(Work);
        
        /* Сохранение данных */
        SaveDataService.FriendsToExcel("TiVkParser_Friends.xlsx", _friends);
        
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

    private Task Work(ProgressContext ctx)
    {
        Guard.Against.Null(_conf);
        Guard.Against.Null(_conf.Friends);
        Guard.Against.Null(_vkServiceLib);

        var userId = _conf.Friends.UserId;
        
        var tsk = ctx.AddTask($"Получение информации о пользователе (UserID = {userId})...");
        tsk.IsIndeterminate = true;
        tsk.StartTask();

        _user = _vkServiceLib.FetchUserById(userId);
        Guard.Against.Null(_user);
        tsk.Description($"[bold]Пользователь:[/] [underline]{_user.FirstName} {_user.LastName}[/]");
        
        _friends = _vkServiceLib.FetchFriendsFromUser(userId);

        tsk.StopTask();
        return Task.CompletedTask;
    }
}