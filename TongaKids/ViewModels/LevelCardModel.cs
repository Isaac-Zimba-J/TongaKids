namespace TongaKids.ViewModels;

/// <summary>A level as the levels screen needs it: content joined to this learner's progress.</summary>
public sealed class LevelCardModel
{
    public int Number { get; init; }
    public int FirstLessonId { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Subtitle { get; init; } = string.Empty;
    public string IconGlyph { get; init; } = string.Empty;
    public bool IsLocked { get; init; }
    public bool IsCurrent { get; init; }
    public bool IsComplete { get; init; }
    public double ProgressFraction { get; init; }
    public int Stars { get; init; }

    public bool IsUnlocked => !IsLocked;
    public bool HasLessons => FirstLessonId > 0;
    public string ProgressLabel => $"Progress: {ProgressFraction * 100:0}%";
    public string ActionLabel => IsComplete ? "Review" : "PLAY";
    public string LockedLabel => $"Complete Level {Number - 1} to unlock";
}
