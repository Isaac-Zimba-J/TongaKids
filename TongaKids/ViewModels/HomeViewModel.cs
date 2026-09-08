using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TongaKids.Data;
using TongaKids.Services;

namespace TongaKids.ViewModels;

public sealed partial class HomeViewModel(
    ILearnerSession session,
    IContentRepository content,
    IProgressRepository progress,
    IProgressCalculator calculator) : ObservableObject
{
    [ObservableProperty] private string _greeting = "Welcome back!";
    [ObservableProperty] private string _avatarKey = "avatar_chipo";
    [ObservableProperty] private string _goalSummary = "Let's begin!";
    [ObservableProperty] private double _goalProgress;
    [ObservableProperty] private string _missionSubtitle = "Start your first lesson";

    [RelayCommand]
    public async Task LoadAsync()
    {
        var learner = session.Current;
        if (learner is null)
        {
            // No profile chosen — send the child back rather than showing an error.
            await Shell.Current.GoToAsync("//profiles");
            return;
        }

        Greeting = $"Welcome back, {learner.Name}!";
        AvatarKey = learner.AvatarKey;

        try
        {
            var levels = await content.GetLevelsAsync();
            var lessonCount = 0;
            foreach (var level in levels)
            {
                lessonCount += (await content.GetLessonsAsync(level.Id)).Count;
            }

            var done = (await progress.GetForLearnerAsync(learner.Id)).Count(p => p.IsMastered);

            GoalProgress = lessonCount == 0 ? 0 : (double)done / lessonCount;
            GoalSummary = $"{calculator.AccuracyPercent(done, lessonCount)}% of today's goal";
            MissionSubtitle = done == 0
                ? "Master the letter 'A' sounds"
                : $"{done} of {lessonCount} lessons mastered";
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Home] load failed: {ex}");
        }
    }

    [RelayCommand]
    private static async Task OpenPhonicsAsync() => await Shell.Current.GoToAsync("levels");
}
