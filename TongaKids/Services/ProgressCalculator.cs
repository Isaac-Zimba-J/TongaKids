namespace TongaKids.Services;

public sealed class ProgressCalculator : IProgressCalculator
{
    public int AccuracyPercent(int correct, int total)
    {
        if (total <= 0)
        {
            return 0;
        }

        var clamped = Math.Clamp(correct, 0, total);
        return (int)Math.Round(clamped * 100.0 / total, MidpointRounding.AwayFromZero);
    }

    public int StarsFor(int accuracyPercent) => accuracyPercent switch
    {
        >= 95 => 5,
        >= 85 => 4,
        >= 80 => 3,
        >= 60 => 2,
        _ => 1
    };

    public double WordsPerMinute(int wordsRead, long durationMs)
    {
        if (durationMs <= 0 || wordsRead <= 0)
        {
            return 0;
        }

        return wordsRead / (durationMs / 60000.0);
    }
}
