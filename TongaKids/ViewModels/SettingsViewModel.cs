using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TongaKids.Services;

namespace TongaKids.ViewModels;

public sealed partial class SettingsViewModel(
    ILearnerSession session,
    IParentalGateService gate) : ObservableObject
{
    [ObservableProperty] private string _currentLearner = string.Empty;

    [RelayCommand]
    public Task LoadAsync()
    {
        CurrentLearner = session.Current?.Name ?? "No one selected";
        return Task.CompletedTask;
    }

    [RelayCommand]
    private async Task SwitchLearnerAsync()
    {
        session.Clear();
        await Shell.Current.GoToAsync("//profiles");
    }

    [RelayCommand]
    private async Task OpenParentAreaAsync()
    {
        // Always relock before presenting the gate: no stale unlock from earlier.
        gate.Lock();
        await Shell.Current.GoToAsync("parentgate");
    }

#if DEBUG
    [RelayCommand]
    private static async Task OpenSelfCheckAsync() => await Shell.Current.GoToAsync("selfcheck");
#endif
}
