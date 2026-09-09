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
}
