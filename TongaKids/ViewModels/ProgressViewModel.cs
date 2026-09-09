using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TongaKids.Services;

namespace TongaKids.ViewModels;

public sealed partial class ProgressViewModel(
    IProgressAnalytics analytics,
    ILearnerSession session) : ObservableObject
{
    [ObservableProperty] private string _title = "My Progress";
    [ObservableProperty] private int _stars;
    [ObservableProperty] private string _lessonsSummary = string.Empty;
    [ObservableProperty] private string _storiesSummary = string.Empty;
    [ObservableProperty] private string _accuracySummary = string.Empty;
    [ObservableProperty] private double _completion;

    [RelayCommand]
    public async Task LoadAsync()
    {
        var learner = session.Current;
        if (learner is null)
        {
            await Shell.Current.GoToAsync("//profiles");
            return;
        }

        var stats = await analytics.ForLearnerAsync(learner.Id);

        Title = $"{learner.Name}'s Progress";
        Stars = Math.Min(stats.StarsEarned, 5);
        LessonsSummary = $"{stats.LessonsMastered} of {stats.LessonsTotal} lessons mastered";
        StoriesSummary = stats.StoriesRead == 1 ? "1 story read" : $"{stats.StoriesRead} stories read";
        AccuracySummary = $"{stats.AccuracyPercent}% correct so far";
        Completion = stats.LessonsTotal == 0 ? 0 : (double)stats.LessonsMastered / stats.LessonsTotal;
    }
}
