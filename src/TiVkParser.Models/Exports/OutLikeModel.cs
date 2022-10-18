// ReSharper disable NotAccessedPositionalProperty.Global

namespace TiVkParser.Models.Exports;

public record OutLikeModel(
    string? UserId, 
    string? GroupId, 
    string? PostId,
    string? PostText,
    string? Url
);