using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TongaKids.Data;
using TongaKids.Models;
using TongaKids.Services;

namespace TongaKids.ViewModels;

[QueryProperty(nameof(StoryId), "storyId")]
public sealed partial class StoryReaderViewModel(
    IStoryRepository stories,
    IAudioService audio,
    ILearnerSession session) : ObservableObject, ILeavingAware
{
    [ObservableProperty] private int _storyId;
    [ObservableProperty] private string _pageText = string.Empty;
    [ObservableProperty] private string _imageKey = string.Empty;
    [ObservableProperty] private string _pageLabel = string.Empty;
    [ObservableProperty] private double _readingProgress;
    [ObservableProperty] private string _nextLabel = "Next";

    private List<StoryPage> _pages = [];
    private int _index;
    private int _wordsRead;
    private readonly Stopwatch _timer = new();

    partial void OnStoryIdChanged(int value) => _ = LoadAsync();

    [RelayCommand]
    public async Task LoadAsync()
    {
        if (StoryId <= 0)
        {
            return;
        }

        try
        {
            _pages = await stories.GetPagesAsync(StoryId);
            _index = 0;
            _wordsRead = 0;
            _timer.Restart();
            ShowPage();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Reader] load failed: {ex}");
        }
    }

    private void ShowPage()
    {
        if (_pages.Count == 0)
        {
            PageText = string.Empty;
            PageLabel = string.Empty;
            return;
        }

        var page = _pages[_index];
        PageText = page.Text;
        ImageKey = page.ImageKey;
        PageLabel = $"Page {_index + 1} of {_pages.Count}";
        ReadingProgress = (double)(_index + 1) / _pages.Count;
        NextLabel = _index == _pages.Count - 1 ? "Finish" : "Next";
    }

    [RelayCommand]
    private async Task PlayNarrationAsync()
    {
        if (_pages.Count > 0)
        {
            await audio.PlayAsync(_pages[_index].AudioKey);
        }
    }

    [RelayCommand]
    private async Task NextAsync()
    {
        if (_pages.Count == 0)
        {
            return;
        }

        // Count the page just finished, then move on.
        _wordsRead += _pages[_index].WordCount;

        if (_index < _pages.Count - 1)
        {
            _index++;
            ShowPage();
            return;
        }

        await FinishAsync();
    }

    [RelayCommand]
    private void Previous()
    {
        if (_index > 0)
        {
            _index--;
            ShowPage();
        }
    }

    private async Task FinishAsync()
    {
        _timer.Stop();
        await audio.StopAsync();

        var learner = session.Current;
        if (learner is not null)
        {
            try
            {
                await stories.AddSessionAsync(new ReadingSession
                {
                    LearnerId = learner.Id,
                    StoryId = StoryId,
                    WordsRead = _wordsRead,
                    DurationMs = _timer.ElapsedMilliseconds,
                    StartedAt = DateTime.UtcNow
                });

                var state = await stories.GetReadStateAsync(learner.Id, StoryId)
                            ?? new StoryReadState { LearnerId = learner.Id, StoryId = StoryId };
                state.LastPageRead = _pages.Count;
                state.IsCompleted = true;
                await stories.SaveReadStateAsync(state);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Reader] save failed: {ex}");
            }
        }

        await Shell.Current.GoToAsync("//main/stories");
    }

    public async Task OnLeavingAsync()
    {
        // A reading session is only recorded when the story is finished, so
        // leaving part-way simply stops the clock and the narration.
        _timer.Stop();
        await audio.StopAsync();
    }
}
