using SQLite;

namespace TongaKids.Models;

/// <summary>
/// One reading sitting. Feeds the words-per-minute figure on the parent dashboard.
/// </summary>
[Table("ReadingSession")]
public class ReadingSession
{
    [PrimaryKey, AutoIncrement] public int Id { get; set; }
    [Indexed] public int LearnerId { get; set; }
    [Indexed] public int StoryId { get; set; }
    public int WordsRead { get; set; }
    public long DurationMs { get; set; }
    public DateTime StartedAt { get; set; }
}
