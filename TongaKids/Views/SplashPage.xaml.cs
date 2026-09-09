using TongaKids.Services;

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

        if (!Preferences.Default.Get("onboarding_complete", false))
        {
            await Shell.Current.GoToAsync("//onboarding");
            return;
        }

        // A guardian registers once; thereafter they sign in, then a child
        // picks a face. No credential field ever appears in the child's path.
        var auth = Handler?.MauiContext?.Services.GetService<IAuthService>();
        if (auth is null)
        {
            await Shell.Current.GoToAsync("//profiles");
            return;
        }

        var registered = await auth.IsRegisteredAsync();
        await Shell.Current.GoToAsync(registered ? "//signin" : "//register");
    }
}
