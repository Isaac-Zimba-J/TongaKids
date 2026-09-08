using TongaKids.ViewModels;

namespace TongaKids.Views;

public partial class ProfileSelectionPage : ContentPage
{
    private readonly ProfileSelectionViewModel _viewModel;

    public ProfileSelectionPage(ProfileSelectionViewModel viewModel)
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
