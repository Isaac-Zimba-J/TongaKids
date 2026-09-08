namespace TongaKids.Services;

public interface IAudioService
{
    /// <summary>Plays a clip by key. Returns false if the clip does not exist.</summary>
    Task<bool> PlayAsync(string audioKey);

    Task StopAsync();
}
