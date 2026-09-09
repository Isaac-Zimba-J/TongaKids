using TongaKids.ViewModels;

namespace TongaKids.Views;

public partial class ParentalGatePage : ContentPage
{
    private readonly ParentalGateViewModel _viewModel;

    public ParentalGatePage(ParentalGateViewModel viewModel)
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
