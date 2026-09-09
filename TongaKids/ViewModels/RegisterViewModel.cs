using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TongaKids.Services;

namespace TongaKids.ViewModels;

public sealed partial class RegisterViewModel(IAuthService auth) : ObservableObject
{
    public string SecurityQuestion => AuthService.SecurityQuestion;

    [ObservableProperty] private string _name = string.Empty;
    [ObservableProperty] private string _contact = string.Empty;
    [ObservableProperty] private string _password = string.Empty;
    [ObservableProperty] private string _securityAnswer = string.Empty;
    [ObservableProperty] private string _error = string.Empty;
    [ObservableProperty] private bool _hasError;

    [RelayCommand]
    private async Task SubmitAsync()
    {
        HasError = false;

        // Parent-facing, so plain honest validation messages are correct here.
        if (string.IsNullOrWhiteSpace(Name))
        {
            Fail("Please enter your name.");
            return;
        }

        if (string.IsNullOrWhiteSpace(Contact))
        {
            Fail("Please enter an email address or phone number.");
            return;
        }

        if (Password.Length < 6)
        {
            Fail("Your password needs at least 6 characters.");
            return;
        }

        if (string.IsNullOrWhiteSpace(SecurityAnswer))
        {
            Fail("Please answer the security question so you can reset your password later.");
            return;
        }

        if (!await auth.RegisterAsync(Name, Contact, Password, SecurityAnswer))
        {
            Fail("We could not create the account. Please try again.");
            return;
        }

        await Shell.Current.GoToAsync("//profiles");
    }

    private void Fail(string message)
    {
        Error = message;
        HasError = true;
    }
}
