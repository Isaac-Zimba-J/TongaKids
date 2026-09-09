using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TongaKids.Services;

namespace TongaKids.ViewModels;

public sealed partial class ParentalGateViewModel(IParentalGateService gate) : ObservableObject
{
    [ObservableProperty] private string _pin = string.Empty;
    [ObservableProperty] private string _instruction = string.Empty;
    [ObservableProperty] private string _error = string.Empty;
    [ObservableProperty] private bool _hasError;

    private bool _isFirstTime;

    [RelayCommand]
    public async Task LoadAsync()
    {
        _isFirstTime = !await gate.HasPinAsync();
        Instruction = _isFirstTime
            ? "Choose a 4-digit PIN. You will need it to see your child's progress."
            : "Enter your 4-digit PIN.";
    }

    [RelayCommand]
    private async Task SubmitAsync()
    {
        HasError = false;

        if (Pin.Length != 4 || !Pin.All(char.IsDigit))
        {
            Error = "The PIN must be 4 digits.";
            HasError = true;
            return;
        }

        if (_isFirstTime)
        {
            await gate.SetPinAsync(Pin);
        }
        else if (!await gate.VerifyPinAsync(Pin))
        {
            Error = "That PIN is not correct.";
            HasError = true;
            Pin = string.Empty;
            return;
        }

        await Shell.Current.GoToAsync("parentdashboard");
    }
}
