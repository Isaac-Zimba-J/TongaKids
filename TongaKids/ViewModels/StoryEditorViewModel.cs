using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TongaKids.Data;
using TongaKids.Models;
using TongaKids.Services;

namespace TongaKids.ViewModels;

/// <summary>One page in the editor: its text, and its narration.</summary>
public sealed partial class PageEditModel(StoryPage page, bool hasRecording) : ObservableObject
{
    public int Id { get; } = page.Id;
    public string AudioKey { get; } = page.AudioKey;

    [ObservableProperty] private int _pageNumber = page.PageNumber;
    [ObservableProperty] private string _text = page.Text;
    [ObservableProperty] private bool _isRecorded = hasRecording;
    [ObservableProperty] private bool _isRecording;
    [ObservableProperty] private string _elapsed = string.Empty;

    public string Heading => $"Page {PageNumber}";

    public string RecordGlyph => IsRecording ? "stop_circle" : IsRecorded ? "check_circle" : "mic";

    public string RecordLabel => IsRecording
        ? $"Recording {Elapsed} - tap to stop"
        : IsRecorded ? "Narration recorded" : "No narration yet";

    partial void OnPageNumberChanged(int value) => OnPropertyChanged(nameof(Heading));
    partial void OnIsRecordedChanged(bool value) => RefreshRecordState();
    partial void OnIsRecordingChanged(bool value) => RefreshRecordState();
    partial void OnElapsedChanged(string value) => OnPropertyChanged(nameof(RecordLabel));

    private void RefreshRecordState()
    {
        OnPropertyChanged(nameof(RecordGlyph));
        OnPropertyChanged(nameof(RecordLabel));
    }
}

[QueryProperty(nameof(StoryId), "storyId")]
public sealed partial class StoryEditorViewModel(
    IStoryRepository stories,
    IRecordingService recording,
    IAudioService audio,
    IDialogService dialogs) : ObservableObject, ILeavingAware
{
    [ObservableProperty] private int _storyId;
    [ObservableProperty] private string _title = string.Empty;
    [ObservableProperty] private string _summary = string.Empty;
    [ObservableProperty] private bool _isEmpty;

    public ObservableCollection<PageEditModel> Pages { get; } = [];

    private PageEditModel? _active;
    private IDispatcherTimer? _tick;
    private DateTime _startedAt;

    partial void OnStoryIdChanged(int value) => _ = LoadAsync();

    [RelayCommand]
    public async Task LoadAsync()
    {
        if (StoryId <= 0)
        {
            return;
        }

        var story = await stories.GetStoryAsync(StoryId);
        Title = story?.Title ?? "Story";

        Pages.Clear();
        foreach (var page in await stories.GetPagesAsync(StoryId))
        {
            Pages.Add(new PageEditModel(page, audio.HasRecording(page.AudioKey)));
        }

        UpdateSummary();
    }

    private void UpdateSummary()
    {
        IsEmpty = Pages.Count == 0;
        var written = Pages.Count(p => !string.IsNullOrWhiteSpace(p.Text));
        var voiced = Pages.Count(p => p.IsRecorded);
        Summary = Pages.Count == 0
            ? "No pages yet. Add the first one."
            : $"{Pages.Count} pages - {written} written, {voiced} recorded";
    }

    [RelayCommand]
    private async Task AddPageAsync()
    {
        var page = await stories.AddPageAsync(StoryId);
        Pages.Add(new PageEditModel(page, hasRecording: false));
        UpdateSummary();
    }

    /// <summary>Saves a page's text. Called as the guardian types, and on leaving.</summary>
    [RelayCommand]
    private async Task SavePageAsync(PageEditModel? row)
    {
        if (row is null)
        {
            return;
        }

        var page = (await stories.GetPagesAsync(StoryId)).FirstOrDefault(p => p.Id == row.Id);
        if (page is null)
        {
            return;
        }

        page.Text = row.Text ?? string.Empty;
        await stories.SavePageAsync(page);
        UpdateSummary();
    }

    [RelayCommand]
    private async Task DeletePageAsync(PageEditModel? row)
    {
        if (row is null)
        {
            return;
        }

        await stories.DeletePageAsync(row.Id);
        await recording.DeleteAsync(row.AudioKey);
        await LoadAsync();
    }

    [RelayCommand]
    private async Task ToggleRecordAsync(PageEditModel? row)
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

        // Save the text first: recording a page whose words are unsaved would
        // leave the audio and the page out of step.
        await SavePageAsync(row);

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
                "TongaKids Read needs permission to use the microphone so you can record the story.");
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
    private async Task PlayAsync(PageEditModel? row)
    {
        if (row is not null)
        {
            await audio.PlayAsync(row.AudioKey);
        }
    }

    private async Task FinishAsync(PageEditModel row, bool hitLimit = false)
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
                ? $"Saved page {row.PageNumber} at the time limit"
                : $"Saved page {row.PageNumber}");
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

        // Abandon any take rather than saving a half-finished one.
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

        // Text is saved as the guardian leaves, so nothing typed is lost.
        foreach (var row in Pages.ToList())
        {
            await SavePageAsync(row);
        }
    }
}
