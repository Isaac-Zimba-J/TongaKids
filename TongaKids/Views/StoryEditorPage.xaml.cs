using TongaKids.ViewModels;

namespace TongaKids.Views;

public partial class StoryEditorPage : ContentPage
{
    private readonly StoryEditorViewModel _viewModel;

    public StoryEditorPage(StoryEditorViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    protected override async void OnDisappearing()
    {
        base.OnDisappearing();

        // Saves typed text, abandons any take, and stops playback.
        await _viewModel.OnLeavingAsync();
    }
}
