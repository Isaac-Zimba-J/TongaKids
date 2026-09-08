using TongaKids.Models;

namespace TongaKids.Data;

public interface IContentRepository
{
    Task<List<Level>> GetLevelsAsync();
    Task<List<Lesson>> GetLessonsAsync(int levelId);
    Task<Lesson?> GetLessonAsync(int lessonId);
    Task<List<PhonicsItem>> GetItemsAsync(int lessonId);
    Task<List<PhonicsItem>> GetAllItemsAsync();
}

public sealed class ContentRepository(ITongaKidsDatabase database) : IContentRepository
{
    public async Task<List<Level>> GetLevelsAsync()
    {
        var db = await database.GetConnectionAsync();
        return await db.Table<Level>().OrderBy(l => l.Number).ToListAsync();
    }

    public async Task<List<Lesson>> GetLessonsAsync(int levelId)
    {
        var db = await database.GetConnectionAsync();
        return await db.Table<Lesson>()
            .Where(l => l.LevelId == levelId)
            .OrderBy(l => l.Number)
            .ToListAsync();
    }

    public async Task<Lesson?> GetLessonAsync(int lessonId)
    {
        var db = await database.GetConnectionAsync();
        return await db.Table<Lesson>().Where(l => l.Id == lessonId).FirstOrDefaultAsync();
    }

    public async Task<List<PhonicsItem>> GetItemsAsync(int lessonId)
    {
        var db = await database.GetConnectionAsync();
        return await db.Table<PhonicsItem>()
            .Where(i => i.LessonId == lessonId)
            .OrderBy(i => i.SortOrder)
            .ToListAsync();
    }

    public async Task<List<PhonicsItem>> GetAllItemsAsync()
    {
        var db = await database.GetConnectionAsync();
        return await db.Table<PhonicsItem>().ToListAsync();
    }
}
