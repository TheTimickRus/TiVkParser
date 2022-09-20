﻿using TiVkParser.Helpers;
using TiVkParser.Models.ExecuteModels;
using VkNet;
using VkNet.Abstractions;
using VkNet.Enums.Filters;
using VkNet.Enums.SafetyEnums;
using VkNet.Model;
using VkNet.Model.Attachments;
using VkNet.Model.RequestParams;

namespace TiVkParser.Services;

/// <summary>
/// Надстройка над сервисом VK.NET
/// </summary>
public class VkServiceLib
{
    private readonly IVkApi _api = new VkApi();
    private readonly long _totalCount;

    /// <summary>
    /// Конструктор с параметрами
    /// </summary>
    /// <param name="accessToken">Токен доступа</param>
    /// <param name="totalCount">Максимальное кол-во элементов, получаемых в методах</param>
    public VkServiceLib(string accessToken, long totalCount)
    {
        _api.Authorize(new ApiAuthParams { AccessToken = accessToken });
        _api.Account.GetProfileInfo();
        
        _totalCount = totalCount;
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
    /// <returns>Посты сообщества</returns>
    public IEnumerable<Post> FetchPostsFromGroup(long ownerId)
    {
        var data = _api.Wall.Get(
            new WallGetParams { 
                OwnerId = -ownerId,
                Count = 100
            }
        );

        if (data.TotalCount < 100)
            return data.WallPosts;
        
        var executeCode =
            $$"""
                var ownerId = -77229360;
                var offset = 0;
                                 
                var tiPosts = { "ids" : [], "likes" : [], "comments" : [] };
                
                var flag = 0;
                while (flag <= 24) { 
                    var posts = API.wall.get({"owner_id":ownerId, "offset":offset, "count":100}).items;
                    tiPosts.ids.push(posts@.id);
                    tiPosts.likes.push(posts@.likes);
                    tiPosts.comments.push(posts@.comments);
                    
                    offset = offset + 100; 
                    flag = flag + 1;
                };
                
                return {"posts" : tiPosts, "offset" : offset};
            """;

        var response = _api.Execute.Execute<PostsExecuteResponse>(executeCode);
        
        var allItems = new List<Post>();
        var pagination = new OffsetPagination((long)data.TotalCount);
        
        allItems.AddRange(data.WallPosts);
        pagination.Increment();
        
        while (pagination.IsNotFinal)
        {
            allItems.AddRange(
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
            Console.Title = $"{Constants.Titles.VeryShortTitle} | Offset = {pagination.CurrentOffset}";
            
            // Временное решение, чтобы не получать миллиарты постов со стены
            if (pagination.CurrentOffset > _totalCount)
                break;
            
            Thread.Sleep(333);
        }

        Console.Title = Constants.Titles.VeryShortTitle;
        return allItems;
    }

    /// <summary>
    /// Получение лайков под постом
    /// </summary>
    /// <param name="groupId">ID группы</param>
    /// <param name="postId">ID поста</param>
    /// <returns>Пользователи</returns>
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
        
        var allItems = new List<User>();
        var pagination = new OffsetPagination((long)data.TotalCount);
        
        allItems.AddRange(data.Users);
        pagination.Increment();
        
        while (pagination.IsNotFinal)
        {
            allItems.AddRange(
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
            Console.Title = $"{Constants.Titles.VeryShortTitle} | CurrentOffset = {pagination.CurrentOffset}";
            
            // Временное решение, чтобы не получать миллиарты значений
            if (pagination.CurrentOffset > _totalCount)
                break;

            Thread.Sleep(333);
        }
        
        Console.Title = Constants.Titles.VeryShortTitle;
        return allItems;
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
        
        var allItems = new List<Comment>();
        var pagination = new OffsetPagination(data.Count);
        
        allItems.AddRange(data.Items);
        pagination.Increment();
        
        while (pagination.IsNotFinal)
        {
            allItems.AddRange(
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
            Console.Title = $"{Constants.Titles.VeryShortTitle} | CurrentOffset = {pagination.CurrentOffset}";
            
            // Временное решение, чтобы не получать миллиарты значений
            if (pagination.CurrentOffset > _totalCount)
                break;
            
            Thread.Sleep(333);
        }
        
        Console.Title = Constants.Titles.VeryShortTitle;
        return allItems;
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
        
        var allItems = new List<Group>();
        var pagination = new OffsetPagination(data.Count);
        
        allItems.AddRange(data);
        pagination.Increment();
        
        while (pagination.IsNotFinal)
        {
            allItems.AddRange(
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
            Console.Title = $"{Constants.Titles.VeryShortTitle} | Offset = {pagination.CurrentOffset}";
            
            // Временное решение, чтобы не получать миллиарты значений
            if (pagination.CurrentOffset > _totalCount)
                break;

            Thread.Sleep(333);
        }
        
        Console.Title = Constants.Titles.VeryShortTitle;
        return allItems;
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
}