namespace TongaKids.Views;

public partial class SplashPage : ContentPage
{
    public SplashPage()
    {
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        await LoadingBar.ProgressTo(1.0, 1200, Easing.CubicInOut);

        var seenOnboarding = Preferences.Default.Get("onboarding_complete", false);
        await Shell.Current.GoToAsync(seenOnboarding ? "//profiles" : "//onboarding");
    }
}
