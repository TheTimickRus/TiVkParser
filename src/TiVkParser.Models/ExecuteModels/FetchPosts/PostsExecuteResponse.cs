using Newtonsoft.Json;

namespace TiVkParser.Models.ExecuteModels.FetchPosts;

[Serializable]
public class PostsExecuteResponse
{
    [JsonProperty("posts")] public PostsExecute Posts { get; set; } = new();
    [JsonProperty("offset")] public long CurrentOffset { get; set; }
}