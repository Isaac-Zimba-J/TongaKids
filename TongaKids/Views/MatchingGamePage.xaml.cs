using TongaKids.ViewModels;

namespace TongaKids.Views;

public partial class MatchingGamePage : ContentPage
{
    public MatchingGamePage(MatchingGameViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
