namespace TongaKids.Services.SelfCheck;

/// <summary>
/// Exercises the credential paths that must never pass. Runs against the real
/// database, so it asserts only on rejection cases, which are safe whether or
/// not a guardian has registered on this device.
/// </summary>
public sealed class AuthSelfCheck(IAuthService auth) : ISelfCheck
{
    public string Area => "Guardian authentication";

    public IReadOnlyList<SelfCheckResult> Run()
    {
        var results = new List<SelfCheckResult>();

        void Check(string name, object expected, object actual) =>
            results.Add(new SelfCheckResult(
                name, Equals(expected, actual), $"expected {expected}, got {actual}"));

        Check("a wrong password is rejected", false,
            auth.SignInAsync("anyone", "definitely-not-the-password").GetAwaiter().GetResult());

        Check("a wrong security answer is rejected", false,
            auth.ResetPasswordAsync("anyone", "not-the-answer", "newpass123")
                .GetAwaiter().GetResult());

        Check("an empty password cannot register", false,
            auth.RegisterAsync("Test", "test@example.com", "", "answer")
                .GetAwaiter().GetResult());

        Check("an empty contact cannot register", false,
            auth.RegisterAsync("Test", "", "password123", "answer")
                .GetAwaiter().GetResult());

        return results;
    }
}
