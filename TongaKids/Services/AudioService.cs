using Plugin.Maui.Audio;

namespace TongaKids.Services;

/// <summary>
/// Clip playback. A missing clip is silence, never an error: the app must be
/// fully usable before a single word of Chitonga has been recorded.
/// </summary>
public sealed class AudioService(IAudioManager audioManager) : IAudioService
{
    private IAudioPlayer? _player;

    public async Task<bool> PlayAsync(string audioKey)
    {
        if (string.IsNullOrWhiteSpace(audioKey))
        {
            return false;
        }

        await StopAsync();

        try
        {
            // Raw assets are addressed by their path under Resources/Raw.
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
