using TongaKids.Models;

namespace TongaKids.Data;

public interface IStoryRepository
{
    Task<List<Story>> GetStoriesAsync();
    Task<List<StoryPage>> GetPagesAsync(int storyId);
    Task<StoryReadState?> GetReadStateAsync(int learnerId, int storyId);
    Task SaveReadStateAsync(StoryReadState state);
    Task AddSessionAsync(ReadingSession session);
    Task<List<ReadingSession>> GetSessionsAsync(int learnerId);

    Task<Story?> GetStoryAsync(int storyId);

    /// <summary>Creates an empty authored story and returns it.</summary>
    Task<Story> CreateStoryAsync(string title);

    Task SaveStoryAsync(Story story);

    /// <summary>Removes a story and its pages. Authored stories only.</summary>
    Task DeleteStoryAsync(int storyId);

    /// <summary>Appends a page to a story and returns it.</summary>
    Task<StoryPage> AddPageAsync(int storyId);

    Task SavePageAsync(StoryPage page);

    /// <summary>Deletes a page and renumbers the ones after it.</summary>
    Task DeletePageAsync(int pageId);
}

public sealed class StoryRepository(ITongaKidsDatabase database) : IStoryRepository
{
    public async Task<List<Story>> GetStoriesAsync()
    {
        var db = await database.GetConnectionAsync();
        return await db.Table<Story>().OrderBy(s => s.SortOrder).ToListAsync();
    }

    public async Task<List<StoryPage>> GetPagesAsync(int storyId)
    {
        var db = await database.GetConnectionAsync();
        return await db.Table<StoryPage>()
            .Where(p => p.StoryId == storyId)
            .OrderBy(p => p.PageNumber)
            .ToListAsync();
    }

    public async Task<StoryReadState?> GetReadStateAsync(int learnerId, int storyId)
    {
        var db = await database.GetConnectionAsync();
        return await db.Table<StoryReadState>()
            .Where(s => s.LearnerId == learnerId && s.StoryId == storyId)
            .FirstOrDefaultAsync();
    }

    public async Task SaveReadStateAsync(StoryReadState state)
    {
        var db = await database.GetConnectionAsync();
        if (state.Id == 0)
        {
            await db.InsertAsync(state);
        }
        else
        {
            await db.UpdateAsync(state);
        }
    }

    public async Task AddSessionAsync(ReadingSession session)
    {
        var db = await database.GetConnectionAsync();
        await db.InsertAsync(session);
    }

    public async Task<List<ReadingSession>> GetSessionsAsync(int learnerId)
    {
        var db = await database.GetConnectionAsync();
        return await db.Table<ReadingSession>().Where(s => s.LearnerId == learnerId).ToListAsync();
    }

    // Authored content starts well above the pack's ids so the two can never
    // collide, however much the pack grows.
    private const int FirstAuthoredStoryId = 10_000;
    private const int FirstAuthoredPageId = 100_000;

    public async Task<Story?> GetStoryAsync(int storyId)
    {
        var db = await database.GetConnectionAsync();
        return await db.Table<Story>().Where(s => s.Id == storyId).FirstOrDefaultAsync();
    }

    public async Task<Story> CreateStoryAsync(string title)
    {
        var db = await database.GetConnectionAsync();

        var maxId = await db.ExecuteScalarAsync<int>(
            "SELECT IFNULL(MAX(Id), 0) FROM Story WHERE Id >= ?", FirstAuthoredStoryId);
        var maxOrder = await db.ExecuteScalarAsync<int>("SELECT IFNULL(MAX(SortOrder), 0) FROM Story");

        var story = new Story
        {
            Id = Math.Max(maxId + 1, FirstAuthoredStoryId),
            Title = title,
            CoverImageKey = "illus_splash_baobab",
            SortOrder = maxOrder + 1,
            IsUserCreated = true
        };

        await db.InsertAsync(story);
        return story;
    }

    public async Task SaveStoryAsync(Story story)
    {
        var db = await database.GetConnectionAsync();
        await db.UpdateAsync(story);
    }

    public async Task DeleteStoryAsync(int storyId)
    {
        var db = await database.GetConnectionAsync();

        var story = await db.Table<Story>().Where(s => s.Id == storyId).FirstOrDefaultAsync();
        if (story is null)
        {
            return;
        }

        await db.ExecuteAsync("DELETE FROM StoryPage WHERE StoryId = ?", storyId);
        await db.ExecuteAsync("DELETE FROM Story WHERE Id = ?", storyId);

        // A pack story must stay deleted, or the next content update restores it.
        if (!story.IsUserCreated)
        {
            await db.InsertAsync(new DeletedSeedItem
            {
                Kind = "story",
                SeedId = storyId,
                DeletedAt = DateTime.UtcNow
            });
        }
    }

    public async Task<StoryPage> AddPageAsync(int storyId)
    {
        var db = await database.GetConnectionAsync();

        var maxId = await db.ExecuteScalarAsync<int>(
            "SELECT IFNULL(MAX(Id), 0) FROM StoryPage WHERE Id >= ?", FirstAuthoredPageId);
        var nextNumber = await db.ExecuteScalarAsync<int>(
            "SELECT IFNULL(MAX(PageNumber), 0) + 1 FROM StoryPage WHERE StoryId = ?", storyId);

        var page = new StoryPage
        {
            Id = Math.Max(maxId + 1, FirstAuthoredPageId),
            StoryId = storyId,
            PageNumber = nextNumber,
            Text = string.Empty,
            ImageKey = "illus_splash_baobab",
            WordCount = 0
        };

        // The audio key is derived from the page id, so it stays stable even if
        // pages are renumbered by a deletion.
        page.AudioKey = $"ustory{storyId}_p{page.Id}";

        await db.InsertAsync(page);
        return page;
    }

    public async Task SavePageAsync(StoryPage page)
    {
        var db = await database.GetConnectionAsync();

        // Derived, never authored, so words-per-minute always matches the page.
        page.WordCount = page.Text.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;

        await db.UpdateAsync(page);
    }

    public async Task DeletePageAsync(int pageId)
    {
        var db = await database.GetConnectionAsync();

        var page = await db.Table<StoryPage>().Where(p => p.Id == pageId).FirstOrDefaultAsync();
        if (page is null)
        {
            return;
        }

        await db.ExecuteAsync("DELETE FROM StoryPage WHERE Id = ?", pageId);

        // Close the gap so page numbers stay 1..n.
        await db.ExecuteAsync(
            "UPDATE StoryPage SET PageNumber = PageNumber - 1 WHERE StoryId = ? AND PageNumber > ?",
            page.StoryId, page.PageNumber);
    }
}
