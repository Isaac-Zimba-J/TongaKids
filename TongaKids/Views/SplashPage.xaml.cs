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

        Dots.Start();
        await PlayEntranceAsync();

        if (!Preferences.Default.Get("onboarding_complete", false))
        {
            Dots.Stop();
            await Shell.Current.GoToAsync("//onboarding");
            return;
        }

        // A guardian registers once; thereafter they sign in, then a child
        // picks a face. No credential field ever appears in the child's path.
        var registered = await _auth.IsRegisteredAsync();

        Dots.Stop();
        await Shell.Current.GoToAsync(registered ? "//signin" : "//register");
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();

        // Never leave a timer running on a page nobody is looking at.
        Dots.Stop();
    }

    /// <summary>
    /// The tree settles in and breathes; the wordmark rises to meet it. Gives the
    /// splash a moment of life instead of a static image and a bar.
    /// </summary>
    private async Task PlayEntranceAsync()
    {
        Baobab.Scale = 0.86;
        Baobab.Opacity = 0;
        Wordmark.Opacity = 0;
        Wordmark.TranslationY = 18;
        Tagline.Opacity = 0;

        await Task.WhenAll(
            Baobab.FadeToAsync(1, 420, Easing.CubicOut),
            Baobab.ScaleToAsync(1.0, 620, Easing.SpringOut));

        await Task.WhenAll(
            Wordmark.FadeToAsync(1, 320, Easing.CubicOut),
            Wordmark.TranslateToAsync(0, 0, 380, Easing.CubicOut));

        await Tagline.FadeToAsync(1, 300, Easing.CubicOut);

        // A slow breath while the database opens behind the scenes.
        await Baobab.ScaleToAsync(1.03, 700, Easing.SinInOut);
        await Baobab.ScaleToAsync(1.0, 700, Easing.SinInOut);
    }
}
