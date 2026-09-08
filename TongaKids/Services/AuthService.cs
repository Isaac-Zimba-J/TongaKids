using System.Security.Cryptography;
using System.Text;
using TongaKids.Data;
using TongaKids.Models;

namespace TongaKids.Services;

/// <summary>
/// Local-only guardian authentication. There is no auth server because there is
/// no backend in scope; the flow is real, its remote counterpart is not built.
/// </summary>
public sealed class AuthService(ITongaKidsDatabase database) : IAuthService
{
    /// <summary>
    /// Email recovery needs a server to send mail, so recovery is a locally
    /// verifiable challenge instead.
    /// </summary>
    public const string SecurityQuestion = "What is the name of your home village?";

    private const int Iterations = 100_000;
    private const int SaltBytes = 16;
    private const int HashBytes = 32;

    public GuardianAccount? CurrentAccount { get; private set; }

    public async Task<bool> IsRegisteredAsync() => await GetAccountAsync() is not null;

    public async Task<bool> RegisterAsync(
        string name, string contact, string password, string securityAnswer)
    {
        if (string.IsNullOrWhiteSpace(contact) || string.IsNullOrWhiteSpace(password))
        {
            return false;
        }

        var db = await database.GetConnectionAsync();

        var (passwordHash, passwordSalt) = Hash(password);
        var (answerHash, answerSalt) = Hash(Normalise(securityAnswer));

        var account = new GuardianAccount
        {
            Name = name.Trim(),
            Contact = contact.Trim(),
            PasswordHash = passwordHash,
            PasswordSalt = passwordSalt,
            SecurityAnswerHash = answerHash,
            SecurityAnswerSalt = answerSalt,
            CreatedAt = DateTime.UtcNow
        };

        await db.InsertAsync(account);
        CurrentAccount = account;
        return true;
    }

    public async Task<bool> SignInAsync(string contact, string password)
    {
        var account = await GetAccountAsync();
        if (account is null || !Verify(password, account.PasswordHash, account.PasswordSalt))
        {
            return false;
        }

        CurrentAccount = account;
        return true;
    }

    public async Task<bool> ResetPasswordAsync(
        string contact, string securityAnswer, string newPassword)
    {
        var account = await GetAccountAsync();
        if (account is null || string.IsNullOrWhiteSpace(newPassword))
        {
            return false;
        }

        if (!Verify(Normalise(securityAnswer), account.SecurityAnswerHash, account.SecurityAnswerSalt))
        {
            return false;
        }

        var (hash, salt) = Hash(newPassword);
        account.PasswordHash = hash;
        account.PasswordSalt = salt;

        var db = await database.GetConnectionAsync();
        await db.UpdateAsync(account);
        return true;
    }

    private async Task<GuardianAccount?> GetAccountAsync()
    {
        try
        {
            var db = await database.GetConnectionAsync();
            return await db.Table<GuardianAccount>().FirstOrDefaultAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Auth] lookup failed: {ex.Message}");
            return null;
        }
    }

    /// <summary>Case and whitespace insensitive, so recovery is not a memory test.</summary>
    private static string Normalise(string value) =>
        (value ?? string.Empty).Trim().ToLowerInvariant();

    private static (string Hash, string Salt) Hash(string value)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltBytes);
        var hash = Rfc2898DeriveBytes.Pbkdf2(
            Encoding.UTF8.GetBytes(value), salt, Iterations, HashAlgorithmName.SHA256, HashBytes);
        return (Convert.ToBase64String(hash), Convert.ToBase64String(salt));
    }

    private static bool Verify(string value, string expectedHash, string saltBase64)
    {
        try
        {
            var salt = Convert.FromBase64String(saltBase64);
            var hash = Rfc2898DeriveBytes.Pbkdf2(
                Encoding.UTF8.GetBytes(value), salt, Iterations, HashAlgorithmName.SHA256, HashBytes);

            // Constant-time comparison: never leak how much of the hash matched.
            return CryptographicOperations.FixedTimeEquals(
                hash, Convert.FromBase64String(expectedHash));
        }
        catch
        {
            return false;
        }
    }
}
