// ReSharper disable NotAccessedPositionalProperty.Global

namespace TiVkParser.Models.Exports;

public record OutComment(
    string? UserId, 
    string? GroupId, 
    string? PostId, 
    string? PostText,
    string? CommentId, 
    string? CommentText,
    string? UrlPost,
    string? UrlComment
);