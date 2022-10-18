// ReSharper disable NotAccessedPositionalProperty.Global

namespace TiVkParser.Models.Exports;

public record OutLike(
    string? UserId, 
    string? GroupId, 
    string? PostId,
    string? PostText,
    string? Url
);