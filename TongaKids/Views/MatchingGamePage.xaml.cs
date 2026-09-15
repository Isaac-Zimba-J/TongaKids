using TongaKids.Animations;
using TongaKids.Controls;
using TongaKids.ViewModels;

namespace TongaKids.Views;

public partial class MatchingGamePage : ContentPage
{
    private readonly MatchingGameViewModel _viewModel;
    private readonly AnswerFeedbackView _feedback;

    public MatchingGamePage(MatchingGameViewModel viewModel, IAnimationClock clock)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;

        _feedback = new AnswerFeedbackView(clock);
        FeedbackHost.Content = _feedback;

        _viewModel.AnswerSubmitted += OnAnswerSubmitted;
    }

    private void OnAnswerSubmitted(object? sender, bool wasCorrect) =>
        _feedback.Show(wasCorrect);

    protected override async void OnDisappearing()
    {
        base.OnDisappearing();

        // The page is transient; a live handler would keep the view model alive.
        _viewModel.AnswerSubmitted -= OnAnswerSubmitted;
        _feedback.Stop();

        await _viewModel.OnLeavingAsync();
    }
}
