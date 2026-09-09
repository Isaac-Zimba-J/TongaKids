using TongaKids.ViewModels;

namespace TongaKids.Views;

public partial class LessonEditorPage : ContentPage
{
    private readonly LessonEditorViewModel _viewModel;

    public LessonEditorPage(LessonEditorViewModel viewModel)
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
