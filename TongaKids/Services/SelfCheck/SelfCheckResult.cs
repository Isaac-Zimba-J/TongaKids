namespace TongaKids.Services.SelfCheck;

public sealed record SelfCheckResult(string Name, bool Passed, string Detail);

public interface ISelfCheck
{
    string Area { get; }

    /// <summary>
    /// Async by design. An earlier sync signature forced database-backed checks
    /// to block on their own continuations, which deadlocked the UI thread at
    /// startup and produced an ANR.
    /// </summary>
    Task<IReadOnlyList<SelfCheckResult>> RunAsync();
}

/// <summary>Collects assertions without every check repeating the boilerplate.</summary>
public sealed class SelfCheckCollector
{
    private readonly List<SelfCheckResult> _results = [];

    public void Check(string name, object expected, object actual) =>
        _results.Add(new SelfCheckResult(
            name, Equals(expected, actual), $"expected {expected}, got {actual}"));

    public IReadOnlyList<SelfCheckResult> Results => _results;
}
