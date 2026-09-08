namespace TongaKids.Services.SelfCheck;

public sealed record SelfCheckResult(string Name, bool Passed, string Detail);

public interface ISelfCheck
{
    string Area { get; }

    IReadOnlyList<SelfCheckResult> Run();
}
