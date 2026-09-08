using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TongaKids.Data;
using TongaKids.Models;
using TongaKids.Services;

namespace TongaKids.ViewModels;

[QueryProperty(nameof(LessonId), "lessonId")]
[QueryProperty(nameof(Correct), "correct")]
[QueryProperty(nameof(Total), "total")]
[QueryProperty(nameof(DurationMs), "durationMs")]
public sealed partial class LessonCompleteViewModel(
    IProgressCalculator calculator,
    IMasteryEvaluator evaluator,
    IProgressRepository progress,
    ILearnerSession session) : ObservableObject
{
    [ObservableProperty] private int _lessonId;
    [ObservableProperty] private int _correct;
    [ObservableProperty] private int _total;
    [ObservableProperty] private long _durationMs;

    [ObservableProperty] private int _stars;
    [ObservableProperty] private int _scorePercent;
    [ObservableProperty] private string _headline = string.Empty;
    [ObservableProperty] private string _message = string.Empty;
    [ObservableProperty] private bool _isMastered;

    private bool _saved;

    partial void OnTotalChanged(int value) => _ = EvaluateAndSaveAsync();

    private async Task EvaluateAndSaveAsync()
    {
        if (_saved || Total <= 0)
        {
            return;
        }

        _saved = true;

        ScorePercent = calculator.AccuracyPercent(Correct, Total);
        Stars = calculator.StarsFor(ScorePercent);
        IsMastered = evaluator.Evaluate(ScorePercent) == MasteryOutcome.Mastered;

        // Encouraging in both directions. A child who scored 40% is not told they failed.
        Headline = IsMastered ? "Well done!" : "Good try!";
        Message = IsMastered
            ? $"You got {Correct} of {Total} right."
            : "Let's practise these sounds once more.";

        var learner = session.Current;
        if (learner is null)
        {
            return;
        }

        try
        {
            await progress.AddAttemptAsync(new QuizAttempt
            {
                LearnerId = learner.Id,
                LessonId = LessonId,
                CorrectCount = Correct,
                TotalCount = Total,
                DurationMs = DurationMs,
                AttemptedAt = DateTime.UtcNow
            });

            var existing = await progress.GetForLessonAsync(learner.Id, LessonId)
                           ?? new LessonProgress { LearnerId = learner.Id, LessonId = LessonId };

            // Keep the learner's best result; a worse retry never takes anything away.
            if (ScorePercent >= existing.BestScorePercent)
            {
                existing.BestScorePercent = ScorePercent;
                existing.StarsEarned = Stars;
            }

            existing.IsMastered = existing.IsMastered || IsMastered;
            existing.CompletedAt = DateTime.UtcNow;

            await progress.SaveAsync(existing);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Complete] save failed: {ex}");
        }
    }

    [RelayCommand]
    private async Task ContinueAsync()
    {
        // Mastered moves on; otherwise back into the lesson for remediation.
        await Shell.Current.GoToAsync(IsMastered ? "//main/home" : $"lesson?lessonId={LessonId}");
    }
}
