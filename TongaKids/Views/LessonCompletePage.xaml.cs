using TongaKids.ViewModels;

namespace TongaKids.Views;

public partial class LessonCompletePage : ContentPage
{
    public LessonCompletePage(LessonCompleteViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
