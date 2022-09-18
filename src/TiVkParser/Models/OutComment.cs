namespace TiVkParser.Models;

public record OutComment(
    string UserId, 
    string GroupId, 
    string PostId, 
    string CommentId, 
    string CommentText
);