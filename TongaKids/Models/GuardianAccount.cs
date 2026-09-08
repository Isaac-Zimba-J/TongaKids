using SQLite;

namespace TongaKids.Models;

/// <summary>
/// The parent or guardian who set the app up. One per device.
/// Report section 3.4.2 requires registration; the child's path has none (spec 3.2).
/// </summary>
[Table("GuardianAccount")]
public class GuardianAccount
{
    [PrimaryKey, AutoIncrement] public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    /// <summary>Email address or phone number, whichever the guardian gave.</summary>
    [Indexed] public string Contact { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string PasswordSalt { get; set; } = string.Empty;
    public string SecurityAnswerHash { get; set; } = string.Empty;
    public string SecurityAnswerSalt { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
