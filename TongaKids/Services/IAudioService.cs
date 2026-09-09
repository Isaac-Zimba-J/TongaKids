namespace TongaKids.Services;

public interface IAudioService
{
    /// <summary>Plays a clip by key. Returns false if no clip exists.</summary>
    Task<bool> PlayAsync(string audioKey);

    Task StopAsync();

    /// <summary>True when a clip for this key has been recorded inside the app.</summary>
    bool HasRecording(string audioKey);
}
