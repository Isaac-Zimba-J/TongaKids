using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace TongaKids.ViewModels;

public sealed record OnboardingSlide(string ImageKey, string Title, string Body);

public sealed partial class OnboardingViewModel : ObservableObject
{
    public IReadOnlyList<OnboardingSlide> Slides { get; } =
    [
        new("illus_onboarding_blocks", "Learn Chitonga Sounds",
            "Master letters and syllables through fun activities."),
        new("illus_lion_reading", "Read Together",
            "Enjoy illustrated folktales with audio narration."),
        new("badge_master_reader", "Earn Your Stars",
            "Track your progress and collect rewards as you grow.")
    ];

    [ObservableProperty]
    private int _position;

    public bool IsLastSlide => Position >= Slides.Count - 1;

    public string NextLabel => IsLastSlide ? "Start" : "Next";

    partial void OnPositionChanged(int value)
    {
        OnPropertyChanged(nameof(IsLastSlide));
        OnPropertyChanged(nameof(NextLabel));
    }

    [RelayCommand]
    private async Task NextAsync()
    {
        if (!IsLastSlide)
        {
            Position++;
            return;
        }

        await FinishAsync();
    }

    [RelayCommand]
    private async Task SkipAsync() => await FinishAsync();

    private static async Task FinishAsync()
    {
        Preferences.Default.Set("onboarding_complete", true);
        await Shell.Current.GoToAsync("//profiles");
    }
}
