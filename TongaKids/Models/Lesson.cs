using SQLite;

namespace TongaKids.Models;

[Table("Lesson")]
public class Lesson
{
    [PrimaryKey] public int Id { get; set; }
    [Indexed] public int LevelId { get; set; }
    public int Number { get; set; }
    public string Title { get; set; } = string.Empty;
}
