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

namespace TiVkParser.Commands;

public class GroupsCommand : Command<GroupsCommand.Settings>
{
    public class Settings : CommandSettings
    {
        [Description("Путь до файла конфигурации")]
        [CommandOption("-c|--cfg")]
        [DefaultValue("TiVkParser.toml")]
        public string ConfigFile { get; init; } = "TiVkParser.toml";
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
        
        var vkService = new VkServiceLib(_conf.AccessToken, 100);

        AnsiConsole.Progress()
            .HideCompleted(true)
            .Columns(
                new TaskDescriptionColumn(), 
                new ProgressBarColumn(), 
                new PercentageColumn(), 
                new RemainingTimeColumn(), 
                new SpinnerColumn()
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

        var usersProgressTask = ctx.AddTask("Получение пользователей", new ProgressTaskSettings { AutoStart = true, MaxValue = _conf.UserIds.Count });
        foreach (var userId in _conf.UserIds)
        {
            var user = vkService.FetchUserById(long.Parse(userId));
            if (user is null)
                continue;

            usersProgressTask.Description = $"Пользователь: ({user.Id} ({user.FirstName} {user.LastName}))";
            
            var groupsProgressTask = ctx
                .AddTask("Получение групп пользователя")
                .IsIndeterminate();
            
            var groups = vkService.FetchUserGroups(Convert.ToInt64(userId)).ToList();
            
            groupsProgressTask.IsIndeterminate = false;
            groupsProgressTask.MaxValue = groups.Count;
            
            foreach (var group in groups)
            {
                groupsProgressTask.Description = $"Группа: ({group.Name})";
                
                var postsProgressTask = ctx
                    .AddTask("Получение постов")
                    .IsIndeterminate();
                
                var posts = vkService.FetchPostsFromGroup(group.Id, 100)
                    .Where(post => post.Comments.Count > 2 || post.Likes.Count > 0)
                    .ToList();

                postsProgressTask.IsIndeterminate = false;
                postsProgressTask.MaxValue = posts.Count;
                
                foreach (var post in posts)
                {
                    postsProgressTask.Description = $"Пост: (PostID = {post.Id})";
                    
                    var likes = vkService.FetchLikesFromPost(group.Id, post.Id).ToList();
                    foreach (var unused in likes.Where(like => like.Id == Convert.ToInt64(userId)))
                    {
                        _outLikes.Add(
                            new LikeModel(
                                userId, 
                                group.Id.ToString(), 
                                post.Id?.ToString() ?? "-1"
                            )
                        );
                    }
                    
                    Thread.Sleep(333);
                    
                    var comments = vkService.FetchCommentsFromPost(group.Id, post.Id);
                    foreach (var comment in comments)
                    {
                        if (comment.FromId != Convert.ToInt64(userId))
                            continue;
                        
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

        foreach (var groupId in _conf.GroupIds)
        {
            AnsiConsole.WriteLine($"Group = {groupId}");
            
            var posts = vkService.FetchPostsFromGroup(long.Parse(groupId));
            foreach (var post in posts.Where(post => post.Comments.Count > 2 || post.Likes.Count > 0))
            {
                var postText = post.Text.Length <= 50 
                    ? post.Text 
                    : post.Text[..50];
                postText = postText.Replace("\n", "");
                
                AnsiConsole.WriteLine($"\t\tPost = {post.Id} ({(postText.Equals("") ? post.ToString() : postText)}) (LikesCount = {post.Likes.Count}) (CommentsCount = {post.Comments.Count})");
                
                /* Лайки */
                var likes = vkService.FetchLikesFromPost(long.Parse(groupId), post.Id);
                foreach (var like in likes)
                {
                    foreach (var userId in _conf.UserIds.Where(userId => like.Id == Convert.ToInt64(userId)))
                    {
                        AnsiConsole.WriteLine($"\t\t\tLike = {like.Id}");
                        
                        _outLikes.Add(
                            new LikeModel(
                                userId, 
                                groupId, 
                                post.Id?.ToString() ?? "-1"
                            )
                        );
                    }
                }
                
                /* Комментарии */
                var comments = vkService.FetchCommentsFromPost(long.Parse(groupId), post.Id);
                foreach (var comment in comments)
                {
                    foreach (var userId in _conf.UserIds.Where(userId => comment.Id == Convert.ToInt64(userId)))
                    {
                        AnsiConsole.WriteLine($"\t\t\t\tComment = {comment.Id} ({comment.Text})");
                    
                        _outComments.Add(
                            new CommentModel(
                                userId, 
                                groupId, 
                                post.Id?.ToString() ?? "-1", 
                                comment.Id.ToString(), 
                                comment.Text
                            )
                        );
                    }
                }
                
                Thread.Sleep(333);
            }
        }
    }
}