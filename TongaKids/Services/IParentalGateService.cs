using TongaKids.Models;

namespace TongaKids.Services;

public interface IParentalGateService
{
    /// <summary>In-memory only: closing the app relocks the parent area.</summary>
    bool IsUnlocked { get; }

    Task<bool> HasPinAsync();
    Task SetPinAsync(string pin);
    Task<bool> VerifyPinAsync(string pin);
    void Lock();

    Task<ParentSettings> GetSettingsAsync();
    Task SaveSettingsAsync(ParentSettings settings);
}
