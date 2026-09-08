namespace TongaKids.Services;

/// <summary>
/// The guard condition from the project report's state diagram (Figure 9):
/// at or above the threshold the learner advances; below it they return to
/// Active Learning for remediation. No penalty, no lost progress.
/// </summary>
public sealed class MasteryEvaluator : IMasteryEvaluator
{
    /// <summary>The single definition of the mastery rule. Never inline this number.</summary>
    public const int MasteryThresholdPercent = 80;

    public MasteryOutcome Evaluate(int accuracyPercent) =>
        accuracyPercent >= MasteryThresholdPercent
            ? MasteryOutcome.Mastered
            : MasteryOutcome.NeedsRemediation;
}
