using TongaKids.Models;

namespace TongaKids.Data;

public interface IProgressRepository
{
    Task<List<LessonProgress>> GetForLearnerAsync(int learnerId);
    Task<LessonProgress?> GetForLessonAsync(int learnerId, int lessonId);
    Task SaveAsync(LessonProgress progress);
    Task AddAttemptAsync(QuizAttempt attempt);
}

public sealed class ProgressRepository(ITongaKidsDatabase database) : IProgressRepository
{
    public async Task<List<LessonProgress>> GetForLearnerAsync(int learnerId)
    {
        var db = await database.GetConnectionAsync();
        return await db.Table<LessonProgress>().Where(p => p.LearnerId == learnerId).ToListAsync();
    }

    public async Task<LessonProgress?> GetForLessonAsync(int learnerId, int lessonId)
    {
        var db = await database.GetConnectionAsync();
        return await db.Table<LessonProgress>()
            .Where(p => p.LearnerId == learnerId && p.LessonId == lessonId)
            .FirstOrDefaultAsync();
    }

    public async Task SaveAsync(LessonProgress progress)
    {
        var db = await database.GetConnectionAsync();
        if (progress.Id == 0)
        {
            await db.InsertAsync(progress);
        }
        else
        {
            await db.UpdateAsync(progress);
        }
    }

    public async Task AddAttemptAsync(QuizAttempt attempt)
    {
        var db = await database.GetConnectionAsync();
        await db.InsertAsync(attempt);
    }
}
