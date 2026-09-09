namespace TongaKids.Services.SelfCheck;

/// <summary>
/// Runs every registered self-check. Shared by the diagnostic page and the
/// DEBUG startup log, so the number on screen and the number in logcat can
/// never disagree.
/// </summary>
public static class SelfCheckRunner
{
    /// <summary>
    /// <paramref name="report"/> is called once per area with a null result to
    /// mark a heading, then once per assertion.
    /// </summary>
    public static async Task<(int Passed, int Total)> RunAsync(
        IEnumerable<ISelfCheck> checks,
        Action<string, SelfCheckResult?>? report = null)
    {
        var passed = 0;
        var total = 0;

        foreach (var check in checks)
        {
            report?.Invoke(check.Area, null);

            IReadOnlyList<SelfCheckResult> results;
            try
            {
                results = await check.RunAsync();
            }
            catch (Exception ex)
            {
                results = [new SelfCheckResult($"{check.Area} threw", false, ex.Message)];
            }

            foreach (var result in results)
            {
                total++;
                if (result.Passed)
                {
                    passed++;
                }

                report?.Invoke(check.Area, result);
            }
        }

        return (passed, total);
    }
}
