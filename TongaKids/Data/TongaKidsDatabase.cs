using SQLite;
using TongaKids.Models;

namespace TongaKids.Data;

public interface ITongaKidsDatabase
{
    Task<SQLiteAsyncConnection> GetConnectionAsync();
}

public sealed class TongaKidsDatabase : ITongaKidsDatabase
{
    public const string FileName = "tongakids.db3";

    private const SQLiteOpenFlags Flags =
        SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.Create | SQLiteOpenFlags.SharedCache;

    private SQLiteAsyncConnection? _connection;
    private readonly SemaphoreSlim _gate = new(1, 1);

    public async Task<SQLiteAsyncConnection> GetConnectionAsync()
    {
        if (_connection is not null)
        {
            return _connection;
        }

        await _gate.WaitAsync();
        try
        {
            if (_connection is not null)
            {
                return _connection;
            }

            var path = Path.Combine(FileSystem.AppDataDirectory, FileName);
            var connection = new SQLiteAsyncConnection(path, Flags);

            // WAL keeps reads fast while progress is being written.
            await connection.ExecuteAsync("PRAGMA journal_mode=WAL;");

            await connection.CreateTableAsync<Learner>();
            await connection.CreateTableAsync<Level>();
            await connection.CreateTableAsync<Lesson>();
            await connection.CreateTableAsync<PhonicsItem>();
            await connection.CreateTableAsync<LessonProgress>();
            await connection.CreateTableAsync<QuizAttempt>();

            _connection = connection;
            return _connection;
        }
        finally
        {
            _gate.Release();
        }
    }
}
