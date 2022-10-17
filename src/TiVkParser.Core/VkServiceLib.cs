using System.Diagnostics.CodeAnalysis;
using TiVkParser.Core.Helpers;
using TiVkParser.Models.Core.ExecuteModels.FetchPosts;
using TiVkParser.Models.Core.Filters;
using VkNet;
using VkNet.Abstractions;
using VkNet.Enums.Filters;
using VkNet.Enums.SafetyEnums;
using VkNet.Model;
using VkNet.Model.Attachments;
using VkNet.Model.RequestParams;
using VkNet.Utils;

namespace TiVkParser.Core;

/// <summary>
/// Надстройка над сервисом VK.NET
/// </summary>
public class VkServiceLib
{
    private readonly IVkApi _api = new VkApi();
    private readonly long _apiLimit;

    /// <summary>
    /// Конструктор с параметрами
    /// </summary>
    /// <param name="accessToken">Токен доступа</param>
    /// <param name="apiLimit">Максимальное кол-во элементов, получаемых в методах</param>
    public VkServiceLib(string accessToken, long apiLimit)
    {
        _api.Authorize(new ApiAuthParams { AccessToken = accessToken });
        _api.Account.GetProfileInfo();
        
        _apiLimit = apiLimit;
    }

    /// <summary>
    /// Поиск групп по запросу
    /// </summary>
    /// <param name="query">Запрос</param>
    /// <returns>Список IDs групп</returns>
    public IEnumerable<long> SearchGroups(string query)
    {
        var chunk = _api.Groups
            .Search(
                new GroupsSearchParams
                {
                    Query = query, 
                    Count = 100, 
                    Sort = GroupSort.Relation
                }
            );
        
        var output = new List<Group>(chunk);

        var totalCount = (long)chunk.TotalCount;
        var limit = _apiLimit < totalCount
            ? totalCount
            : _apiLimit;
        
        var pagination = new OffsetPagination(limit, 100);
        while (pagination.IsNotFinal)
        {
            chunk = _api.Groups
                .Search(
                    new GroupsSearchParams
                    {
                        Query = query, 
                        Offset = pagination.CurrentOffset, 
                        Count = 100,
                        Sort = GroupSort.Relation
                    }
                );
            
            output.AddRange(chunk);
            
            pagination.Increment();
        }
        
        return output.Select(group => group.Id);
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
    /// <param name="filter"></param>
    /// <returns>Посты сообщества</returns>
    [SuppressMessage("Performance", "CA1822:Пометьте члены как статические")]
    public IEnumerable<Post> FetchPostsFromGroup(long ownerId, FetchPostsFilter filter)
    {
        static PostsExecuteResponse Execute(IVkApiCategories api, long ownerId, long offset)
        {
            const string executeCode = 
                $$"""
                    var ownerId = parseInt(Args.ownerId);
                    var offset = parseInt(Args.offset);
                              
                    var tiPosts = { "ids" : [], "likes" : [], "comments" : [], "date": [] };

                    var flag = 0;
                    while (flag <= 24) { 
                        var posts = API.wall.get({"owner_id":ownerId, "offset":offset, "count":100}).items;
                        tiPosts.ids.push(posts@.id);
                        tiPosts.likes.push(posts@.likes);
                        tiPosts.comments.push(posts@.comments);
                        tiPosts.date.push(posts@.date);

                        offset = offset + 100; 
                        flag = flag + 1;
                    };

                    return {"posts" : tiPosts, "offset" : offset};
                """ ;

            var vkArgs = new VkParameters
            {
                { "ownerId", ownerId },
                { "offset", offset }
            };

            return api.Execute.Execute<PostsExecuteResponse>(executeCode, vkArgs);
        }
        
        var data = _api.Wall.Get(
            new WallGetParams { 
                OwnerId = ownerId > 0 ? -ownerId : ownerId,
                Count = 100
            }
        );
        
        if (data.TotalCount < 100)
            return data.WallPosts;
        
        var allItems = new List<Post>(data.WallPosts);
        var pagination = new OffsetPagination((long)data.TotalCount, 100);
        
        while (pagination.IsNotFinal)
        {
            var response = Execute(_api, -ownerId, pagination.CurrentOffset);
            
            for (var i = 0; i < response.Posts.Ids?.Count; i++)
            {
                for (var j = 0; j < response.Posts.Ids?[i].Count; j++)
                {
                    var post = new Post
                    {
                        Id = response.Posts.Ids[i][j],
                        Likes = response.Posts.Likes?[i][j],
                        Comments = response.Posts.Comments?[i][j],
                        Date = DateTimeOffset.FromUnixTimeSeconds(response.Posts.Dates?[i][j] ?? 0).DateTime
                    };
                
                    allItems.Add(post);
                }
            }
            
            pagination.Increment(2500);
            
            if (allItems.Count >= filter.TotalLimit)
                break;
        }
        
        return allItems;
    }

    /// <summary>
    /// Получение лайков под постом
    /// </summary>
    /// <param name="groupId">ID группы</param>
    /// <param name="postId">ID поста</param>
    /// <returns>Пользователи</returns>
    public IEnumerable<User> FetchLikesFromPost(long groupId, long postId)
    {
        var safetyGroupId = groupId < 0 ? -groupId : groupId;
        
        var chunk = _api.Likes.GetListEx(new LikesGetListParams { Count = 100, Type = LikeObjectType.Post, ItemId = postId, OwnerId = safetyGroupId });
        var output = new List<User>(chunk.Users);
        
        var pagination = new OffsetPagination(GetCurrentLimit(chunk.TotalCount), 100);
        while (pagination.IsNotFinal)
        {
            output.AddRange(
                _api.Likes.GetListEx(new LikesGetListParams { Count = 100, Type = LikeObjectType.Post, ItemId = postId, OwnerId = safetyGroupId, Offset = (uint?)pagination.CurrentOffset }).Users
            );
            pagination.Increment();
        }
        
        return output;
    }
    
    /// <summary>
    /// Получение комментариев под постом
    /// </summary>
    /// <param name="groupId">ID сообщества</param>
    /// <param name="postId">ID поста</param>
    /// <returns>Комментарии</returns>
    public IEnumerable<Comment> FetchCommentsFromPost(long groupId, long? postId)
    {
        var safetyGroupId = groupId < 0 ? -groupId : groupId;
        
        var chunk = _api.Wall.GetComments(new WallGetCommentsParams { OwnerId = safetyGroupId, PostId = postId ?? 0, Count = 100 });
        var output = new List<Comment>(chunk.Items);
        
        var pagination = new OffsetPagination(GetCurrentLimit(chunk.Count), 100);
        while (pagination.IsNotFinal)
        {
            output.AddRange(
                _api.Wall.GetComments(new WallGetCommentsParams { OwnerId = safetyGroupId, PostId = postId ?? 0, Count = 100, Offset = pagination.CurrentOffset }).Items
            );
            pagination.Increment();
        }
        
        return output;
    }

    public IEnumerable<Comment> FetchCommentsThread(long groupId, long postId, long commentId)
    {
        try
        {
            return _api.Wall.GetComments(new WallGetCommentsParams { OwnerId = groupId, Count = 100, PostId = postId, CommentId = commentId }).Items;
        }
        catch
        {
            return new List<Comment>();
        }
    }

    /// <summary>
    /// Получение списка сообществ пользователя
    /// </summary>
    /// <param name="userId">ID пользоветеля</param>
    /// <returns>Группы</returns>
    public IEnumerable<Group> FetchGroupsFromUser(long userId)
    {
        try
        {
            var chunk = _api.Groups.Get(new GroupsGetParams { UserId = userId, Count = 100, Fields = GroupsFields.All });
            var output = new List<Group>(chunk);
            
            var pagination = new OffsetPagination(GetCurrentLimit(chunk.TotalCount), 100);
            while (pagination.IsNotFinal)
            {
                output.AddRange(
                    _api.Groups.Get(new GroupsGetParams { UserId = userId, Count = 100, Fields = GroupsFields.All, Offset = pagination.CurrentOffset })
                );
                pagination.Increment();
            }
            
            return output;
        }
        catch
        {
            return new List<Group>();
        }
    }

    /// <summary>
    /// Получение пользователя по ID
    /// </summary>
    /// <param name="userId">ID пользователя</param>
    /// <returns>Пользователь</returns>
    public User? FetchUserById(long userId)
    {
        try
        {
            return _api.Users.Get(new List<long> { userId }).FirstOrDefault();
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Получение пользователей по IDs
    /// </summary>
    /// <param name="userIds">ID пользователей</param>
    /// <returns>Пользователи</returns>
    public IEnumerable<User> FetchUsersById(IEnumerable<long> userIds)
    {
        try
        {
            return _api.Users.Get(userIds);
        }
        catch
        {
            return new List<User>();
        }
    }

    /// <summary>
    /// Получает список друзей пользователя
    /// </summary>
    /// <param name="userId">ID пользователя</param>
    /// <returns>Список ID</returns>
    public IEnumerable<User> FetchFriendsFromUser(long userId)
    {
        try
        {
            var chunk = _api.Friends.Get(new FriendsGetParams { UserId = userId, Count = 100 });
            var allFriends = new List<User>(chunk);
            
            var pagination = new OffsetPagination(GetCurrentLimit(chunk.TotalCount), 100);
            while (pagination.IsNotFinal)
            {
                allFriends.AddRange(
                    _api.Friends.Get(new FriendsGetParams { UserId = userId, Count = 100, Offset = pagination.CurrentOffset })
                );
                pagination.Increment();
            }
            
            return allFriends;
        }
        catch
        {
            return new List<User>();
        }
    }

    private long GetCurrentLimit<T>(T totalLimit)
    {
        var tl = Convert.ToInt64(totalLimit);
        return _apiLimit < tl
            ? tl
            : _apiLimit;
    }
}