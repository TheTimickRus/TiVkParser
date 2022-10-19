// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable RedundantNullableFlowAttribute
// ReSharper disable ClassNeverInstantiated.Global

using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using Ardalis.GuardClauses;
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
using VkNet.Model;
using VkNet.Model.Attachments;

namespace TiVkParser.Commands.Keywords;

public class KeywordsCommand : Command<KeywordsCommandSettings>
{
    private KeywordsCommandSettings? _settings;
    private MainConfiguration? _conf;
    private VkServiceLib? _vkServiceLib;
    private readonly List<OutCommentModel> _outputData = new();

    public override ValidationResult Validate([NotNull] CommandContext context, [NotNull] KeywordsCommandSettings commandSettings)
    {
        try
        {
            if (commandSettings.IsExtractConfigFile)
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
            
            if (commandSettings.ConfigFile is  null || File.Exists(commandSettings.ConfigFile) is false)
                throw new Exception("Не найден файл конфигурации!");
            
            var mainConf = Toml.ToModel<MainConfiguration>(File.ReadAllText(commandSettings.ConfigFile));
            if (mainConf.AccessToken is null or "")
                throw new Exception("Не указан AccessToken!");

            _settings = commandSettings;
            _conf = mainConf;
            
            return ValidationResult.Success();
        }
        catch (Exception ex)
        {
            return ValidationResult.Error(ex.Message);
        }
    }
    
    public override int Execute([NotNull] CommandContext context, [NotNull] KeywordsCommandSettings settings)
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
                new PercentageColumn
                {
                    Style = new Style(foreground: Constants.Colors.MainColor), 
                    CompletedStyle = new Style(Constants.Colors.SuccessColor)
                },
                new SpinnerColumn(Spinner.Known.BouncingBar)
                {
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
    
    private Task Work(ProgressContext ctx)
    {
        Guard.Against.Null(_settings);
        Guard.Against.Null(_conf);
        Guard.Against.Null(_conf.Keywords);
        Guard.Against.Null(_conf.Keywords.GroupIds);
        Guard.Against.Null(_conf.Keywords.Keywords);
        Guard.Against.Null(_vkServiceLib);

        var groupsProgressTask = ctx
            .AddTask($"[bold {Constants.Colors.SecondColor}]Получение групп[/]")
            .IsIndeterminate();
        
        var groups = _vkServiceLib
            .FetchGroupsInfo(_conf.Keywords.GroupIds.Select(groupId => groupId.ToString()))
            .Where(group => group.IsClosed is GroupPublicity.Public)
            .ToList();

        if (groups.Count is 0)
        {
            groupsProgressTask.StopTask();
            return Task.CompletedTask;
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
                postsProgressTask.Description($"[bold {Constants.Colors.SecondColor}]Пост:[/] [underline]{post.Id} ({post.Date})[/]");

                foreach (var keyword in _conf.Keywords.Keywords)
                {
                    if (post.Text is not null or "" && Regex.IsMatch(post.Text.ToLower(), $"\\b{keyword}\\b"))
                    {
                        _outputData.Add(MappingToCommentModel(group, post, null));
                        SerilogLib.Info($"Post = ({post.FromId},{group.Id},{post.Id})");
                    }
                    
                    if (_settings.IsComments is false || post.Comments.Count <= 0) 
                        continue;
                    
                    var comments = _vkServiceLib.FetchComments(group.Id, post.Id!.Value);
                    foreach (var comment in comments)
                    {
                        var commentsThread = new List<Comment> { comment };
                        if (comment.Thread.Count > 0)
                        {
                            Thread.Sleep(333);
                            
                            commentsThread.AddRange(
                                _vkServiceLib.FetchComments(group.Id, post.Id.Value, comment.Id)
                            ); 
                        }
                        
                        foreach (
                            var cThread in from cThread in commentsThread 
                            where cThread.Text is not null or ""
                            let math = _conf.Keywords.Keywords.FirstOrDefault(kw => Regex.IsMatch(cThread.Text, $"\\b{kw}\\b")) 
                            where math != null 
                            select cThread
                        )
                        {
                            _outputData.Add(MappingToCommentModel(group, post, cThread));
                            SerilogLib.Info($"Comment = ({post.FromId},{group.Id},{post.Id},{comment.Id})");
                        }
                    }
                    
                    Thread.Sleep(333);
                }
                
                postsProgressTask.Increment(1);
            }
            
            groupsProgressTask.Increment(1);
        }
        
        return Task.CompletedTask;
    }
    
    private static OutCommentModel MappingToCommentModel(VkNet.Model.Group group, Post post, Comment? comment)
    {
        return new OutCommentModel(
            UserId:      post.FromId.ToString() ?? "",
            GroupId:     group.Id.ToString(),
            PostId:      post.Id.ToString() ?? "",
            PostText:    post.Text,
            CommentId:   comment?.Id.ToString() ?? "",
            CommentText: comment?.Text ?? "",
            UrlPost:     $"https://vk.com/{group.ScreenName}?w=wall-{group.Id}_{post.Id}",
            UrlComment:  $"https://vk.com/{group.ScreenName}?w=wall-{group.Id}_{post.Id}_r{comment?.Id}"
        );
    }
}