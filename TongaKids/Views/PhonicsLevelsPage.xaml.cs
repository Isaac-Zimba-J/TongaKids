using TongaKids.ViewModels;

namespace TongaKids.Views;

public partial class PhonicsLevelsPage : ContentPage
{
    private readonly PhonicsLevelsViewModel _viewModel;

    public PhonicsLevelsPage(PhonicsLevelsViewModel viewModel)
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
