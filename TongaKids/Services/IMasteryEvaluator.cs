namespace TongaKids.Services;

public enum MasteryOutcome
{
    Mastered,
    NeedsRemediation
}

public interface IMasteryEvaluator
{
    MasteryOutcome Evaluate(int accuracyPercent);
}
