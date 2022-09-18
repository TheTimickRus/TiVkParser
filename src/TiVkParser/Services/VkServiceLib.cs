using TiVkParser.Helpers;
using VkNet;
using VkNet.Abstractions;
using VkNet.Enums.Filters;
using VkNet.Enums.SafetyEnums;
using VkNet.Model;
using VkNet.Model.Attachments;
using VkNet.Model.RequestParams;

namespace TiVkParser.Services;

public class VkServiceLib
{
    public bool IsAuth { get; set; }

    private readonly IVkApi _api = new VkApi();
    private readonly long _limit;

    public VkServiceLib(string accessToken, long limit)
    {
        _api.Authorize(new ApiAuthParams { AccessToken = accessToken });
        _api.Account.GetProfileInfo();
        
        _limit = limit;
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
        var data = _api.Wall.Get(
            new WallGetParams { 
                OwnerId = -ownerId,
                Count = 100
            }
        );

        if (data.TotalCount < 100)
            return data.WallPosts;
        
        /*
        var executeCode =
            $$"""
                var ownerId = -77229360;
                var offset = 100;
                
                var posts = API.wall.get({"owner_id":ownerId, "offset":offset, "count":100}).items; 
                
                var tiPosts = {"ids":posts@.id, "likes":posts@.likes, "comments":posts@.comments};
                
                var flag = 1;
                while (flag <= 24) { 
                    var newPosts = API.wall.get({"owner_id":ownerId, "offset":offset, "count":100}).items;
                    tiPosts.ids.push(newPosts@.id);
                    tiPosts.likes.push(newPosts@.likes);
                    tiPosts.comments.push(newPosts@.comments);
                    
                    offset = offset + 100;
                    
                    flag = flag + 1;
                };
                
                return {"posts":tiPosts, "offset":offset};
            """;

        var response = _api.Execute.Execute(executeCode);
        var p = FetchPostsExecuteClass.FromJson(response);
        */
        
        var offsetData = new List<Post>();
        var pagination = new OffsetPagination((long)data.TotalCount);
        
        offsetData.AddRange(data.WallPosts);
        pagination.Increment();
        
        while (pagination.IsNotFinal)
        {
            offsetData.AddRange(
                _api.Wall.Get(
                    new WallGetParams
                    {
                        Count = 100, 
                        Offset = (ulong)pagination.CurrentOffset, 
                        OwnerId = -ownerId
                    }
                )
                .WallPosts
            );
            pagination.Increment();
            Console.Title = $"{Constants.Titles.FullTitle} | Offset = {pagination.CurrentOffset}";
            
            // Временное решение, чтобы не получать миллиарты постов со стены
            if (pagination.CurrentOffset > _limit)
                break;
            
            Thread.Sleep(333);
        }

        Console.Title = Constants.Titles.FullTitle;
        return offsetData;
    }

    /// <summary>
    /// Получение комментариев под постом
    /// </summary>
    /// <param name="groupId">ID сообщества</param>
    /// <param name="postId">ID поста</param>
    /// <returns>Комментарии</returns>
    public IEnumerable<Comment> FetchCommentsFromPost(long groupId, long? postId)
    {
        var data = _api.Wall.GetComments(
            new WallGetCommentsParams
            {
                OwnerId = groupId > 0 ? -groupId : groupId, 
                PostId = postId ?? 0, 
                Count = 100
            }
        );

        if (data.Count < 100)
            return data.Items;
        
        var offsetData = new List<Comment>();
        var pagination = new OffsetPagination(data.Count);
        
        offsetData.AddRange(data.Items);
        pagination.Increment();
        
        while (pagination.IsNotFinal)
        {
            offsetData.AddRange(
                _api.Wall.GetComments(
                    new WallGetCommentsParams
                    {
                        OwnerId = groupId > 0 ? -groupId : groupId, 
                        PostId = postId ?? 0, 
                        Count = 100,
                        Offset = pagination.CurrentOffset
                    }
                )
                .Items
            );
            pagination.Increment();
            Console.Title = $"{Constants.Titles.FullTitle} | Offset = {pagination.CurrentOffset}";
            
            // Временное решение, чтобы не получать миллиарты постов со стены
            if (pagination.CurrentOffset > _limit)
                break;
            
            Thread.Sleep(333);
        }
        
        Console.Title = Constants.Titles.FullTitle;
        return offsetData;
    }

    public IEnumerable<User> FetchLikesFromPost(long groupId, long? postId)
    {
        var data = _api.Likes.GetListEx(
            new LikesGetListParams
            {
                Count = 100,
                Type = LikeObjectType.Post,
                ItemId = postId ?? 0,
                OwnerId = groupId > 0 ? -groupId : groupId
            }
        );

        if (data.TotalCount < 100)
            return data.Users;
        
        var offsetData = new List<User>();
        var pagination = new OffsetPagination((long)data.TotalCount);
        
        offsetData.AddRange(data.Users);
        pagination.Increment();
        
        while (pagination.IsNotFinal)
        {
            offsetData.AddRange(
                _api.Likes.GetListEx(
                    new LikesGetListParams
                    {
                        Count = 100,
                        Type = LikeObjectType.Post,
                        ItemId = postId ?? 0,
                        OwnerId = groupId > 0 ? -groupId : groupId,
                        Offset = (uint?)pagination.CurrentOffset
                    }
                )
                .Users
            );
            pagination.Increment();
            Console.Title = $"{Constants.Titles.FullTitle} | Offset = {pagination.CurrentOffset}";
            
            // Временное решение, чтобы не получать миллиарты постов со стены
            if (pagination.CurrentOffset > _limit)
                break;

            Thread.Sleep(333);
        }
        
        Console.Title = Constants.Titles.FullTitle;
        return offsetData;
    }

    /// <summary>
    /// Получение списка сообществ пользователя
    /// </summary>
    /// <param name="userId">ID пользоветеля</param>
    /// <returns>Группы</returns>
    public IEnumerable<Group> FetchUserGroups(long userId)
    {
        var data = _api.Groups.Get(
            new GroupsGetParams
            {
                UserId = userId, 
                Count = 100,
                Extended = true
            }
        );

        if (data.Count < 100)
            return data;
        
        var offsetData = new List<Group>();
        var pagination = new OffsetPagination(data.Count);
        
        offsetData.AddRange(data);
        pagination.Increment();
        
        while (pagination.IsNotFinal)
        {
            offsetData.AddRange(
                _api.Groups.Get(
                    new GroupsGetParams
                    {
                        UserId = userId, 
                        Count = 100, 
                        Fields = GroupsFields.All,
                        Offset = pagination.CurrentOffset
                    }
                )
            );
            pagination.Increment();
            Console.Title = $"{Constants.Titles.FullTitle} | Offset = {pagination.CurrentOffset}";
            
            // Временное решение, чтобы не получать миллиарты постов со стены
            if (pagination.CurrentOffset > _limit)
                break;

            Thread.Sleep(333);
        }
        
        Console.Title = Constants.Titles.FullTitle;
        return offsetData;
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
            // ignore
        }

        return null;
    }

    /// <summary>
    /// Получение пользователей по IDs
    /// </summary>
    /// <param name="ids">ID пользователей</param>
    /// <returns>Пользователи</returns>
    public IEnumerable<User> FetchUsersById(IEnumerable<long> ids)
    {
        try
        {
            return _api.Users.Get(ids);
        }
        catch
        {
            // ignore
        }

        return new List<User>();
    }
}