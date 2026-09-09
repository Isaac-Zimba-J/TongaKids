using TongaKids.ViewModels;

namespace TongaKids.Views;

public partial class StoryReaderPage : ContentPage
{
    private readonly StoryReaderViewModel _viewModel;

    public StoryReaderPage(StoryReaderViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    protected override async void OnDisappearing()
    {
        base.OnDisappearing();
        await _viewModel.OnLeavingAsync();
    }
}
