using System.Collections.ObjectModel;
using Newtonsoft.Json;
using VkNet.Model;
using VkNet.Utils;

namespace TiVkParser.Models.ExecuteModels;

[Serializable]
public class PostsExecuteResponse
{
    [JsonProperty("posts")] public PostsExecute Posts { get; set; } = new();
    [JsonProperty("offset")] public long Offset { get; set; }

    public static PostsExecuteResponse FromJson(VkResponse response)
    {
        var obj = new PostsExecuteResponse
        {
            Posts = PostsExecute.FromJson(response["posts"]),
            Offset = (long) response["offset"]
        };
        
        return obj;
    }
}

[Serializable]
public class PostsExecute
{
    [JsonProperty("ids")] public ReadOnlyCollection<long>? Ids { get; set; }
    [JsonProperty("likes")] public ReadOnlyCollection<Likes>? Likes { get; set; }
    [JsonProperty("comments")] public ReadOnlyCollection<Comments>? Comments { get; set; }

    public static PostsExecute FromJson(VkResponse response)
    {
        var obj = new PostsExecute
        {
            Ids = response["ids"].ToReadOnlyCollectionOf((Func<VkResponse, long>) (r => (long) r)),
            Likes = response["likes"].ToReadOnlyCollectionOf((Func<VkResponse, Likes>) (r => (Likes) r)),
            Comments = response["comments"].ToReadOnlyCollectionOf((Func<VkResponse, Comments>) (r => (Comments) r))
        };
        
        return obj;
    }
}