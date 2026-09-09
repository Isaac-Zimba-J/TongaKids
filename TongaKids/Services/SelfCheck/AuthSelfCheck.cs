namespace TongaKids.Services.SelfCheck;

/// <summary>
/// Exercises the credential paths that must never pass. Fully async: these hit
/// the database, and blocking on them from the UI thread deadlocks.
/// </summary>
public sealed class AuthSelfCheck(IAuthService auth) : ISelfCheck
{
    public string Area => "Guardian authentication";

    public async Task<IReadOnlyList<SelfCheckResult>> RunAsync()
    {
        var c = new SelfCheckCollector();

        c.Check("a wrong password is rejected", false,
            await auth.SignInAsync("anyone", "definitely-not-the-password"));

        c.Check("a wrong security answer is rejected", false,
            await auth.ResetPasswordAsync("anyone", "not-the-answer", "newpass123"));

        c.Check("an empty password cannot register", false,
            await auth.RegisterAsync("Test", "test@example.com", "", "answer"));

        c.Check("an empty contact cannot register", false,
            await auth.RegisterAsync("Test", "", "password123", "answer"));

        return c.Results;
    }
}
