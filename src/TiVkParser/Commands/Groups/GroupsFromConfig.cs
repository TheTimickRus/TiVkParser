using Spectre.Console;
using TiVkParser.Core;
using TiVkParser.Logging;
using TiVkParser.Models.Core.Filters;
using TiVkParser.Models.Exports;
using TiVkParser.Models.Main.ConfigurationModels;
using VkNet.Enums;
using VkNet.Model;

namespace TiVkParser.Commands.Groups;

public static class GroupsFromConfig
{
    /// <summary>
    /// Работа с группами из файла конфигурации
    /// </summary>
    /// <param name="ctx"></param>
    /// <param name="settings"></param>
    /// <param name="conf"></param>
    /// <param name="vkServiceLib"></param>
    public static (List<OutLike>, List<OutComment>) Execute(ProgressContext ctx, GroupsSettings settings, GroupsConfiguration conf, VkServiceLib vkServiceLib)
    {
        var outLikes = new List<OutLike>();
        var outComments = new List<OutComment>();
        
        var groupsProgressTask = ctx
            .AddTask($"[bold {Constants.Colors.SecondColor}]Получение групп[/]")
            .IsIndeterminate();
        
        var groups = vkServiceLib
            .FetchGroupsInfo(conf.GroupIds)
            .Where(group => group.IsClosed is GroupPublicity.Public)
            .ToList();
        
        groupsProgressTask.IsIndeterminate = false;
        groupsProgressTask.MaxValue = groups.Count;

        foreach (var group in groups)
        {
            groupsProgressTask.Description = $"[bold {Constants.Colors.SecondColor}] Группа:[/] [underline]{group.Name} ({group.Id})[/]";
            
            var postsProgressTask = ctx
                .AddTask($"[bold {Constants.Colors.SecondColor}]Получение постов[/]")
                .IsIndeterminate();

            var filter = new FetchPostsFilter(
                settings.TotalItemsForApi,
                settings.DateFilter
            );
            
            var posts = vkServiceLib
                .FetchPostsFromGroup(group.Id, filter)
                .ToList();
            
            if (settings.DateFilter != null)
                posts = posts.Where(post => post.Date > settings.DateFilter).ToList();
            
            if (posts.Count is 0)
            {
                postsProgressTask.StopTask();
                continue;
            }
            
            postsProgressTask.IsIndeterminate = false;
            postsProgressTask.MaxValue = posts.Count;
            
            foreach (var post in posts)
            {
                postsProgressTask.Description = $"[bold {Constants.Colors.SecondColor}]Пост:[/] [underline]{post.Id} ({post.Date})[/]";
                
                if (settings.IsLike && post.Likes.Count > 0)
                {
                    var likes = vkServiceLib.FetchLikesFromPost(group.Id, post.Id!.Value);
                    foreach (var like in likes)
                    {
                        foreach (var userId in conf.UserIds.Where(userId => like.Id == Convert.ToInt64(userId)))
                        {
                            outLikes.Add(
                                new OutLike(
                                    userId, 
                                    group.Id.ToString(), 
                                    post.Id?.ToString(),
                                    post.Text,
                                    $"https://vk.com/{group.ScreenName}?w=likes%2Fwall-{group.Id}_{post.Id}"
                                )
                            );
                            
                            SerilogLib.Info($"Like = ({userId},{group.Id.ToString()},{post.Id.ToString()})");
                        }
                    }  
                    
                    Thread.Sleep(333);
                }
                
                if (settings.IsComments && post.Comments.Count > 0)
                {
                    var comments = vkServiceLib
                        .FetchComments(group.Id, post.Id!.Value);
                    
                    foreach (var comment in comments)
                    {
                        var commentsThread = new List<Comment> { comment };
                        if (comment.Thread.Count > 0)
                        {
                            Thread.Sleep(333);
                            
                            commentsThread.AddRange(
                                vkServiceLib.FetchComments(group.Id, post.Id.Value, comment.Id)
                            ); 
                        }

                        foreach (var cThread in commentsThread)
                        {
                            var userId = conf.UserIds.FirstOrDefault(uId => long.Parse(uId) == cThread.FromId);
                            if (userId == null) 
                                continue;
                            
                            outComments.Add(
                                new OutComment(
                                    userId,
                                    group.Id.ToString(),
                                    post.Id.ToString(), 
                                    post.Text,
                                    cThread.Id.ToString(), 
                                    cThread.Text,
                                    $"https://vk.com/{group.ScreenName}?w=wall-{group.Id}_{post.Id}",
                                    $"https://vk.com/{group.ScreenName}?w=wall-{group.Id}_{post.Id}_r{cThread.Id}"
                                )
                            );
                            
                            SerilogLib.Info($"Comment = ({userId},{group.Id},{post.Id},{comment.Id})");
                        }
                    }
                
                    Thread.Sleep(333);  
                }
                
                postsProgressTask.Increment(1);
            }
            
            groupsProgressTask.Increment(1);
        }
        
        return (outLikes, outComments);
    }
}