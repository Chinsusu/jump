namespace ShadowFox.UI.Models;

public class GroupItem
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int ProfilesCount { get; set; }
    public bool IsSelected { get; set; }
}
