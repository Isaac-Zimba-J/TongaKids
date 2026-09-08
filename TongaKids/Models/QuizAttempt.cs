using SQLite;

namespace TongaKids.Models;

/// <summary>
/// One completed quiz. These raw rows are what the ProgressCalculator
/// aggregates — without them, reported accuracy would have nothing behind it.
/// </summary>
[Table("QuizAttempt")]
public class QuizAttempt
{
    [PrimaryKey, AutoIncrement] public int Id { get; set; }
    [Indexed] public int LearnerId { get; set; }
    [Indexed] public int LessonId { get; set; }
    public int CorrectCount { get; set; }
    public int TotalCount { get; set; }
    public long DurationMs { get; set; }
    public DateTime AttemptedAt { get; set; }
}
