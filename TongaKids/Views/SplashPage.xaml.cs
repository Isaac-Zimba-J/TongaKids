using TongaKids.Services;

namespace TongaKids.Views;

public partial class SplashPage : ContentPage
{
    private readonly IAuthService _auth;

    /// <summary>
    /// IAuthService is injected rather than resolved from Handler.MauiContext,
    /// which is not yet populated during OnAppearing. Resolving it there returned
    /// null and silently routed past registration.
    /// </summary>
    public SplashPage(IAuthService auth)
    {
        InitializeComponent();
        _auth = auth;
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
        var registered = await _auth.IsRegisteredAsync();
        await Shell.Current.GoToAsync(registered ? "//signin" : "//register");
    }
}
