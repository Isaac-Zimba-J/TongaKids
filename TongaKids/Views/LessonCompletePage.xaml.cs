using TongaKids.Animations;
using TongaKids.Controls;
using TongaKids.ViewModels;

namespace TongaKids.Views;

public partial class LessonCompletePage : ContentPage
{
    private readonly LessonCompleteViewModel _viewModel;
    private readonly CelebrationView _celebration;

    public LessonCompletePage(LessonCompleteViewModel viewModel, IAnimationClock clock)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;

        _celebration = new CelebrationView(clock);
        CelebrationHost.Content = _celebration;

        // Tapping anywhere skips the flourish. A child repeating a lesson should
        // never have to sit through it again. The gesture goes on the content,
        // because ContentPage itself has no gesture recognizers.
        var skip = new TapGestureRecognizer();
        skip.Tapped += (_, _) => _celebration.Skip();
        RootGrid.GestureRecognizers.Add(skip);
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        // Only celebrate mastery. A "Good try!" result gets a calm screen.
        if (_viewModel.IsMastered)
        {
            _celebration.StarCount = _viewModel.Stars;
            _celebration.Play();
        }
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();

        // A timer left running keeps waking the CPU.
        _celebration.Stop();
    }
}
