namespace TiVkParser.Models;

public record CommentModel(
    string UserId, 
    string GroupId, 
    string PostId, 
    string CommentId, 
    string CommentText
);