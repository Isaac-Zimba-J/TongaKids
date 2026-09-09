using TongaKids.Data;

namespace TongaKids.Services;

/// <summary>
/// One place computes every reported figure, so the child's progress screen and
/// the parent dashboard cannot drift apart.
/// </summary>
public sealed class ProgressAnalytics(
    IProgressRepository progress,
    IStoryRepository stories,
    IContentRepository content,
    IProgressCalculator calculator) : IProgressAnalytics
{
    public async Task<LearnerStats> ForLearnerAsync(int learnerId)
    {
        try
        {
            var lessonProgress = await progress.GetForLearnerAsync(learnerId);

            var lessonsTotal = 0;
            var lessonTitles = new Dictionary<int, string>();
            foreach (var level in await content.GetLevelsAsync())
            {
                foreach (var lesson in await content.GetLessonsAsync(level.Id))
                {
                    lessonsTotal++;
                    lessonTitles[lesson.Id] = lesson.Title;
                }
            }

            // Mean of each attempted lesson's best score. Lessons never attempted
            // are excluded rather than counted as zero: a parent should see how
            // the child did at what they tried, not be punished for untouched content.
            var accuracy = lessonProgress.Count == 0
                ? 0
                : lessonProgress.Sum(p => p.BestScorePercent) / lessonProgress.Count;

            var sessions = await stories.GetSessionsAsync(learnerId);
            var wpm = calculator.WordsPerMinute(
                sessions.Sum(s => s.WordsRead),
                sessions.Sum(s => s.DurationMs));

            var weakest = lessonProgress.OrderBy(p => p.BestScorePercent).FirstOrDefault();
            var weakestTitle = weakest is null
                ? "Nothing yet"
                : lessonTitles.GetValueOrDefault(weakest.LessonId, "A lesson");

            return new LearnerStats(
                LessonsMastered: lessonProgress.Count(p => p.IsMastered),
                LessonsTotal: lessonsTotal,
                StarsEarned: lessonProgress.Sum(p => p.StarsEarned),
                AccuracyPercent: accuracy,
                WordsPerMinute: wpm,
                StoriesRead: sessions.Select(s => s.StoryId).Distinct().Count(),
                WeakestLessonTitle: weakestTitle);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Analytics] failed: {ex}");
            return new LearnerStats(0, 0, 0, 0, 0, 0, "Nothing yet");
        }
    }
}
