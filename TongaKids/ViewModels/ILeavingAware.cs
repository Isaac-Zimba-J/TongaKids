namespace TongaKids.ViewModels;

/// <summary>
/// A view model that must release something when its page goes away: audio still
/// playing, a running timer, an open microphone. Pages call this from
/// OnDisappearing, so leaving a screen never leaves work running behind it.
/// </summary>
public interface ILeavingAware
{
    Task OnLeavingAsync();
}
