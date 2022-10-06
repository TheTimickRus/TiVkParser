// ReSharper disable CollectionNeverUpdated.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable ClassNeverInstantiated.Global

namespace TiVkParser.Models.ConfModels;

public record GroupsConfiguration
{
    public List<string> UserIds { get; set; } = new();
    public List<string> GroupIds { get; set; } = new();
    public bool IsGroupsFromUser { get; set; }
}