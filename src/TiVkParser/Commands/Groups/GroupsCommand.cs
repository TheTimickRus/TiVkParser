// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable RedundantNullableFlowAttribute
// ReSharper disable ClassNeverInstantiated.Global

using System.Diagnostics.CodeAnalysis;
using Ardalis.GuardClauses;
using Spectre.Console;
using Spectre.Console.Cli;
using TiVkParser.Helpers;
using TiVkParser.Models;
using TiVkParser.Models.ConfModels;
using TiVkParser.Models.SaveDataModels;
using TiVkParser.Services;
using Tomlyn;
using VkNet.Enums;

namespace TiVkParser.Commands.Groups;

public class GroupsCommand : Command<GroupSettings>
{
    private GroupSettings? _settings;
    private GroupsConfiguration? _conf;
    
    private string _accessToken = "";
    
    private readonly List<OutLike> _outLikes = new();
    private readonly List<OutComment> _outComments = new();
    
    public override ValidationResult Validate([NotNull] CommandContext context, [NotNull] GroupSettings settings)
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
            
            _accessToken = mainConf.AccessToken;
            _conf = mainConf.Groups;
            
            return ValidationResult.Success();
        }
        catch (Exception ex)
        {
            return ValidationResult.Error(ex.Message);
        }
    }

    public override int Execute([NotNull] CommandContext context, [NotNull] GroupSettings settings)
    {
        _settings = settings;
        Guard.Against.Null(_conf);
        
        SerilogLib.IsLogging = true;
        Console.Title = Constants.Titles.VeryShortTitle;
        
        AnsiConsoleLib.ShowFiglet(Constants.Titles.VeryShortTitle, Justify.Center, Constants.Colors.MainColor);
        AnsiConsoleLib.ShowRule(Constants.Titles.FullTitle, Justify.Right, Constants.Colors.MainColor);
        
        var vkService = new VkServiceLib(_accessToken, (ulong)settings.LimitFilter);

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
                if (_conf.IsGroupsFromUser)
                    GroupsFromUser(ctx, vkService);
                else
                    GroupFromConfig(ctx, vkService);
                
                return Task.CompletedTask;
            });
        
        SaveDataService.LikesToExcel("TiVkParser.xlsx", _outLikes);
        SaveDataService.CommentsToExcel("TiVkParser.xlsx", _outComments);
        
        return 0;
    }

    /// <summary>
    /// Работа с пользователями из файла конфигурации
    /// </summary>
    /// <param name="ctx"></param>
    /// <param name="vkService"></param>
    private void GroupsFromUser(ProgressContext ctx, VkServiceLib vkService)
    {
        Guard.Against.Null(_settings);
        Guard.Against.Null(_conf);

        var usersProgressTask = ctx
            .AddTask("[bold]Получение пользователей[/][/]")
            .IsIndeterminate();
        
        var users = vkService
            .FetchUsersById(_conf.UserIds.Select(long.Parse))
            .ToList();

        usersProgressTask.IsIndeterminate = false;
        usersProgressTask.MaxValue = users.Count;
        
        // Обработка пользователей
        foreach (var user in users)
        {
            usersProgressTask.Description = $"[bold]Пользователь:[/] [underline]{user.FirstName} {user.LastName} ({user.Id})[/]";
            
            var groupsProgressTask = ctx
                .AddTask("[bold]Получение групп пользователя[/]")
                .IsIndeterminate();
            
            var groups = vkService
                .FetchUserGroups(Convert.ToInt64(user.Id))
                .ToList();
            
            groupsProgressTask.IsIndeterminate = false;
            groupsProgressTask.MaxValue = groups.Count;
            
            // Обработка групп пользователя
            foreach (var group in groups)
            {
                groupsProgressTask.Description = $"[bold]Группа:[/] [underline]{group.Name}[/]";
                
                var postsProgressTask = ctx
                    .AddTask("[bold]Получение постов[/]")
                    .IsIndeterminate();

                var dateParseResult = DateTime.TryParse(_settings.DateFilter, out var dateFilter);
                var posts = vkService
                    .FetchPostsFromGroup(group.Id, new FetchPostsFilterParams(10000, dateParseResult ? dateFilter : null))
                    .Where(post => _settings.IsComments && post.Comments.Count > 0 || _settings.IsLike && post.Likes.Count > 0)
                    .ToList();

                postsProgressTask.IsIndeterminate = false;
                postsProgressTask.MaxValue = posts.Count;
                
                // Обработка постов из группы
                foreach (var post in posts)
                {
                    postsProgressTask.Description = $"[bold]Пост:[/] [underline]{post.Id}[/]";

                    if (_settings.IsLike)
                    {
                        var likes = vkService
                            .FetchLikesFromPost(group.Id, post.Id);
                        foreach (var unused in likes.Where(like => like.Id == user?.Id))
                        {
                            _outLikes.Add(
                                new OutLike(
                                    user?.Id.ToString() ?? "-1", 
                                    group.Id.ToString(), 
                                    post.Id?.ToString() ?? "-1"
                                )
                            );
                            
                            SerilogLib.Info($"{user?.Id.ToString() ?? "-1"},{group.Id.ToString()},{post.Id.ToString()}");
                        }
                        
                        Thread.Sleep(333);
                    }

                    if (_settings.IsComments)
                    {
                        var comments = vkService
                            .FetchCommentsFromPost(group.Id, post.Id);
                        foreach (var comment in comments.Where(comment => comment.FromId == user?.Id))
                        {
                            _outComments.Add(
                                new OutComment(
                                    user?.Id.ToString() ?? "-1",
                                    group.Id.ToString(), 
                                    post.Id.ToString() ?? "-1", 
                                    comment.Id.ToString(), 
                                    comment.Text
                                )
                            );
                            
                            SerilogLib.Info($"{user?.Id.ToString() ?? "-1"},{group.Id.ToString()},{post.Id.ToString()},{comment.Id.ToString()},{comment.Text}");
                        }
                    
                        Thread.Sleep(333); 
                    }
                    
                    postsProgressTask.Increment(1);
                }
                
                groupsProgressTask.Increment(1);
            }
            
            usersProgressTask.Increment(1);
        }
    }
    
    /// <summary>
    /// Работа с группами из файла конфигурации
    /// </summary>
    /// <param name="ctx"></param>
    /// <param name="vkService"></param>
    private void GroupFromConfig(ProgressContext ctx, VkServiceLib vkService)
    {
        Guard.Against.Null(_settings);
        Guard.Against.Null(_conf);
        Guard.Against.Zero(_conf.UserIds.Count);
        Guard.Against.Zero(_conf.GroupIds.Count);
        
        var groupsProgressTask = ctx
            .AddTask($"[bold {Constants.Colors.SecondColor}]Получение групп[/]")
            .IsIndeterminate();
        
        var groups = vkService.FetchGroupsInfo(_conf.GroupIds)
            .Where(group => group.IsClosed is GroupPublicity.Public)
            .ToList();
        
        groupsProgressTask.IsIndeterminate = false;
        groupsProgressTask.MaxValue = groups.Count;
        
        foreach (var group in groups)
        {
            groupsProgressTask.Description = $"[bold {Constants.Colors.SecondColor}] Группа:[/] [underline]{group.Name}[/]";
            
            var postsProgressTask = ctx
                .AddTask($"[bold {Constants.Colors.SecondColor}]Получение постов[/]")
                .IsIndeterminate();
            
            var dateParseResult = DateTime.TryParse(_settings.DateFilter, out var dateFilter);
            var posts = VkServiceLib.FetchPostsFromGroup(group.Id, new FetchPostsFilterParams((long)_settings.LimitFilter, dateParseResult ? dateFilter : null))
                .ToList();
            
            if (posts.Count is 0)
                continue;
            
            postsProgressTask.IsIndeterminate = false;
            postsProgressTask.MaxValue = posts.Count;
            
            foreach (var post in posts)
            {
                postsProgressTask.Description = $"[bold {Constants.Colors.SecondColor}]Пост:[/] [underline]{post.Id}[/]";

                if (_settings.IsLike && post.Likes.Count > 0)
                {
                    var likes = vkService.FetchLikesFromPost(group.Id, post.Id);
                    foreach (var like in likes)
                    {
                        foreach (var userId in _conf.UserIds.Where(userId => like.Id == Convert.ToInt64(userId)))
                        {
                            _outLikes.Add(
                                new OutLike(
                                    userId, 
                                    group.Id.ToString(), 
                                    post.Id?.ToString() ?? "-1"
                                )
                            );
                            
                            SerilogLib.Info($"Like = ({userId},{group.Id.ToString()},{post.Id.ToString()})");
                        }
                    }  
                    
                    //Thread.Sleep(333);
                }

                if (_settings.IsComments && post.Comments.Count > 0)
                {
                    var comments = vkService.FetchCommentsFromPost(group.Id, post.Id);
                    foreach (var comment in comments)
                    {
                        foreach (var userId in _conf.UserIds.Where(userId => comment.FromId == Convert.ToInt64(userId)))
                        {
                            _outComments.Add(
                                new OutComment(
                                    userId, 
                                    group.Id.ToString(), 
                                    post.Id?.ToString() ?? "-1", 
                                    comment.Id.ToString(), 
                                    comment.Text
                                )
                            );
                            
                            SerilogLib.Info($"Comment = ({userId},{group.Id.ToString()},{post.Id.ToString()},{comment.Id.ToString()})");
                        }
                    }
                
                    Thread.Sleep(333);  
                }
                
                postsProgressTask.Increment(1);
            }
            
            groupsProgressTask.Increment(1);
        }
    }
}