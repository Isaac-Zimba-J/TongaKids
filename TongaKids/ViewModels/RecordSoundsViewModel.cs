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
    [ObservableProperty] private string _elapsed = string.Empty;

    public string StatusGlyph => IsRecording ? "stop_circle" : IsRecorded ? "check_circle" : "mic";

    public string StatusLabel => IsRecording
        ? $"Recording {Elapsed} - tap to stop"
        : IsRecorded ? "Recorded" : "Not recorded yet";

    partial void OnElapsedChanged(string value) => OnPropertyChanged(nameof(StatusLabel));

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
    private IDispatcherTimer? _tick;
    private DateTime _startedAt;

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
            await FinishAsync(row);
            return;
        }

        // Only one recording at a time; abandon any other take.
        if (_active is not null)
        {
            await recording.CancelAsync();
            StopTicking();
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
        StartTicking();
    }

    /// <summary>Stops, saves and reports. Shared by the stop tap and the time cap.</summary>
    private async Task FinishAsync(ClipRowModel row, bool hitLimit = false)
    {
        StopTicking();

        var saved = await recording.StopAsync();
        row.IsRecording = false;
        row.Elapsed = string.Empty;
        _active = null;

        if (saved)
        {
            row.IsRecorded = true;
            UpdateSummary();
            await dialogs.ToastAsync(hitLimit
                ? $"Saved \"{row.Say}\" at the time limit"
                : $"Saved \"{row.Say}\"");
        }
        else
        {
            await dialogs.AlertAsync("Nothing recorded",
                "That take did not capture any sound. Please try again.");
        }
    }

    private void StartTicking()
    {
        _startedAt = DateTime.UtcNow;

        _tick = Application.Current?.Dispatcher.CreateTimer();
        if (_tick is null)
        {
            return;
        }

        _tick.Interval = TimeSpan.FromMilliseconds(250);
        _tick.Tick += OnTick;
        _tick.Start();
    }

    private async void OnTick(object? sender, EventArgs e)
    {
        var row = _active;
        if (row is null)
        {
            StopTicking();
            return;
        }

        var elapsed = DateTime.UtcNow - _startedAt;
        row.Elapsed = $"{(int)elapsed.TotalSeconds}s";

        // Someone who walks away mid-take would otherwise record until the
        // device filled up. Stop and keep what was captured.
        if (elapsed >= recording.MaxDuration)
        {
            await FinishAsync(row, hitLimit: true);
        }
    }

    private void StopTicking()
    {
        if (_tick is not null)
        {
            _tick.Tick -= OnTick;
            _tick.Stop();
            _tick = null;
        }
    }

    /// <summary>
    /// Called when the screen goes away. Leaving mid-take abandons it rather than
    /// saving, so a half-finished clip can never replace a good one.
    /// </summary>
    public async Task AbandonAsync()
    {
        StopTicking();

        if (_active is not null || recording.IsRecording)
        {
            await recording.CancelAsync();

            if (_active is not null)
            {
                _active.IsRecording = false;
                _active.Elapsed = string.Empty;
                _active = null;
            }
        }
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
