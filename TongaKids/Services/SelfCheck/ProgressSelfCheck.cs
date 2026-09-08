namespace TongaKids.Services.SelfCheck;

public sealed class ProgressSelfCheck(
    IProgressCalculator calculator,
    IMasteryEvaluator evaluator) : ISelfCheck
{
    public string Area => "Progress and mastery";

    public IReadOnlyList<SelfCheckResult> Run()
    {
        var results = new List<SelfCheckResult>();

        void Check(string name, object expected, object actual) =>
            results.Add(new SelfCheckResult(
                name,
                Equals(expected, actual),
                $"expected {expected}, got {actual}"));

        // Accuracy
        Check("8 of 10 is 80%", 80, calculator.AccuracyPercent(8, 10));
        Check("0 of 0 is 0% (no divide by zero)", 0, calculator.AccuracyPercent(0, 0));
        Check("3 of 7 rounds to 43%", 43, calculator.AccuracyPercent(3, 7));
        Check("correct above total is clamped", 100, calculator.AccuracyPercent(99, 10));

        // Stars - DESIGN.md thresholds
        Check("100% earns 5 stars", 5, calculator.StarsFor(100));
        Check("95% earns 5 stars", 5, calculator.StarsFor(95));
        Check("94% earns 4 stars", 4, calculator.StarsFor(94));
        Check("85% earns 4 stars", 4, calculator.StarsFor(85));
        Check("80% earns 3 stars", 3, calculator.StarsFor(80));
        Check("60% earns 2 stars", 2, calculator.StarsFor(60));
        Check("59% earns 1 star", 1, calculator.StarsFor(59));

        // Words per minute
        Check("60 words in 60s is 60 wpm", 60d, calculator.WordsPerMinute(60, 60_000));
        Check("30 words in 30s is 60 wpm", 60d, calculator.WordsPerMinute(30, 30_000));
        Check("zero duration is 0 wpm", 0d, calculator.WordsPerMinute(50, 0));

        // The report's guard condition
        Check("80% is mastered", MasteryOutcome.Mastered, evaluator.Evaluate(80));
        Check("79% needs remediation", MasteryOutcome.NeedsRemediation, evaluator.Evaluate(79));
        Check("100% is mastered", MasteryOutcome.Mastered, evaluator.Evaluate(100));
        Check("0% needs remediation", MasteryOutcome.NeedsRemediation, evaluator.Evaluate(0));

        return results;
    }
}
