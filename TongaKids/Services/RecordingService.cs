using Plugin.Maui.Audio;
using TongaKids.Data;

namespace TongaKids.Services;

/// <summary>
/// Lets a guardian voice the Chitonga sounds inside the app. The clip list is
/// derived from the content pack, so it can never drift from what the lessons
/// actually ask for.
/// </summary>
public sealed class RecordingService(
    IAudioManager audioManager,
    IContentRepository content,
    IStoryRepository stories,
    IAudioService audio) : IRecordingService
{
    private IAudioRecorder? _recorder;
    private string? _currentKey;

    /// <summary>
    /// A forgotten recording would otherwise run until the device filled up.
    /// Sixty seconds is well beyond any single sound or story page.
    /// </summary>
    public TimeSpan MaxDuration { get; } = TimeSpan.FromSeconds(60);

    public string? CurrentKey => _currentKey;

    public bool IsRecording => _recorder?.IsRecording == true;

    public async Task<List<ClipToRecord>> GetClipsAsync()
    {
        var clips = new List<ClipToRecord>();

        try
        {
            foreach (var level in await content.GetLevelsAsync())
            {
                foreach (var lesson in await content.GetLessonsAsync(level.Id))
                {
                    foreach (var item in await content.GetItemsAsync(lesson.Id))
                    {
                        if (string.IsNullOrWhiteSpace(item.AudioKey))
                        {
                            continue;
                        }

                        clips.Add(new ClipToRecord(
                            item.AudioKey,
                            item.Grapheme,
                            lesson.Title,
                            audio.HasRecording(item.AudioKey)));
                    }
                }
            }

            foreach (var story in await stories.GetStoriesAsync())
            {
                foreach (var page in await stories.GetPagesAsync(story.Id))
                {
                    if (string.IsNullOrWhiteSpace(page.AudioKey))
                    {
                        continue;
                    }

                    clips.Add(new ClipToRecord(
                        page.AudioKey,
                        page.Text,
                        $"{story.Title} - page {page.PageNumber}",
                        audio.HasRecording(page.AudioKey)));
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Recording] clip list failed: {ex}");
        }

        return clips;
    }

    public async Task<bool> EnsurePermissionAsync()
    {
        try
        {
            var status = await Permissions.CheckStatusAsync<Permissions.Microphone>();
            if (status != PermissionStatus.Granted)
            {
                status = await Permissions.RequestAsync<Permissions.Microphone>();
            }

            return status == PermissionStatus.Granted;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Recording] permission failed: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> StartAsync(string audioKey)
    {
        if (IsRecording || string.IsNullOrWhiteSpace(audioKey))
        {
            return false;
        }

        try
        {
            Directory.CreateDirectory(AudioService.RecordingsDirectory);

            _recorder = audioManager.CreateRecorder();
            if (!_recorder.CanRecordAudio)
            {
                System.Diagnostics.Debug.WriteLine("[Recording] device cannot record");
                return false;
            }

            _currentKey = audioKey;

            // Record to a temporary file, so a cancelled or failed take never
            // overwrites a good clip that is already saved.
            await _recorder.StartAsync(TempPath(audioKey));
            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Recording] start failed: {ex.Message}");
            _recorder = null;
            _currentKey = null;
            return false;
        }
    }

    public async Task<bool> StopAsync()
    {
        if (_recorder is null || _currentKey is null)
        {
            return false;
        }

        var key = _currentKey;

        try
        {
            await _recorder.StopAsync();

            var temp = TempPath(key);
            if (!File.Exists(temp) || new FileInfo(temp).Length == 0)
            {
                System.Diagnostics.Debug.WriteLine($"[Recording] '{key}' captured nothing");
                return false;
            }

            // Only now replace the saved clip.
            var final = AudioService.PathFor(key);
            File.Move(temp, final, overwrite: true);
            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Recording] stop failed: {ex.Message}");
            return false;
        }
        finally
        {
            _recorder = null;
            _currentKey = null;
        }
    }

    public async Task CancelAsync()
    {
        if (_recorder is null)
        {
            return;
        }

        var key = _currentKey;

        try
        {
            await _recorder.StopAsync();

            // Discard the take. Any previously saved clip is left alone, so
            // walking away from the screen can never destroy good audio.
            if (key is not null)
            {
                var temp = TempPath(key);
                if (File.Exists(temp))
                {
                    File.Delete(temp);
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Recording] cancel failed: {ex.Message}");
        }
        finally
        {
            _recorder = null;
            _currentKey = null;
        }
    }

    public Task DeleteAsync(string audioKey)
    {
        try
        {
            var path = AudioService.PathFor(audioKey);
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Recording] delete failed: {ex.Message}");
        }

        return Task.CompletedTask;
    }

    private static string TempPath(string audioKey) =>
        Path.Combine(AudioService.RecordingsDirectory, $"{audioKey}.recording.tmp");
}
