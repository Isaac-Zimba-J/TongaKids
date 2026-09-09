using TongaKids.ViewModels;

namespace TongaKids.Views;

public partial class StoryReaderPage : ContentPage
{
    public StoryReaderPage(StoryReaderViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
