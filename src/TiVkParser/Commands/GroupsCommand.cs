// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable RedundantNullableFlowAttribute
// ReSharper disable ClassNeverInstantiated.Global

using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using Ardalis.GuardClauses;
using Spectre.Console;
using Spectre.Console.Cli;
using TiVkParser.Helpers;
using TiVkParser.Models;
using TiVkParser.Services;
using Tomlyn;
using VkNet.Enums;

namespace TiVkParser.Commands;

public class GroupsCommand : Command<GroupsCommand.Settings>
{
    public class Settings : CommandSettings
    {
        [Description("Путь до файла конфигурации (Default = TiVkParser.toml)")]
        [CommandOption("-c|--cfg")]
        [DefaultValue("TiVkParser.toml")]
        public string ConfigFile { get; init; } = "TiVkParser.toml";

        [Description("Общий лимит кол-ва получаемых объектов для всех запросов (Default = 100)")]
        [CommandOption("-l|--limit")]
        [DefaultValue((long)100)]
        public long Limit { get; init; }
    }

    private ConfigurationModel? _conf;
    private readonly List<LikeModel> _outLikes = new();
    private readonly List<CommentModel> _outComments = new();

    public GroupsCommand()
    {
        Console.Title = Constants.Titles.FullTitle;
    }

    public override ValidationResult Validate([NotNull] CommandContext context, [NotNull] Settings settings)
    {
        var result = File.Exists(settings.ConfigFile) && Toml.TryToModel(File.ReadAllText(settings.ConfigFile), out _conf, out _) 
            ? ValidationResult.Success() 
            : ValidationResult.Error();

        if (!result.Successful)
            AnyHelpers.ExtractResourceToFile("TiVkParser", "Assets", "TiVkParser.toml", Environment.CurrentDirectory);
        
        return result;
    }

