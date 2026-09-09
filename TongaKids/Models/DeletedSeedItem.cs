using SQLite;

namespace TongaKids.Models;

/// <summary>
/// A tombstone for content that came from the pack and was deleted in the app.
/// Without this, the next content update would silently restore anything the
/// guardian had removed.
/// </summary>
[Table("DeletedSeedItem")]
public class DeletedSeedItem
{
    [PrimaryKey, AutoIncrement] public int RowId { get; set; }

    /// <summary>"story", "level", "lesson" or "item".</summary>
    [Indexed] public string Kind { get; set; } = string.Empty;

    [Indexed] public int SeedId { get; set; }

    public DateTime DeletedAt { get; set; }
}
