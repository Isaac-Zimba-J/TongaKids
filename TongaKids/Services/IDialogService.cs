namespace TongaKids.Services;

/// <summary>
/// Popups and modals, wrapped so the UXDivers Popups dependency lives in exactly
/// one implementation file rather than spreading through view models.
/// </summary>
public interface IDialogService
{
    /// <summary>Asks for one line of text. Returns null if the person cancelled.</summary>
    Task<string?> PromptAsync(
        string title, string message, string placeholder,
        string acceptText = "Save", bool isPassword = false);

    /// <summary>A brief, self-dismissing confirmation.</summary>
    Task ToastAsync(string title);

    /// <summary>An informational popup with a single acknowledge button.</summary>
    Task AlertAsync(string title, string message, string acceptText = "OK");
}
