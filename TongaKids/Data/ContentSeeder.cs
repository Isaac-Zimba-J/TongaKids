using System.Text.Json;
using TongaKids.Models;

namespace TongaKids.Data;

public interface IContentSeeder
{
    Task<bool> SeedIfNeededAsync();
}

public sealed class ContentSeeder(ITongaKidsDatabase database) : IContentSeeder
{
    private const string AssetPath = "content/chitonga-content.json";
    private const string VersionKey = "content_version";

    public async Task<bool> SeedIfNeededAsync()
    {
        ContentPack? pack;
        try
        {
            await using var stream = await FileSystem.OpenAppPackageFileAsync(AssetPath);
            pack = await JsonSerializer.DeserializeAsync<ContentPack>(stream);
        }
        catch (Exception ex)
        {
            // No content pack means an empty library, not a crash.
            System.Diagnostics.Debug.WriteLine($"[ContentSeeder] could not read pack: {ex}");
            return false;
        }

        if (pack is null)
        {
            return false;
        }

        var installed = Preferences.Default.Get(VersionKey, 0);
        if (installed >= pack.Version)
        {
            return false;
        }

        var db = await database.GetConnectionAsync();

        // Content tables only. Learner progress is never touched by seeding.
        await db.DeleteAllAsync<StoryPage>();
        await db.DeleteAllAsync<Story>();
        await db.DeleteAllAsync<PhonicsItem>();
        await db.DeleteAllAsync<Lesson>();
        await db.DeleteAllAsync<Level>();

        foreach (var level in pack.Levels)
        {
            if (level.Id <= 0 || string.IsNullOrWhiteSpace(level.Title))
            {
                System.Diagnostics.Debug.WriteLine($"[ContentSeeder] skipped level {level.Id}");
                continue;
            }

            await db.InsertAsync(new Level
            {
                Id = level.Id,
                Number = level.Number,
                Title = level.Title,
                Subtitle = level.Subtitle,
                IconGlyph = level.IconGlyph,
                RequiresLevelNumber = level.RequiresLevelNumber
            });

            foreach (var lesson in level.Lessons)
            {
                if (lesson.Id <= 0)
                {
                    continue;
                }

                await db.InsertAsync(new Lesson
                {
                    Id = lesson.Id,
                    LevelId = level.Id,
                    Number = lesson.Number,
                    Title = lesson.Title
                });

                foreach (var item in lesson.Items)
                {
                    if (item.Id <= 0 || string.IsNullOrWhiteSpace(item.Grapheme))
                    {
                        continue;
                    }

                    await db.InsertAsync(new PhonicsItem
                    {
                        Id = item.Id,
                        LessonId = lesson.Id,
                        Grapheme = item.Grapheme,
                        AudioKey = item.AudioKey,
                        ExampleWord = item.ExampleWord,
                        Gloss = item.Gloss,
                        ImageKey = item.ImageKey,
                        SortOrder = item.SortOrder
                    });
                }
            }
        }

        foreach (var story in pack.Stories)
        {
            if (story.Id <= 0 || string.IsNullOrWhiteSpace(story.Title))
            {
                continue;
            }

            await db.InsertAsync(new Story
            {
                Id = story.Id,
                Title = story.Title,
                CoverImageKey = story.CoverImageKey,
                SortOrder = story.SortOrder
            });

            foreach (var page in story.Pages)
            {
                if (page.Id <= 0)
                {
                    continue;
                }

                await db.InsertAsync(new StoryPage
                {
                    Id = page.Id,
                    StoryId = story.Id,
                    PageNumber = page.PageNumber,
                    Text = page.Text,
                    ImageKey = page.ImageKey,
                    AudioKey = page.AudioKey,
                    // Derived, never authored: a hand-typed count would let
                    // words-per-minute disagree with what is on the page.
                    WordCount = page.Text.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length
                });
            }
        }

        Preferences.Default.Set(VersionKey, pack.Version);
        return true;
    }
}
