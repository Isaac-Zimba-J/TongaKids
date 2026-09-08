namespace TongaKids.Services;

public interface IProgressCalculator
{
    /// <summary>Rounded percentage. Zero questions scores 0, never divides by zero.</summary>
    int AccuracyPercent(int correct, int total);

    /// <summary>DESIGN.md five-star layout: 95/85/80/60 thresholds.</summary>
    int StarsFor(int accuracyPercent);

    /// <summary>Reading fluency. Zero or negative duration returns 0.</summary>
    double WordsPerMinute(int wordsRead, long durationMs);
}
