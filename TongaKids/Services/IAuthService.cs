using TongaKids.Models;

namespace TongaKids.Services;

public interface IAuthService
{
    GuardianAccount? CurrentAccount { get; }

    Task<bool> IsRegisteredAsync();

    Task<bool> RegisterAsync(string name, string contact, string password, string securityAnswer);

    Task<bool> SignInAsync(string contact, string password);

    Task<bool> ResetPasswordAsync(string contact, string securityAnswer, string newPassword);
}
