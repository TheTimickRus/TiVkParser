using Ardalis.GuardClauses;
using VkNet;
using VkNet.Abstractions;
using VkNet.Enums.Filters;
using VkNet.Model;
using VkNet.Model.Attachments;
using VkNet.Model.RequestParams;

namespace TiVkParser.Services;

public class VkServiceLib
{
    private readonly IVkApi _api = new VkApi();

    public VkServiceLib(string accessToken)
    {
        _api.Authorize(new ApiAuthParams { AccessToken = accessToken });
    }

    /// <summary>
    /// Поиск групп по запросу
    /// </summary>
    /// <param name="query">Запрос</param>
    /// <returns>Список IDs групп</returns>
    public IEnumerable<string> SearchGroups(string query)
    {
        return _api.Groups
            .Search(new GroupsSearchParams { Query = query, Count = 100, Sort = GroupSort.Relation })
            .Select(group => group.Id.ToString());
    }

    /// <summary>
    /// Получение информации по IDs групп
    /// </summary>
    /// <param name="ids">IDs групп</param>
    /// <returns>Список групп</returns>
    public IEnumerable<Group> FetchGroupsInfo(IEnumerable<string> ids)
    {
        return _api.Groups
            .GetById(ids, null, GroupsFields.All);
    }

    /// <summary>
    /// Получение постов из группы
    /// </summary>
    /// <param name="ownerId">ID сообщества</param>
    /// <param name="count">Кол-во постов</param>
    /// <returns>Сообщество, Посты сообщества</returns>
    public IEnumerable<Post> FetchPostsFromGroup(long ownerId, int count = -1)
    {
        var posts = new List<Post>();

        var flag = true;
        ulong offset = 0;
        do
        {
            var currentPosts = _api.Wall.Get(new WallGetParams { Count = 100, Offset = offset, OwnerId = -ownerId });
            
            posts.AddRange(currentPosts.WallPosts);
            
            offset += 100;
            
            if (currentPosts.TotalCount <= offset || (posts.Count >= count && count != -1)) 
                flag = false;
        } while(flag);
        
        return posts;
    }

    /// <summary>
    /// Получение комментариев под постом
    /// </summary>
    /// <param name="groupId">ID сообщества</param>
    /// <param name="postId">ID поста</param>
    /// <returns>Комментарии</returns>
    public IEnumerable<Comment> FetchCommentsFromPost(long groupId, long? postId)
    {
        return _api.Wall
            .GetComments(new WallGetCommentsParams { OwnerId = groupId > 0 ? -groupId : groupId, PostId = postId ?? 0, Count = 100 })
            .Items;
    }

    /// <summary>
    /// Получение списка сообществ пользователя
    /// </summary>
    /// <param name="userId">ID пользоветеля </param>
    /// <returns></returns>
    public IEnumerable<Group> FetchUserGroups(long userId)
    {
        return _api.Groups
            .Get(new GroupsGetParams { UserId = userId, Count = 100 });
    }

    /// <summary>
    /// Получение пользователя по ID
    /// </summary>
    /// <param name="userId"></param>
    /// <returns></returns>
    public User? FetchUserById(long userId)
    {
        var users = _api.Users.Get(new[] { userId });
        
        return users.Count > 0 
            ? users[0] 
            : null;
    }
}