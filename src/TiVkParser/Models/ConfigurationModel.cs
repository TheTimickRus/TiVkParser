namespace TiVkParser.Models;

public record ConfigurationModel
{
    public string AccessToken { get; set; } = string.Empty;
    public List<string> UserIds { get; set; } = new();
    public List<string> GroupIds { get; set; } = new();
    public bool IsGroupsFromUser { get; set; }
}