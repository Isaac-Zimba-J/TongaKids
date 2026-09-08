using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TongaKids.Data;
using TongaKids.Services;

namespace TongaKids.ViewModels;

public sealed partial class PhonicsLevelsViewModel(
    IContentRepository content,
    IProgressRepository progress,
    ILearnerSession session) : ObservableObject
{
    public ObservableCollection<LevelCardModel> Levels { get; } = [];

    [ObservableProperty] private string _greeting = "Mwapoloka! (Hello!)";

    [RelayCommand]
    public async Task LoadAsync()
    {
        var learner = session.Current;
        if (learner is null)
        {
            await Shell.Current.GoToAsync("//profiles");
            return;
        }

        Levels.Clear();

        var levels = await content.GetLevelsAsync();
        var learnerProgress = await progress.GetForLearnerAsync(learner.Id);

        // A level is unlocked when its prerequisite level is fully mastered.
        var masteredByLevel = new Dictionary<int, bool>();

        foreach (var level in levels)
        {
            var lessons = await content.GetLessonsAsync(level.Id);
            var mastered = lessons.Count > 0 && lessons.All(l =>
                learnerProgress.Any(p => p.LessonId == l.Id && p.IsMastered));
            masteredByLevel[level.Number] = mastered;

            var completedCount = lessons.Count(l =>
                learnerProgress.Any(p => p.LessonId == l.Id && p.IsMastered));

            var isLocked = level.RequiresLevelNumber > 0
                           && !masteredByLevel.GetValueOrDefault(level.RequiresLevelNumber);

            var stars = lessons.Count == 0
                ? 0
                : learnerProgress
                    .Where(p => lessons.Any(l => l.Id == p.LessonId))
                    .Sum(p => p.StarsEarned);

            Levels.Add(new LevelCardModel
            {
                Number = level.Number,
                Title = level.Title,
                Subtitle = level.Subtitle,
                IconGlyph = level.IconGlyph,
                FirstLessonId = lessons.FirstOrDefault()?.Id ?? 0,
                IsLocked = isLocked,
                IsComplete = mastered,
                IsCurrent = !isLocked && !mastered,
                ProgressFraction = lessons.Count == 0 ? 0 : (double)completedCount / lessons.Count,
                Stars = Math.Min(stars, 3)
            });
        }
    }

    [RelayCommand]
    private static async Task OpenAsync(LevelCardModel? level)
    {
        // A locked or empty level is simply not tappable. No message, no error.
        if (level is null || level.IsLocked || !level.HasLessons)
        {
            return;
        }

        await Shell.Current.GoToAsync($"lesson?lessonId={level.FirstLessonId}");
    }
}
