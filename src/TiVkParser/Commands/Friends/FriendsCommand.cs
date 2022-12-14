// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable RedundantNullableFlowAttribute
// ReSharper disable ClassNeverInstantiated.Global

using System.Diagnostics;
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
        _vkServiceLib = new VkServiceLib(_conf.AccessToken!, settings.ApiLimit);
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
                new SpinnerColumn(Spinner.Known.SimpleDotsScrolling)
                {
                    Style = new Style(foreground: Constants.Colors.SecondColor),
                    CompletedStyle = new Style(Constants.Colors.SuccessColor)
                }
            )
            .Start(Work);
        
        /* Сохранение данных */
        AnsiConsoleLib.ShowHeader();
        AnsiConsole.MarkupLine("Сохранение данных...");
        ExportData.ToExcel(new ExportsDataModel { Friends = _friends });
        AnsiConsole.MarkupLine("Данные успешно сохранены!\n");

        /* Запуск сохраненного файла */
        var prompt = new SelectionPrompt<string>()
            .HighlightStyle(new Style(foreground: Constants.Colors.SecondColor))
            .Title("Запустить сохраненный файл?")
            .AddChoices("Да", "Нет");
        
        if (AnsiConsole.Prompt(prompt) == "Да")
            Process.Start("cmd", $"/c {Path.Combine(Environment.CurrentDirectory, ExportData.FileName)}");

        /* Завершение работы */
        AnsiConsoleLib.ShowRule(
            "Работа программы завершена! Нажмите любою кнопку, чтобы выйти", 
            Justify.Center, 
            Constants.Colors.SuccessColor
        );
        
        AnsiConsole.Console.Input.ReadKey(true);
        return 0;
    }

    private void Work(ProgressContext ctx)
    {
        Guard.Against.Null(_conf);
        Guard.Against.Null(_conf.Friends);
        Guard.Against.Null(_vkServiceLib);

        var userId = _conf.Friends.UserId;
        
        var mainProgressTask = ctx
            .AddTask($"Получение информации о пользователе (UserID = {userId})...")
            .IsIndeterminate();
        
        mainProgressTask.StartTask();

        _user = _vkServiceLib.FetchUserById(userId);
        Guard.Against.Null(_user);
        mainProgressTask.Description($"[bold]Пользователь:[/] [underline]{_user.FirstName} {_user.LastName}[/]");
        
        _friends = _vkServiceLib.FetchFriendsFromUser(userId).Select(user => user.Id);

        mainProgressTask.StopTask();
    }
}