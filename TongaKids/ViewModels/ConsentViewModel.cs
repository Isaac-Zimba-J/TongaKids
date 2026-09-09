using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TongaKids.Services;

namespace TongaKids.ViewModels;

public sealed partial class ConsentViewModel(IParentalGateService gate) : ObservableObject
{
    public const int CurrentConsentVersion = 1;

    [ObservableProperty] private bool _hasConsented;
    [ObservableProperty] private string _statusText = "Consent has not been given.";

    [RelayCommand]
    public async Task LoadAsync()
    {
        var settings = await gate.GetSettingsAsync();
        HasConsented = settings.ConsentGivenAt is not null;
        StatusText = HasConsented
            ? $"Consent given on {settings.ConsentGivenAt:d MMMM yyyy}."
            : "Consent has not been given.";
    }

    [RelayCommand]
    private async Task GiveConsentAsync()
    {
        var settings = await gate.GetSettingsAsync();
        settings.ConsentGivenAt = DateTime.UtcNow;
        settings.ConsentVersion = CurrentConsentVersion;
        await gate.SaveSettingsAsync(settings);
        await LoadAsync();
    }

    [RelayCommand]
    private async Task WithdrawConsentAsync()
    {
        var settings = await gate.GetSettingsAsync();
        settings.ConsentGivenAt = null;
        await gate.SaveSettingsAsync(settings);
        await LoadAsync();
    }
}
