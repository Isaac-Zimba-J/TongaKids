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
}
