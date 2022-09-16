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

    private readonly List<OutputModel> _output = new();

    public GroupsCommand()
    {
        Console.Title = Constants.Titles.FullTitle;
    }

    public override int Execute([NotNull] CommandContext context, [NotNull] Settings settings)
    {
        Guard.Against.Null(settings.UserIds);
        
        AnsiConsoleLib.ShowFiglet(Constants.Titles.VeryShortTitle, Justify.Center, Constants.Colors.MainColor);
        
        var vkService = new VkServiceLib(Constants.Params.AccessToken);
        foreach (var userId in settings.UserIds)
        {
            var user = vkService.FetchUserById(long.Parse(userId));
            if (user is null)
                continue;
            
            AnsiConsole.WriteLine($"User = {userId} ({user.FirstName} {user.LastName}))");
            
            var groups = vkService.FetchUserGroups(Convert.ToInt64(userId));
            foreach (var group in groups)
            {
                AnsiConsole.WriteLine($"\tGroup = {group.Id} ({group.Name})");
                
                var posts = vkService.FetchPostsFromGroup(group.Id, 100);
                foreach (var post in posts.Where(post => post.Comments.Count > 0))
                {
                    var postText = post.Text.Length <= 50 
                        ? post.Text 
                        : post.Text[..50];
                    AnsiConsole.WriteLine($"\t\tPost = {post.Id} ({postText}) (CommentCount = {post.Comments.Count})");
                    
                    var comments = vkService.FetchCommentsFromPost(group.Id, post.Id);
                    foreach (var comment in comments)
                    {
                        if (comment.FromId != Convert.ToInt64(userId))
                            continue;
                        
                        AnsiConsole.WriteLine($"\t\t\tComment = {comment.Id} ({comment.Text})");
                        
                        _output.Add(new OutputModel(
                            userId, 
                            group.Id.ToString(), 
                            post.Id?.ToString() ?? "-1", 
                            comment.Id.ToString(), 
                            comment.Text)
                        );
                    }
                }
            }
        }
        
        OutputService.ToExcel("Output.xlsx", _output);
        
        return 0;
    }
}