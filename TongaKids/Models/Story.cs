using SQLite;

namespace TongaKids.Models;

[Table("Story")]
public class Story
{
    [PrimaryKey] public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string CoverImageKey { get; set; } = string.Empty;
    public int SortOrder { get; set; }

    /// <summary>
    /// True for stories written inside the app. Seeding replaces only the
    /// stories that came from the content pack, so authored work survives a
    /// content update.
    /// </summary>
    public bool IsUserCreated { get; set; }
}
