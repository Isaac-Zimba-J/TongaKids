using SQLite;

namespace TongaKids.Models;

[Table("Story")]
public class Story
{
    [PrimaryKey] public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string CoverImageKey { get; set; } = string.Empty;
    public int SortOrder { get; set; }
}
