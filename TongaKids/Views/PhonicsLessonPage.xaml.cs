using TongaKids.ViewModels;

namespace TongaKids.Views;

public partial class PhonicsLessonPage : ContentPage
{
    private readonly PhonicsLessonViewModel _viewModel;

    public PhonicsLessonPage(PhonicsLessonViewModel viewModel)
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
