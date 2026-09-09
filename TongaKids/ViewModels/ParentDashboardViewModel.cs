using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TongaKids.Data;
using TongaKids.Services;

namespace TongaKids.ViewModels;

public sealed record LearnerReport(
    string Name, string Accuracy, string WordsPerMinute, string Lessons, string Weakest);

public sealed partial class ParentDashboardViewModel(
    ILearnerRepository learners,
    IProgressAnalytics analytics,
    IParentalGateService gate) : ObservableObject
{
    public ObservableCollection<LearnerReport> Reports { get; } = [];

    [ObservableProperty] private bool _isEmpty;

    [RelayCommand]
    public async Task LoadAsync()
    {
        // Defence in depth: never render this screen if the gate was bypassed.
        if (!gate.IsUnlocked)
        {
            await Shell.Current.GoToAsync("//main/settings");
            return;
        }

        Reports.Clear();

        foreach (var learner in await learners.GetAllAsync())
        {
            var stats = await analytics.ForLearnerAsync(learner.Id);
            Reports.Add(new LearnerReport(
                Name: learner.Name,
                Accuracy: $"{stats.AccuracyPercent}%",
                WordsPerMinute: $"{stats.WordsPerMinute:0} wpm",
                Lessons: $"{stats.LessonsMastered} of {stats.LessonsTotal}",
                Weakest: stats.WeakestLessonTitle));
        }

        IsEmpty = Reports.Count == 0;
    }

    [RelayCommand]
    private static async Task OpenConsentAsync() => await Shell.Current.GoToAsync("consent");

    [RelayCommand]
    private static async Task OpenRecordSoundsAsync() =>
        await Shell.Current.GoToAsync("recordsounds");

    [RelayCommand]
    private static async Task OpenStoriesAsync() =>
        await Shell.Current.GoToAsync("managestories");

    [RelayCommand]
    private static async Task OpenPhonicsAsync() =>
        await Shell.Current.GoToAsync("managephonics");
}
