using System.Collections.ObjectModel;
using Newtonsoft.Json;
using VkNet.Model;
using VkNet.Utils;

namespace TiVkParser.Models.ExecuteModels;

[Serializable]
public class PostsExecuteResponse
{
    [JsonProperty("posts")] public PostsExecute Posts { get; set; } = new();
    [JsonProperty("offset")] public long CurrentOffset { get; set; }

    /*
    public static PostsExecuteResponse FromJson(VkResponse response)
    {
        var obj = new PostsExecuteResponse
        {
            Posts = PostsExecute.FromJson(response["posts"]),
            CurrentOffset = (long) response["offset"]
        };
        
        return obj;
    }
    */
}

[Serializable]
public class PostsExecute
{
    [JsonProperty("ids")] public ReadOnlyCollection<ReadOnlyCollection<long>>? Ids { get; set; }
    [JsonProperty("likes")] public ReadOnlyCollection<ReadOnlyCollection<Likes>>? Likes { get; set; }
    [JsonProperty("comments")] public ReadOnlyCollection<ReadOnlyCollection<Comments>>? Comments { get; set; }

    /*
    public static PostsExecute FromJson(VkResponse response)
    {
        var obj = new PostsExecute
        {
            Ids = response["ids"].ToReadOnlyCollectionOf((Func<VkResponse, ReadOnlyCollection<long>>) (r => r)),
            Likes = response["likes"].ToReadOnlyCollectionOf((Func<VkResponse, Likes>) (r => (Likes) r)),
            Comments = response["comments"].ToReadOnlyCollectionOf((Func<VkResponse, Comments>) (r => (Comments) r))
        };
        
        return obj;
    }
    */
}