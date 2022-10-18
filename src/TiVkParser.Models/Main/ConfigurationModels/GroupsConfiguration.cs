// ReSharper disable CollectionNeverUpdated.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable ClassNeverInstantiated.Global

namespace TiVkParser.Models.Main.ConfigurationModels;

public record GroupsConfiguration
{
    public bool IsGroupsFromUser { get; set; }
    public List<string> UserIds { get; set; } = new();
    public List<string> GroupIds { get; set; } = new();
}