using SQLite;

namespace TongaKids.Models;

[Table("ParentSettings")]
public class ParentSettings
{
    [PrimaryKey, AutoIncrement] public int Id { get; set; }
    public string PinHash { get; set; } = string.Empty;
    public string PinSalt { get; set; } = string.Empty;
    public DateTime? ConsentGivenAt { get; set; }
    public int ConsentVersion { get; set; }
    public bool SyncEnabled { get; set; }
}
