// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable RedundantNullableFlowAttribute
// ReSharper disable ClassNeverInstantiated.Global

using System.Diagnostics.CodeAnalysis;
using Ardalis.GuardClauses;
using MoreLinq;
using MoreLinq.Experimental;
using Spectre.Console;
using Spectre.Console.Cli;
using TiVkParser.Core;
using TiVkParser.Exports;
using TiVkParser.Helpers;
using TiVkParser.Logging;
using TiVkParser.Models.Core.Filters;
using TiVkParser.Models.Exports;
using TiVkParser.Models.Main.ConfigurationModels;
using TiVkParser.Services;
using Tomlyn;
using VkNet.Enums;

namespace TiVkParser.Commands.SearchByKeywords;

public class SearchByKeywordsCommand : Command<SearchByKeywordsSettings>
{
    private SearchByKeywordsSettings? _settings;
    private MainConfiguration? _conf;
    private VkServiceLib? _vkServiceLib;
    private List<OutCommentModel> _outputData = new();

    public override ValidationResult Validate([NotNull] CommandContext context, [NotNull] SearchByKeywordsSettings settings)
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

            _settings = settings;
            _conf = mainConf;
            
            return ValidationResult.Success();
        }
        catch (Exception ex)
        {
            return ValidationResult.Error(ex.Message);
        }
    }

    public override int Execute([NotNull] CommandContext context, [NotNull] SearchByKeywordsSettings settings)
    {
        /* Подготовка */
        SerilogLib.IsLogging = settings.IsLogging;
        Guard.Against.Null(_conf);
        _vkServiceLib = new VkServiceLib(_conf.AccessToken!, settings.TotalItemsForApi);
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
        ExportData.ToExcel(new ExportsDataModel { SearchByKeywords = _outputData });
        
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
    
    private void Work(ProgressContext ctx)
    {
        Guard.Against.Null(_settings);
        Guard.Against.Null(_conf);
        Guard.Against.Null(_conf.SearchByKeywords);
        Guard.Against.Null(_conf.SearchByKeywords.GroupIds);
        Guard.Against.Null(_conf.SearchByKeywords.Keywords);
        Guard.Against.Null(_vkServiceLib);

        var groupsProgressTask = ctx
            .AddTask($"[bold {Constants.Colors.SecondColor}]Получение групп[/]")
            .IsIndeterminate();
        
        var groups = _vkServiceLib
            .FetchGroupsInfo(_conf.SearchByKeywords.GroupIds.Select(groupId => groupId.ToString()))
            .Where(group => group.IsClosed is GroupPublicity.Public)
            .ToList();

        if (groups.Count is 0)
        {
            groupsProgressTask.StopTask();
            return;
        }

        groupsProgressTask
            .IsIndeterminate(false)
            .MaxValue(groups.Count);

        foreach (var group in groups)
        {
            groupsProgressTask.Description = $"[bold {Constants.Colors.SecondColor}] Группа:[/] [underline]{group.Name} ({group.Id})[/]";
            
            var postsProgressTask = ctx
                .AddTask($"[bold {Constants.Colors.SecondColor}]Получение постов[/]")
                .IsIndeterminate();
            
            var posts = _vkServiceLib
                .FetchPostsFromGroup(group.Id, new FetchPostsFilter(null, null))
                .ToList();
            
            if (posts.Count is 0)
            {
                postsProgressTask.StopTask();
                continue;
            }
            
            postsProgressTask
                .IsIndeterminate(false)
                .MaxValue(posts.Count);

            foreach (var post in posts)
            {
                
            }
        }
    }
}