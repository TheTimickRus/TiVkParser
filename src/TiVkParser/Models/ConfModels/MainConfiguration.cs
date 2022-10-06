// ReSharper disable CollectionNeverUpdated.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace TiVkParser.Models.ConfModels;

public record MainConfiguration
{
    public string? AccessToken { get; set; }
    public GroupsConfiguration? Groups { get; set; }
    public FriendsConfiguration? Friends { get; set; }
}