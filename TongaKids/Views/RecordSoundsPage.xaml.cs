using TongaKids.ViewModels;

namespace TongaKids.Views;

public partial class RecordSoundsPage : ContentPage
{
    private readonly RecordSoundsViewModel _viewModel;

    public RecordSoundsPage(RecordSoundsViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.LoadAsync();
    }

    protected override async void OnDisappearing()
    {
        base.OnDisappearing();

        // Leaving the screen abandons any take in progress; the microphone must
        // never keep running on a page nobody is looking at.
        await _viewModel.AbandonAsync();
    }
}
