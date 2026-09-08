using SQLite;

namespace TongaKids.Models;

[Table("Learner")]
public class Learner
{
    [PrimaryKey, AutoIncrement] public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    /// <summary>Image resource name without extension, e.g. "avatar_chipo".</summary>
    public string AvatarKey { get; set; } = string.Empty;
    /// <summary>Colour resource key for the profile ring, e.g. "Tertiary".</summary>
    public string AccentColorKey { get; set; } = "Tertiary";
    public DateTime CreatedAt { get; set; }
    public DateTime LastActiveAt { get; set; }
}
