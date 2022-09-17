// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable RedundantNullableFlowAttribute
// ReSharper disable ClassNeverInstantiated.Global

using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using Ardalis.GuardClauses;
using Spectre.Console;
using Spectre.Console.Cli;
using TiVkParser.Models;
using TiVkParser.Services;

namespace TiVkParser.Commands;

public class GroupsCommand : Command<GroupsCommand.Settings>
{
    public class Settings : CommandSettings
    {
        [Description("IDs пользователей")]
        [CommandArgument(0, "[UserIds]")]
        public string[]? UserIds { get; init; }

        [Description("AccessToken доступа")]
        [CommandOption("-t|--token")]
        public string? AccessToken { get; init; }
    }
    
    private readonly List<LikeModel> _outLikes = new();
    private readonly List<CommentModel> _outComments = new();

    public GroupsCommand()
    {
        Console.Title = Constants.Titles.FullTitle;
    }

    public override int Execute([NotNull] CommandContext context, [NotNull] Settings settings)
    {
        Guard.Against.Null(settings.UserIds);
        
        AnsiConsoleLib.ShowFiglet(Constants.Titles.VeryShortTitle, Justify.Center, Constants.Colors.MainColor);
        AnsiConsoleLib.ShowRule(Constants.Titles.FullTitle, Justify.Right, Constants.Colors.MainColor);
        
        var vkService = new VkServiceLib(Constants.Params.AccessToken);
        foreach (var userId in settings.UserIds)
        {
            var user = vkService.FetchUserById(long.Parse(userId));
            if (user is null)
                continue;
            
            AnsiConsole.WriteLine($"User = {userId} ({user.FirstName} {user.LastName})");
            
            var groups = vkService.FetchUserGroups(Convert.ToInt64(userId));
            foreach (var group in groups)
            {
                AnsiConsole.WriteLine($"\tGroup = {group.Id} ({group.Name})");
                
                var posts = vkService.FetchPostsFromGroup(group.Id, 100);
                foreach (var post in posts.Where(post => post.Comments.Count > 2 || post.Likes.Count > 0))
                {
                    var postText = post.Text.Length <= 50 
                        ? post.Text 
                        : post.Text[..50];
                    postText = postText.Replace("\n", "");
                    
                    AnsiConsole.WriteLine($"\t\tPost = {post.Id} ({(postText.Equals("") ? post.ToString() : postText)}) (LikesCount = {post.Likes.Count}) (CommentsCount = {post.Comments.Count})");
                    
                    var likes = vkService.FetchLikesFromPost(group.Id, post.Id);
                    foreach (var like in likes)
                    {
                        if (like.Id != Convert.ToInt64(userId))
                            continue;
                        
                        AnsiConsole.WriteLine($"\t\t\tLike = {like.Id}");
                        
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
                        
                        AnsiConsole.WriteLine($"\t\t\t\tComment = {comment.Id} ({comment.Text})");
                        
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
                }
            }
        }
        
        OutputService.LikesToExcel("Output.xlsx", _outLikes);
        OutputService.CommentsToExcel("Output.xlsx", _outComments);
        
        return 0;
    }
}