namespace TiVkParser.Models.SaveDataModels;

public record OutComment(
    string UserId, 
    string GroupId, 
    string PostId, 
    string CommentId, 
    string CommentText
);