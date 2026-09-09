using SQLite;

namespace TongaKids.Models;

[Table("StoryReadState")]
public class StoryReadState
{
    [PrimaryKey, AutoIncrement] public int Id { get; set; }
    [Indexed] public int LearnerId { get; set; }
    [Indexed] public int StoryId { get; set; }
    public int LastPageRead { get; set; }
    public bool IsCompleted { get; set; }
}
