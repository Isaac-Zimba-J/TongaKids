using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TongaKids.Data;
using TongaKids.Models;
using TongaKids.Services;

namespace TongaKids.ViewModels;

/// <summary>One sound in the lesson editor: its letters, example word, and audio.</summary>
public sealed partial class SoundEditModel(PhonicsItem item, bool hasRecording) : ObservableObject
{
    public int Id { get; } = item.Id;
    public string AudioKey { get; } = item.AudioKey;
    public bool IsUserCreated { get; } = item.IsUserCreated;

    [ObservableProperty] private string _grapheme = item.Grapheme;
    [ObservableProperty] private string _exampleWord = item.ExampleWord;
    [ObservableProperty] private string _gloss = item.Gloss;
    [ObservableProperty] private bool _isRecorded = hasRecording;
    [ObservableProperty] private bool _isRecording;
    [ObservableProperty] private string _elapsed = string.Empty;

    public string RecordGlyph => IsRecording ? "stop_circle" : IsRecorded ? "check_circle" : "mic";

    public string RecordLabel => IsRecording
        ? $"Recording {Elapsed} - tap to stop"
        : IsRecorded ? "Sound recorded" : "No sound yet";

    partial void OnIsRecordedChanged(bool value) => Refresh();
    partial void OnIsRecordingChanged(bool value) => Refresh();
    partial void OnElapsedChanged(string value) => OnPropertyChanged(nameof(RecordLabel));

    private void Refresh()
    {
        OnPropertyChanged(nameof(RecordGlyph));
        OnPropertyChanged(nameof(RecordLabel));
    }
}

[QueryProperty(nameof(LessonId), "lessonId")]
public sealed partial class LessonEditorViewModel(
    IContentRepository content,
    IRecordingService recording,
    IAudioService audio,
    IDialogService dialogs) : ObservableObject, ILeavingAware
{
    [ObservableProperty] private int _lessonId;
    [ObservableProperty] private string _title = string.Empty;
    [ObservableProperty] private string _summary = string.Empty;
    [ObservableProperty] private bool _isEmpty;

    public ObservableCollection<SoundEditModel> Sounds { get; } = [];

    private SoundEditModel? _active;
    private IDispatcherTimer? _tick;
    private DateTime _startedAt;

    partial void OnLessonIdChanged(int value) => _ = LoadAsync();

    [RelayCommand]
    public async Task LoadAsync()
    {
        if (LessonId <= 0)
        {
            return;
        }

        var lesson = await content.GetLessonAsync(LessonId);
        Title = lesson?.Title ?? "Lesson";

        Sounds.Clear();
        foreach (var item in await content.GetItemsAsync(LessonId))
        {
            Sounds.Add(new SoundEditModel(item, audio.HasRecording(item.AudioKey)));
        }

        UpdateSummary();
    }

    private void UpdateSummary()
    {
        IsEmpty = Sounds.Count == 0;
        var voiced = Sounds.Count(s => s.IsRecorded);
        Summary = Sounds.Count == 0
            ? "No sounds yet. Add the first one."
            : $"{Sounds.Count} sounds - {voiced} recorded";
    }

    [RelayCommand]
    private async Task AddSoundAsync()
    {
        var grapheme = await dialogs.PromptAsync(
            "New sound", "Which letters make this sound? For example BA.", "Letters", "Add");

        if (string.IsNullOrWhiteSpace(grapheme))
        {
            return;
        }

        var item = await content.CreateItemAsync(LessonId, grapheme.Trim().ToUpperInvariant());
        Sounds.Add(new SoundEditModel(item, hasRecording: false));
        UpdateSummary();
    }

    [RelayCommand]
    private async Task SaveSoundAsync(SoundEditModel? row)
    {
        if (row is null)
        {
            return;
        }

        var item = (await content.GetItemsAsync(LessonId)).FirstOrDefault(i => i.Id == row.Id);
        if (item is null)
        {
            return;
        }

        item.Grapheme = (row.Grapheme ?? string.Empty).Trim();
        item.ExampleWord = (row.ExampleWord ?? string.Empty).Trim();
        item.Gloss = (row.Gloss ?? string.Empty).Trim();

        await content.SaveItemAsync(item);
    }

    [RelayCommand]
    private async Task DeleteSoundAsync(SoundEditModel? row)
    {
        if (row is null)
        {
            return;
        }

        await content.DeleteItemAsync(row.Id);
        await recording.DeleteAsync(row.AudioKey);
        await LoadAsync();
    }

    [RelayCommand]
    private async Task ToggleRecordAsync(SoundEditModel? row)
    {
        if (row is null)
        {
            return;
        }

        if (row.IsRecording)
        {
            await FinishAsync(row);
            return;
        }

        await SaveSoundAsync(row);

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
                "TongaKids Read needs permission to use the microphone so you can record the sound.");
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

    [RelayCommand]
    private async Task PlayAsync(SoundEditModel? row)
    {
        if (row is not null)
        {
            await audio.PlayAsync(row.AudioKey);
        }
    }

    private async Task FinishAsync(SoundEditModel row, bool hitLimit = false)
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
                ? $"Saved \"{row.Grapheme}\" at the time limit"
                : $"Saved \"{row.Grapheme}\"");
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

    public async Task OnLeavingAsync()
    {
        StopTicking();

        if (_active is not null || recording.IsRecording)
        {
            await recording.CancelAsync();
            if (_active is not null)
            {
                _active.IsRecording = false;
                _active = null;
            }
        }

        await audio.StopAsync();

        foreach (var row in Sounds.ToList())
        {
            await SaveSoundAsync(row);
        }
    }
}
