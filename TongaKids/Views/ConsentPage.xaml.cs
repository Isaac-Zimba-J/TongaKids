using TongaKids.ViewModels;

namespace TongaKids.Views;

public partial class ConsentPage : ContentPage
{
    private readonly ConsentViewModel _viewModel;

    public ConsentPage(ConsentViewModel viewModel)
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
