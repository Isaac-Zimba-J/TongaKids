using TongaKids.ViewModels;

namespace TongaKids.Views;

public partial class PhonicsLessonPage : ContentPage
{
    public PhonicsLessonPage(PhonicsLessonViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
