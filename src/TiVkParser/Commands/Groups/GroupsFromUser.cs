using Spectre.Console;
using TiVkParser.Core;
using TiVkParser.Logging;
using TiVkParser.Models.Core.Filters;
using TiVkParser.Models.Exports;
using TiVkParser.Models.Main.ConfigurationModels;
using VkNet.Model;

namespace TiVkParser.Commands.Groups;

internal static class GroupsFromUser
{
    /// <summary>
    /// Работа с пользователями из файла конфигурации
    /// </summary>
    /// <param name="ctx"></param>
    /// <param name="settings"></param>
    /// <param name="conf"></param>
    /// <param name="vkServiceLib"></param>
    public static (List<OutLikeModel>, List<OutCommentModel>) Execute(ProgressContext ctx, GroupsSettings settings, GroupsConfiguration conf, VkServiceLib vkServiceLib)
    {
        var outLikes = new List<OutLikeModel>();
        var outComments = new List<OutCommentModel>();
        
        var usersProgressTask = ctx
            .AddTask("[bold]Получение пользователей[/]")
            .IsIndeterminate();
        
        var users = vkServiceLib
            .FetchUsersById(conf.UserIds.Select(long.Parse));

        usersProgressTask
            .IsIndeterminate(false)
            .MaxValue(users.Count);
        
        // Обработка пользователей
        foreach (var user in users)
        {
            usersProgressTask.Description($"[bold]Пользователь:[/] [underline]{user.FirstName} {user.LastName} ({user.Id})[/]");
            
            var groupsProgressTask = ctx
                .AddTask("[bold]Получение групп пользователя[/]")
                .IsIndeterminate();
            
            var groups = vkServiceLib
                .FetchGroupsFromUser(Convert.ToInt64(user.Id));
            
            groupsProgressTask
                .IsIndeterminate(false)
                .MaxValue(groups.Count);
            
            // Обработка групп пользователя
            foreach (var group in groups)
            {
                groupsProgressTask.Description($"[bold]Группа:[/] [underline]{group.Name}[/]");
                
                var postsProgressTask = ctx
                    .AddTask("[bold]Получение постов[/]")
                    .IsIndeterminate();

                var filter = new FetchPostsFilter(
                    settings.TotalItemsForApi,
                    settings.DateFilter
                );
                
                var posts = vkServiceLib
                    .FetchPostsFromGroup(group.Id, filter)
                    .Where(post => (settings.IsLike && post.Likes.Count > 0) || (settings.IsComments && post.Comments.Count > 0))
                    .ToList();
                
                if (settings.DateFilter != null)
                    posts = posts.Where(post => post.Date > settings.DateFilter).ToList();
                
                if (posts.Count is 0)
                {
                    postsProgressTask.Increment(1);
                    continue;
                }
                
                postsProgressTask
                    .IsIndeterminate(false)
                    .MaxValue(posts.Count);
                
                // Обработка постов из группы
                foreach (var post in posts)
                {
                    postsProgressTask.Description($"[bold]Пост:[/] [underline]{post.Id} ({post.Date})[/]");
                    
                    if (settings.IsLike)
                    {
                        var likes = vkServiceLib.FetchLikesFromPost(group.Id, post.Id!.Value);
                        foreach (var _ in likes.Where(like => like.Id == user?.Id))
                        {
                            outLikes.Add(
                                new OutLikeModel(
                                    user?.Id.ToString(), 
                                    group.Id.ToString(), 
                                    post.Id?.ToString(),
                                    post.Text,
                                    $"https://vk.com/{group.ScreenName}?w=likes%2Fwall-{group.Id}_{post.Id}"
                                )
                            );
                            
                            SerilogLib.Info($"Like = {user?.Id},{group.Id},{post.Id}");
                        }
                        
                        Thread.Sleep(333);
                    }

                    if (settings.IsComments)
                    {
                        var comments = vkServiceLib
                            .FetchComments(group.Id, post.Id ?? 0);
                        
                        foreach (var comment in comments)
                        {
                            var commentsThread = new List<Comment> { comment };
                            if (comment.Thread.Count > 0)
                            {
                                commentsThread.AddRange(
                                    vkServiceLib.FetchComments(group.Id, post.Id ?? 0, comment.Id)
                                );
                            }

                            foreach (var cThread in commentsThread)
                            {
                                var userId = conf.UserIds.FirstOrDefault(uId => long.Parse(uId) == cThread.FromId);
                                if (userId == null)
                                    continue;

                                outComments.Add(
                                    new OutCommentModel(
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
                    }

                    postsProgressTask.Increment(1);
                }
                
                groupsProgressTask.Increment(1);
            }
            
            usersProgressTask.Increment(1);
        }

        return (outLikes, outComments);
    }
}