    public override int Execute([NotNull] CommandContext context, [NotNull] Settings settings)
    {
        Guard.Against.Null(_conf);
        
        AnsiConsoleLib.ShowFiglet(Constants.Titles.VeryShortTitle, Justify.Center, Constants.Colors.MainColor);
        AnsiConsoleLib.ShowRule(Constants.Titles.FullTitle, Justify.Right, Constants.Colors.MainColor);
        
        var vkService = new VkServiceLib(_conf.AccessToken, settings.Limit);

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
                    Spinner = Spinner.Known.Aesthetic, 
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
            });
        
        OutputService.LikesToExcel("Output.xlsx", _outLikes);
        OutputService.CommentsToExcel("Output.xlsx", _outComments);
        
        return 0;
    }

    private void GroupsFromUser(ProgressContext ctx, VkServiceLib vkService)
    {
        Guard.Against.Null(_conf);

        var usersProgressTask = ctx
            .AddTask("[bold]Получение пользователей[/]")
            .IsIndeterminate();
        
        var users = vkService.FetchUsersById(_conf.UserIds.Select(long.Parse)).ToList();

        usersProgressTask.IsIndeterminate = false;
        usersProgressTask.MaxValue = users.Count;
        
        foreach (var user in users)
        {
            usersProgressTask.Description = $"[bold]Пользователь:[/] [underline]{user.FirstName} {user.LastName} ({user.Id})[/])";
            
            var groupsProgressTask = ctx
                .AddTask("[bold]Получение групп пользователя[/]")
                .IsIndeterminate();
            
            var groups = vkService.FetchUserGroups(Convert.ToInt64(user)).ToList();
            
            groupsProgressTask.IsIndeterminate = false;
            groupsProgressTask.MaxValue = groups.Count;
            
            foreach (var group in groups)
            {
                groupsProgressTask.Description = $"[bold]Группа:[/] [underline]({group.Name})[/]";
                
                var postsProgressTask = ctx
                    .AddTask("[bold]Получение постов[/]")
                    .IsIndeterminate();
                
                var posts = vkService.FetchPostsFromGroup(group.Id, 100)
                    .Where(post => post.Comments.Count > 2 || post.Likes.Count > 0)
                    .ToList();

                postsProgressTask.IsIndeterminate = false;
                postsProgressTask.MaxValue = posts.Count;
                
                foreach (var post in posts)
                {
                    postsProgressTask.Description = $"[bold]Пост:[/] [underline]{post.Id}[/]";
                    
                    var likes = vkService.FetchLikesFromPost(group.Id, post.Id).ToList();
                    foreach (var unused in likes.Where(like => like.Id == Convert.ToInt64(user)))
                    {
                        _outLikes.Add(
                            new LikeModel(
                                user?.Id.ToString() ?? "-1", 
                                group.Id.ToString(), 
                                post.Id?.ToString() ?? "-1"
                            )
                        );
                    }
                    
                    Thread.Sleep(333);
                    
                    var comments = vkService.FetchCommentsFromPost(group.Id, post.Id);
                    foreach (var comment in comments)
                    {
                        if (comment.FromId != Convert.ToInt64(user))
                            continue;
                        
                        _outComments.Add(
                            new CommentModel(
                                user?.Id.ToString() ?? "-1",
                                group.Id.ToString(), 
                                post.Id.ToString() ?? "-1", 
                                comment.Id.ToString(), 
                                comment.Text
                            )
                        );
                    }
                    
                    Thread.Sleep(333);
                    postsProgressTask.Increment(1);
                }
                
                groupsProgressTask.Increment(1);
            }
            
            usersProgressTask.Increment(1);
        }
    }
    
    private void GroupFromConfig(ProgressContext ctx, VkServiceLib vkService)
    {
        Guard.Against.Null(_conf);

        var groupsProgressTask = ctx
            .AddTask("[bold]Получение групп[/]")
            .IsIndeterminate();
        
        var groups = vkService.FetchGroupsInfo(_conf.GroupIds)
            .Where(group => group.IsClosed is GroupPublicity.Public)
            .ToList();
        
        groupsProgressTask.IsIndeterminate = false;
        groupsProgressTask.MaxValue = groups.Count;
        
        foreach (var group in groups)
        {
            groupsProgressTask.Description = $"[bold]Группа:[/] [underline]{group.Name}[/]";
            
            var postsProgressTask = ctx
                .AddTask("[bold]Получение постов[/]")
                .IsIndeterminate();
            
            var posts = vkService.FetchPostsFromGroup(group.Id)
                .Where(post => post.Comments.Count > 2 || post.Likes.Count > 0)
                .ToList();
            
            postsProgressTask.IsIndeterminate = false;
            postsProgressTask.MaxValue = posts.Count;
            
            foreach (var post in posts)
            {
                postsProgressTask.Description = $"[bold]Пост:[/] [underline]{post.Id}[/]";
                
                /* Лайки */
                var likes = vkService.FetchLikesFromPost(group.Id, post.Id);
                foreach (var like in likes)
                {
                    foreach (var userId in _conf.UserIds.Where(userId => like.Id == Convert.ToInt64(userId)))
                    {
                        _outLikes.Add(
                            new LikeModel(
                                userId, 
                                group.Id.ToString(), 
                                post.Id?.ToString() ?? "-1"
                            )
                        );
                    }
                }
                
                Thread.Sleep(333);
                
                /* Комментарии */
                var comments = vkService.FetchCommentsFromPost(group.Id, post.Id);
                foreach (var comment in comments)
                {
                    foreach (var userId in _conf.UserIds.Where(userId => comment.Id == Convert.ToInt64(userId)))
                    {
                        _outComments.Add(
                            new CommentModel(
                                userId, 
                                group.Id.ToString(), 
                                post.Id?.ToString() ?? "-1", 
                                comment.Id.ToString(), 
                                comment.Text
                            )
                        );
                    }
                }
                
                Thread.Sleep(333);
                postsProgressTask.Increment(1);
            }
            
            groupsProgressTask.Increment(1);
        }
    }
}