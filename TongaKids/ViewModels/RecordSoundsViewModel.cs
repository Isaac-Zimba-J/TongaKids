using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TongaKids.Services;

namespace TongaKids.ViewModels;

/// <summary>One row on the recording screen.</summary>
public sealed partial class ClipRowModel(ClipToRecord clip) : ObservableObject
{
    public string AudioKey { get; } = clip.AudioKey;
    public string Say { get; } = clip.Say;
    public string Context { get; } = clip.Context;

    [ObservableProperty] private bool _isRecorded = clip.IsRecorded;
    [ObservableProperty] private bool _isRecording;

    public string StatusGlyph => IsRecording ? "stop_circle" : IsRecorded ? "check_circle" : "mic";
    public string StatusLabel => IsRecording ? "Recording... tap to stop"
        : IsRecorded ? "Recorded" : "Not recorded yet";

    partial void OnIsRecordedChanged(bool value)
    {
        OnPropertyChanged(nameof(StatusGlyph));
        OnPropertyChanged(nameof(StatusLabel));
    }

    partial void OnIsRecordingChanged(bool value)
    {
        OnPropertyChanged(nameof(StatusGlyph));
        OnPropertyChanged(nameof(StatusLabel));
    }
}

public sealed partial class RecordSoundsViewModel(
    IRecordingService recording,
    IAudioService audio,
    IDialogService dialogs) : ObservableObject
{
    public ObservableCollection<ClipRowModel> Clips { get; } = [];

    [ObservableProperty] private string _summary = string.Empty;

    private ClipRowModel? _active;

    [RelayCommand]
    public async Task LoadAsync()
    {
        Clips.Clear();

        foreach (var clip in await recording.GetClipsAsync())
        {
            Clips.Add(new ClipRowModel(clip));
        }

        UpdateSummary();
    }

    private void UpdateSummary()
    {
        var done = Clips.Count(c => c.IsRecorded);
        Summary = Clips.Count == 0
            ? "No sounds to record yet."
            : $"{done} of {Clips.Count} recorded";
    }

    [RelayCommand]
    private async Task ToggleRecordAsync(ClipRowModel? row)
    {
        if (row is null)
        {
            return;
        }

        // Tapping the row that is already recording stops it.
        if (row.IsRecording)
        {
            var saved = await recording.StopAsync();
            row.IsRecording = false;
            _active = null;

            if (saved)
            {
                row.IsRecorded = true;
                UpdateSummary();
                await dialogs.ToastAsync($"Saved \"{row.Say}\"");
            }
            else
            {
                await dialogs.AlertAsync("Nothing recorded",
                    "That take did not capture any sound. Please try again.");
            }

            return;
        }

        // Only one recording at a time.
        if (_active is not null)
        {
            await recording.StopAsync();
            _active.IsRecording = false;
            _active = null;
        }

        if (!await recording.EnsurePermissionAsync())
        {
            await dialogs.AlertAsync("Microphone needed",
                "TongaKids Read needs permission to use the microphone so you can record the sounds.");
            return;
        }

        if (!await recording.StartAsync(row.AudioKey))
        {
            await dialogs.AlertAsync("Could not start",
                "Recording could not start on this device.");
            return;
        }

        row.IsRecording = true;
        _active = row;
    }

    [RelayCommand]
    private async Task PlayAsync(ClipRowModel? row)
    {
        if (row is not null)
        {
            await audio.PlayAsync(row.AudioKey);
        }
    }

    [RelayCommand]
    private async Task DeleteAsync(ClipRowModel? row)
    {
        if (row is null || !row.IsRecorded)
        {
            return;
        }

        await recording.DeleteAsync(row.AudioKey);
        row.IsRecorded = false;
        UpdateSummary();
    }
}
