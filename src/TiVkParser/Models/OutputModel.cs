namespace TiVkParser.Models;

public record OutputModel(
    string UserId, 
    string GroupId, 
    string PostId, 
    string CommentId, 
    string CommentText
);