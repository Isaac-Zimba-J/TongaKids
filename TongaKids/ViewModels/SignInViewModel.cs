using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TongaKids.Services;

namespace TongaKids.ViewModels;

public sealed partial class SignInViewModel(
    IAuthService auth,
    IDialogService dialogs) : ObservableObject
{
    [ObservableProperty] private string _contact = string.Empty;
    [ObservableProperty] private string _password = string.Empty;
    [ObservableProperty] private string _error = string.Empty;
    [ObservableProperty] private bool _hasError;

    [RelayCommand]
    private async Task SignInAsync()
    {
        HasError = false;

        if (!await auth.SignInAsync(Contact, Password))
        {
            Error = "That password did not match. Please try again.";
            HasError = true;
            return;
        }

        await Shell.Current.GoToAsync("//profiles");
    }

    [RelayCommand]
    private async Task ForgotPasswordAsync()
    {
        var answer = await dialogs.PromptAsync(
            "Reset password", AuthService.SecurityQuestion, "Your answer", "Next");

        if (string.IsNullOrWhiteSpace(answer))
        {
            return;
        }

        var newPassword = await dialogs.PromptAsync(
            "Reset password", "Choose a new password (at least 6 characters)",
            "New password", "Save", isPassword: true);

        if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 6)
        {
            return;
        }

        var ok = await auth.ResetPasswordAsync(Contact, answer, newPassword);

        await dialogs.AlertAsync(
            ok ? "Password changed" : "That answer did not match",
            ok ? "You can sign in with your new password." : "Please try again.");
    }
}
