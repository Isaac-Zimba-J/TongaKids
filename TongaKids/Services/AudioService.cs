using Plugin.Maui.Audio;

namespace TongaKids.Services;

/// <summary>
/// Clip playback. A missing clip is silence, never an error: the app must be
/// fully usable before a single word of Chitonga has been recorded.
/// </summary>
/// <remarks>
/// Resolution order is deliberate. A clip recorded inside the app wins over the
/// bundled asset, so a guardian or teacher can voice the sounds in their own
/// dialect and hear the result immediately.
/// </remarks>
public sealed class AudioService(IAudioManager audioManager) : IAudioService
{
    /// <summary>Where in-app recordings live. Writable, unlike bundled assets.</summary>
    public static string RecordingsDirectory =>
        Path.Combine(FileSystem.AppDataDirectory, "recordings");

    public static string PathFor(string audioKey) =>
        Path.Combine(RecordingsDirectory, $"{audioKey}.m4a");

    public bool HasRecording(string audioKey) =>
        !string.IsNullOrWhiteSpace(audioKey) && File.Exists(PathFor(audioKey));

    private IAudioPlayer? _player;

    public async Task<bool> PlayAsync(string audioKey)
    {
        if (string.IsNullOrWhiteSpace(audioKey))
        {
            return false;
        }

        await StopAsync();

        // 1. A clip recorded in the app.
        var recorded = PathFor(audioKey);
        if (File.Exists(recorded))
        {
            try
            {
                _player = audioManager.CreatePlayer(File.OpenRead(recorded));
                _player.Play();
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Audio] recorded '{audioKey}' failed: {ex.Message}");
            }
        }

        // 2. A clip shipped with the app.
        try
        {
            await using var stream = await FileSystem.OpenAppPackageFileAsync($"audio/{audioKey}.m4a");

            // The stream must outlive this call, so copy it into memory first.
            var buffer = new MemoryStream();
            await stream.CopyToAsync(buffer);
            buffer.Position = 0;

            _player = audioManager.CreatePlayer(buffer);
            _player.Play();
            return true;
        }
        catch (FileNotFoundException)
        {
            // Not yet recorded. Silence is the designed behaviour.
            System.Diagnostics.Debug.WriteLine($"[Audio] no clip for '{audioKey}'");
            return false;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Audio] '{audioKey}' failed: {ex.Message}");
            return false;
        }
    }

    public Task StopAsync()
    {
        try
        {
            if (_player is not null)
            {
                if (_player.IsPlaying)
                {
                    _player.Stop();
                }

                _player.Dispose();
                _player = null;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Audio] stop failed: {ex.Message}");
        }

        return Task.CompletedTask;
    }
}
