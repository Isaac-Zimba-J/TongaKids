using TongaKids.Models;

namespace TongaKids.Data;

public interface IContentRepository
{
    Task<List<Level>> GetLevelsAsync();
    Task<List<Lesson>> GetLessonsAsync(int levelId);
    Task<Lesson?> GetLessonAsync(int lessonId);
    Task<List<PhonicsItem>> GetItemsAsync(int lessonId);
    Task<List<PhonicsItem>> GetAllItemsAsync();

    Task<Level> CreateLevelAsync(string title, string subtitle);
    Task<Lesson> CreateLessonAsync(int levelId, string title);
    Task<PhonicsItem> CreateItemAsync(int lessonId, string grapheme);

    Task SaveItemAsync(PhonicsItem item);

    Task DeleteLevelAsync(int levelId);
    Task DeleteLessonAsync(int lessonId);
    Task DeleteItemAsync(int itemId);
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

    // Authored ids sit well above the pack's, so the ranges can never collide.
    private const int FirstAuthoredLevelId = 1_000;
    private const int FirstAuthoredLessonId = 10_000;
    private const int FirstAuthoredItemId = 100_000;

    public async Task<Level> CreateLevelAsync(string title, string subtitle)
    {
        var db = await database.GetConnectionAsync();

        var maxId = await db.ExecuteScalarAsync<int>(
            "SELECT IFNULL(MAX(Id), 0) FROM Level WHERE Id >= ?", FirstAuthoredLevelId);
        var maxNumber = await db.ExecuteScalarAsync<int>("SELECT IFNULL(MAX(Number), 0) FROM Level");

        var level = new Level
        {
            Id = Math.Max(maxId + 1, FirstAuthoredLevelId),
            Number = maxNumber + 1,
            Title = title,
            Subtitle = subtitle,
            IconGlyph = "music_note",
            // Unlocked from the start: a guardian adding a level wants it used.
            RequiresLevelNumber = 0,
            IsUserCreated = true
        };

        await db.InsertAsync(level);
        return level;
    }

    public async Task<Lesson> CreateLessonAsync(int levelId, string title)
    {
        var db = await database.GetConnectionAsync();

        var maxId = await db.ExecuteScalarAsync<int>(
            "SELECT IFNULL(MAX(Id), 0) FROM Lesson WHERE Id >= ?", FirstAuthoredLessonId);
        var nextNumber = await db.ExecuteScalarAsync<int>(
            "SELECT IFNULL(MAX(Number), 0) + 1 FROM Lesson WHERE LevelId = ?", levelId);

        var lesson = new Lesson
        {
            Id = Math.Max(maxId + 1, FirstAuthoredLessonId),
            LevelId = levelId,
            Number = nextNumber,
            Title = title,
            IsUserCreated = true
        };

        await db.InsertAsync(lesson);
        return lesson;
    }

    public async Task<PhonicsItem> CreateItemAsync(int lessonId, string grapheme)
    {
        var db = await database.GetConnectionAsync();

        var maxId = await db.ExecuteScalarAsync<int>(
            "SELECT IFNULL(MAX(Id), 0) FROM PhonicsItem WHERE Id >= ?", FirstAuthoredItemId);
        var nextOrder = await db.ExecuteScalarAsync<int>(
            "SELECT IFNULL(MAX(SortOrder), 0) + 1 FROM PhonicsItem WHERE LessonId = ?", lessonId);

        var item = new PhonicsItem
        {
            Id = Math.Max(maxId + 1, FirstAuthoredItemId),
            LessonId = lessonId,
            Grapheme = grapheme,
            SortOrder = nextOrder,
            IsUserCreated = true
        };

        // Derived from the id so it survives reordering.
        item.AudioKey = $"uitem{item.Id}";

        await db.InsertAsync(item);
        return item;
    }

    public async Task SaveItemAsync(PhonicsItem item)
    {
        var db = await database.GetConnectionAsync();
        await db.UpdateAsync(item);
    }

    public async Task DeleteLevelAsync(int levelId)
    {
        var db = await database.GetConnectionAsync();

        var level = await db.Table<Level>().Where(l => l.Id == levelId).FirstOrDefaultAsync();
        if (level is null)
        {
            return;
        }

        foreach (var lesson in await db.Table<Lesson>().Where(l => l.LevelId == levelId).ToListAsync())
        {
            await DeleteLessonAsync(lesson.Id);
        }

        await db.ExecuteAsync("DELETE FROM Level WHERE Id = ?", levelId);
        await TombstoneAsync(db, level.IsUserCreated, "level", levelId);
    }

    public async Task DeleteLessonAsync(int lessonId)
    {
        var db = await database.GetConnectionAsync();

        var lesson = await db.Table<Lesson>().Where(l => l.Id == lessonId).FirstOrDefaultAsync();
        if (lesson is null)
        {
            return;
        }

        foreach (var item in await db.Table<PhonicsItem>().Where(i => i.LessonId == lessonId).ToListAsync())
        {
            await DeleteItemAsync(item.Id);
        }

        await db.ExecuteAsync("DELETE FROM Lesson WHERE Id = ?", lessonId);
        await TombstoneAsync(db, lesson.IsUserCreated, "lesson", lessonId);
    }

    public async Task DeleteItemAsync(int itemId)
    {
        var db = await database.GetConnectionAsync();

        var item = await db.Table<PhonicsItem>().Where(i => i.Id == itemId).FirstOrDefaultAsync();
        if (item is null)
        {
            return;
        }

        await db.ExecuteAsync("DELETE FROM PhonicsItem WHERE Id = ?", itemId);
        await TombstoneAsync(db, item.IsUserCreated, "item", itemId);
    }

    /// <summary>Pack content must stay deleted; authored content needs no marker.</summary>
    private static async Task TombstoneAsync(
        SQLite.SQLiteAsyncConnection db, bool isUserCreated, string kind, int id)
    {
        if (isUserCreated)
        {
            return;
        }

        await db.InsertAsync(new DeletedSeedItem
        {
            Kind = kind,
            SeedId = id,
            DeletedAt = DateTime.UtcNow
        });
    }
}
