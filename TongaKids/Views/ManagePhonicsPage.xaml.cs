using TongaKids.ViewModels;

namespace TongaKids.Views;

public partial class ManagePhonicsPage : ContentPage
{
    private readonly ManagePhonicsViewModel _viewModel;

    public ManagePhonicsPage(ManagePhonicsViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    // Reloads on every appearance so returning from the lesson editor shows
    // sounds added there.
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.LoadAsync();
    }
}
