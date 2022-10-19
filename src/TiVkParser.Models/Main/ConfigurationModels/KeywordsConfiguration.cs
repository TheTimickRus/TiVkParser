// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedMember.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace TiVkParser.Models.Main.ConfigurationModels;

public record KeywordsConfiguration
{
    public List<long>? GroupIds { get; init; }
    public List<string>? Keywords { get; init; }
}