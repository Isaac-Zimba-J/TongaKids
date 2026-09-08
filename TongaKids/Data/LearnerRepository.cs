using TongaKids.Models;

namespace TongaKids.Data;

public interface ILearnerRepository
{
    Task<List<Learner>> GetAllAsync();
    Task<Learner> AddAsync(Learner learner);
    Task TouchAsync(int learnerId);
}

public sealed class LearnerRepository(ITongaKidsDatabase database) : ILearnerRepository
{
    public async Task<List<Learner>> GetAllAsync()
    {
        var db = await database.GetConnectionAsync();
        return await db.Table<Learner>().OrderBy(l => l.Id).ToListAsync();
    }

    public async Task<Learner> AddAsync(Learner learner)
    {
        var db = await database.GetConnectionAsync();
        learner.CreatedAt = DateTime.UtcNow;
        learner.LastActiveAt = DateTime.UtcNow;
        await db.InsertAsync(learner);
        return learner;
    }

    public async Task TouchAsync(int learnerId)
    {
        var db = await database.GetConnectionAsync();
        await db.ExecuteAsync(
            "UPDATE Learner SET LastActiveAt = ? WHERE Id = ?", DateTime.UtcNow, learnerId);
    }
}
