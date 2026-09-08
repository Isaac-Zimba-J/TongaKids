using SQLite;

namespace TongaKids.Models;

[Table("PhonicsItem")]
public class PhonicsItem
{
    [PrimaryKey] public int Id { get; set; }
    [Indexed] public int LessonId { get; set; }
    /// <summary>The letter or syllable shown on the card, e.g. "BA".</summary>
    public string Grapheme { get; set; } = string.Empty;
    /// <summary>Audio filename without extension, e.g. "syl_ba".</summary>
    public string AudioKey { get; set; } = string.Empty;
    public string ExampleWord { get; set; } = string.Empty;
    /// <summary>English gloss, parent-facing support text.</summary>
    public string Gloss { get; set; } = string.Empty;
    public string ImageKey { get; set; } = string.Empty;
    public int SortOrder { get; set; }
}
