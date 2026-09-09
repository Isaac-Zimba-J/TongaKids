using SQLite;

namespace TongaKids.Models;

[Table("StoryPage")]
public class StoryPage
{
    [PrimaryKey] public int Id { get; set; }
    [Indexed] public int StoryId { get; set; }
    public int PageNumber { get; set; }
    public string Text { get; set; } = string.Empty;
    public string ImageKey { get; set; } = string.Empty;
    public string AudioKey { get; set; } = string.Empty;
    /// <summary>Derived at seed time, never authored, so words-per-minute cannot lie.</summary>
    public int WordCount { get; set; }
}
