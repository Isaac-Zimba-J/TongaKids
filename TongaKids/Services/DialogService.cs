using UXDivers.Popups;
using UXDivers.Popups.Services;
using UXDivers.Popups.Maui;
using UXDivers.Popups.Maui.Controls;

namespace TongaKids.Services;

/// <inheritdoc />
public sealed class DialogService : IDialogService
{
    public async Task<string?> PromptAsync(
        string title, string message, string placeholder,
        string acceptText = "Save", bool isPassword = false)
    {
        var popup = new PromptPopup(title, message, placeholder, acceptText, isPassword);
        await IPopupService.Current.PushAsync(popup);
        return await popup.Completion;
    }

    /// <summary>
    /// The library's Toast has no auto-dismiss, so it stays on screen until
    /// something pops it. This shows it, returns immediately, and dismisses it
    /// on a timer without making the caller wait.
    /// </summary>
    public async Task ToastAsync(string title)
    {
        var toast = new TongaToast(title);
        await IPopupService.Current.PushAsync(toast);

        _ = DismissAfterAsync(toast, TimeSpan.FromSeconds(2.2));
    }

    private static async Task DismissAfterAsync(PopupPage popup, TimeSpan delay)
    {
        try
        {
            await Task.Delay(delay);
            await IPopupService.Current.PopAsync(popup);
        }
        catch (Exception ex)
        {
            // Already dismissed, or the page went away first. Nothing to do.
            System.Diagnostics.Debug.WriteLine($"[Dialogs] toast dismiss: {ex.Message}");
        }
    }

    public async Task AlertAsync(string title, string message, string acceptText = "OK")
    {
        var popup = new AlertPopup(title, message);
        await IPopupService.Current.PushAsync(popup);
        await popup.Completion;
    }
}

/// <summary>
/// A one-field form popup that reports its result as a Task. Closing by backdrop
/// or the Android back button resolves to null, so a caller never waits forever.
/// </summary>
internal sealed class PromptPopup : FormPopup
{
    private readonly TaskCompletionSource<string?> _completion = new();
    private readonly FormField _field;

    /// <summary>Named Completion, not Result: the base already defines Result.</summary>
    public Task<string?> Completion => _completion.Task;

    public PromptPopup(
        string title, string message, string placeholder, string acceptText, bool isPassword)
    {
        _field = new FormField { Placeholder = placeholder, IsPassword = isPassword };

        Title = title;
        Text = message;
        ActionButtonText = acceptText;
        ShowActionButton = true;
        Items = new List<FormField> { _field };

        ActionButtonCommand = new Command(async () =>
        {
            _completion.TrySetResult(_field.Value);
            SetResult([_field.Value]);
            await IPopupService.Current.PopAsync(this);
        });

        ApplyTongaKidsSurface();

        if (Application.Current?.Resources
                .TryGetValue("TongaKidsFormFieldTemplate", out var template) == true
            && template is DataTemplate fieldTemplate)
        {
            ItemDataTemplate = fieldTemplate;
        }
    }

    /// <summary>
    /// Paints the popup in the app's own palette. The library ships only a dark
    /// theme, and its card background comes from an internal key, so the surface
    /// is set directly here where it is fully under our control.
    /// </summary>
    private void ApplyTongaKidsSurface()
    {
        PopupBackground = new SolidColorBrush(Color.FromArgb("#ffffff"));
        PopupBorderBrush = new SolidColorBrush(Color.FromArgb("#e0d8c3"));
        PopupBorderThickness = 1;
        PopupCornerRadius = new CornerRadius(16);
    }


    public override Task OnPopupClosedAsync(PopupEventArgs e)
    {
        // No-op when the action button already supplied a value.
        _completion.TrySetResult(null);
        return base.OnPopupClosedAsync(e);
    }
}

/// <summary>
/// A brief confirmation in the app's palette. Unlike the library default this
/// paints no backdrop and lets touches through, because a toast is not modal.
/// </summary>
internal sealed class TongaToast : Toast
{
    public TongaToast(string title)
    {
        Title = title;

        PopupBackground = new SolidColorBrush(Color.FromArgb("#ffffff"));
        PopupBorderBrush = new SolidColorBrush(Color.FromArgb("#e0d8c3"));
        PopupBorderThickness = 1;
        PopupCornerRadius = new CornerRadius(24);

        // No dimming, and taps continue through to the page beneath.
        BackgroundColor = Colors.Transparent;
        BackgroundInputTransparent = true;
        CloseWhenBackgroundIsClicked = false;
    }
}

/// <summary>An informational popup whose dismissal can be awaited.</summary>
internal sealed class AlertPopup : SimpleTextPopup
{
    private readonly TaskCompletionSource _completion = new();

    public Task Completion => _completion.Task;

    public AlertPopup(string title, string message)
    {
        Title = title;
        Text = message;
        ApplyTongaKidsSurface();
    }

    /// <summary>
    /// Paints the popup in the app's own palette. The library ships only a dark
    /// theme, and its card background comes from an internal key, so the surface
    /// is set directly here where it is fully under our control.
    /// </summary>
    private void ApplyTongaKidsSurface()
    {
        PopupBackground = new SolidColorBrush(Color.FromArgb("#ffffff"));
        PopupBorderBrush = new SolidColorBrush(Color.FromArgb("#e0d8c3"));
        PopupBorderThickness = 1;
        PopupCornerRadius = new CornerRadius(16);
    }


    public override Task OnPopupClosedAsync(PopupEventArgs e)
    {
        _completion.TrySetResult();
        return base.OnPopupClosedAsync(e);
    }
}
