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
}

[Serializable]
public class PostsExecute
{
    [JsonProperty("ids")] public ReadOnlyCollection<ReadOnlyCollection<long>>? Ids { get; set; }
    [JsonProperty("likes")] public ReadOnlyCollection<ReadOnlyCollection<Likes>>? Likes { get; set; }
    [JsonProperty("comments")] public ReadOnlyCollection<ReadOnlyCollection<Comments>>? Comments { get; set; }
    [JsonProperty("date")] public ReadOnlyCollection<ReadOnlyCollection<long>>? Dates { get; set; }
}