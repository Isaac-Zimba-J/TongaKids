using System.Text.Json.Serialization;

namespace TongaKids.Models;

public sealed class ContentPack
{
    [JsonPropertyName("version")] public int Version { get; set; }
    [JsonPropertyName("levels")] public List<ContentLevel> Levels { get; set; } = [];
    [JsonPropertyName("stories")] public List<ContentStory> Stories { get; set; } = [];
}

public sealed class ContentLevel
{
    [JsonPropertyName("id")] public int Id { get; set; }
    [JsonPropertyName("number")] public int Number { get; set; }
    [JsonPropertyName("title")] public string Title { get; set; } = string.Empty;
    [JsonPropertyName("subtitle")] public string Subtitle { get; set; } = string.Empty;
    [JsonPropertyName("iconGlyph")] public string IconGlyph { get; set; } = string.Empty;
    [JsonPropertyName("requiresLevelNumber")] public int RequiresLevelNumber { get; set; }
    [JsonPropertyName("lessons")] public List<ContentLesson> Lessons { get; set; } = [];
}

public sealed class ContentLesson
{
    [JsonPropertyName("id")] public int Id { get; set; }
    [JsonPropertyName("number")] public int Number { get; set; }
    [JsonPropertyName("title")] public string Title { get; set; } = string.Empty;
    [JsonPropertyName("items")] public List<ContentItem> Items { get; set; } = [];
}

public sealed class ContentItem
{
    [JsonPropertyName("id")] public int Id { get; set; }
    [JsonPropertyName("grapheme")] public string Grapheme { get; set; } = string.Empty;
    [JsonPropertyName("audioKey")] public string AudioKey { get; set; } = string.Empty;
    [JsonPropertyName("exampleWord")] public string ExampleWord { get; set; } = string.Empty;
    [JsonPropertyName("gloss")] public string Gloss { get; set; } = string.Empty;
    [JsonPropertyName("imageKey")] public string ImageKey { get; set; } = string.Empty;
    [JsonPropertyName("sortOrder")] public int SortOrder { get; set; }
}

public sealed class ContentStory
{
    [JsonPropertyName("id")] public int Id { get; set; }
    [JsonPropertyName("title")] public string Title { get; set; } = string.Empty;
    [JsonPropertyName("coverImageKey")] public string CoverImageKey { get; set; } = string.Empty;
    [JsonPropertyName("sortOrder")] public int SortOrder { get; set; }
    [JsonPropertyName("pages")] public List<ContentStoryPage> Pages { get; set; } = [];
}

public sealed class ContentStoryPage
{
    [JsonPropertyName("id")] public int Id { get; set; }
    [JsonPropertyName("pageNumber")] public int PageNumber { get; set; }
    [JsonPropertyName("text")] public string Text { get; set; } = string.Empty;
    [JsonPropertyName("imageKey")] public string ImageKey { get; set; } = string.Empty;
    [JsonPropertyName("audioKey")] public string AudioKey { get; set; } = string.Empty;
}
