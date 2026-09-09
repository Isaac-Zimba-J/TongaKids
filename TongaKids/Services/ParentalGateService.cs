using System.Security.Cryptography;
using System.Text;
using TongaKids.Data;
using TongaKids.Models;

namespace TongaKids.Services;

public sealed class ParentalGateService(ITongaKidsDatabase database) : IParentalGateService
{
    private const int Iterations = 100_000;

    public bool IsUnlocked { get; private set; }

    public async Task<bool> HasPinAsync() =>
        !string.IsNullOrEmpty((await GetSettingsAsync()).PinHash);

    public async Task SetPinAsync(string pin)
    {
        var settings = await GetSettingsAsync();
        var salt = RandomNumberGenerator.GetBytes(16);
        settings.PinSalt = Convert.ToBase64String(salt);
        settings.PinHash = Convert.ToBase64String(Derive(pin, salt));
        await SaveSettingsAsync(settings);
        IsUnlocked = true;
    }

    public async Task<bool> VerifyPinAsync(string pin)
    {
        var settings = await GetSettingsAsync();
        if (string.IsNullOrEmpty(settings.PinHash))
        {
            return false;
        }

        var salt = Convert.FromBase64String(settings.PinSalt);

        // Constant time: never leak how much of the PIN matched.
        var ok = CryptographicOperations.FixedTimeEquals(
            Derive(pin, salt), Convert.FromBase64String(settings.PinHash));

        IsUnlocked = ok;
        return ok;
    }

    public void Lock() => IsUnlocked = false;

    public async Task<ParentSettings> GetSettingsAsync()
    {
        try
        {
            var db = await database.GetConnectionAsync();
            return await db.Table<ParentSettings>().FirstOrDefaultAsync() ?? new ParentSettings();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Gate] settings load failed: {ex.Message}");
            return new ParentSettings();
        }
    }

    public async Task SaveSettingsAsync(ParentSettings settings)
    {
        var db = await database.GetConnectionAsync();
        if (settings.Id == 0)
        {
            await db.InsertAsync(settings);
        }
        else
        {
            await db.UpdateAsync(settings);
        }
    }

    private static byte[] Derive(string pin, byte[] salt) =>
        Rfc2898DeriveBytes.Pbkdf2(
            Encoding.UTF8.GetBytes(pin ?? string.Empty), salt, Iterations,
            HashAlgorithmName.SHA256, 32);
}
