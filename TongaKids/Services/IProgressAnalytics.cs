namespace TongaKids.Services;

public sealed record LearnerStats(
    int LessonsMastered,
    int LessonsTotal,
    int StarsEarned,
    int AccuracyPercent,
    double WordsPerMinute,
    int StoriesRead,
    string WeakestLessonTitle);

public interface IProgressAnalytics
{
    Task<LearnerStats> ForLearnerAsync(int learnerId);
}
