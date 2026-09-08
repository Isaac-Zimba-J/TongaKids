using SQLite;

namespace TongaKids.Models;

[Table("LessonProgress")]
public class LessonProgress
{
    [PrimaryKey, AutoIncrement] public int Id { get; set; }
    [Indexed] public int LearnerId { get; set; }
    [Indexed] public int LessonId { get; set; }
    public int StarsEarned { get; set; }
    public int BestScorePercent { get; set; }
    public bool IsMastered { get; set; }
    public DateTime? CompletedAt { get; set; }
}
