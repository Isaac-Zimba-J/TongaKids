using TongaKids.ViewModels;

namespace TongaKids.Views;

public partial class ManageStoriesPage : ContentPage
{
    private readonly ManageStoriesViewModel _viewModel;

    public ManageStoriesPage(ManageStoriesViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    // Reloads every time it is shown, so returning from the editor reflects
    // pages added or text written there.
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.LoadAsync();
    }
}
