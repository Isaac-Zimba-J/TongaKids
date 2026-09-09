using SQLite;

namespace TongaKids.Models;

[Table("Level")]
public class Level
{
    [PrimaryKey] public int Id { get; set; }
    public int Number { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Subtitle { get; set; } = string.Empty;
    /// <summary>Material Symbols ligature, e.g. "music_note".</summary>
    public string IconGlyph { get; set; } = string.Empty;
    /// <summary>Level number that must be mastered first. 0 means always unlocked.</summary>
    public int RequiresLevelNumber { get; set; }

    /// <summary>True for content authored in the app; seeding never replaces it.</summary>
    public bool IsUserCreated { get; set; }
}
