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

    public async Task ToastAsync(string title)
    {
        await IPopupService.Current.PushAsync(new Toast { Title = title });
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
    }

    public override Task OnPopupClosedAsync(PopupEventArgs e)
    {
        // No-op when the action button already supplied a value.
        _completion.TrySetResult(null);
        return base.OnPopupClosedAsync(e);
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
    }

    public override Task OnPopupClosedAsync(PopupEventArgs e)
    {
        _completion.TrySetResult();
        return base.OnPopupClosedAsync(e);
    }
}
