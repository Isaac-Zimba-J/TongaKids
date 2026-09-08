using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TongaKids.Data;
using TongaKids.Models;
using TongaKids.Services;

namespace TongaKids.ViewModels;

[QueryProperty(nameof(LessonId), "lessonId")]
public sealed partial class PhonicsLessonViewModel(
    IContentRepository content,
    IAudioService audio) : ObservableObject
{
    [ObservableProperty] private int _lessonId;
    [ObservableProperty] private string _lessonTitle = string.Empty;
    [ObservableProperty] private string _lessonSubtitle = string.Empty;
    [ObservableProperty] private string _grapheme = string.Empty;
    [ObservableProperty] private double _lessonProgress;
    [ObservableProperty] private bool _hasExamples;

    public ObservableCollection<PhonicsItem> Examples { get; } = [];

    private List<PhonicsItem> _items = [];
    private int _index;

    partial void OnLessonIdChanged(int value) => _ = LoadAsync();

    [RelayCommand]
    public async Task LoadAsync()
    {
        if (LessonId <= 0)
        {
            return;
        }

        try
        {
            var lesson = await content.GetLessonAsync(LessonId);
            _items = await content.GetItemsAsync(LessonId);
            _index = 0;

            LessonTitle = lesson?.Title ?? "Lesson";
            ShowCurrent();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Lesson] load failed: {ex}");
        }
    }

    private void ShowCurrent()
    {
        Examples.Clear();

        if (_items.Count == 0)
        {
            Grapheme = string.Empty;
            LessonSubtitle = string.Empty;
            HasExamples = false;
            return;
        }

        var item = _items[_index];
        Grapheme = item.Grapheme;
        LessonSubtitle = $"Sound {_index + 1} of {_items.Count}";
        LessonProgress = (double)(_index + 1) / _items.Count;

        // Only show the example row once the owner has authored a word for it.
        if (!string.IsNullOrWhiteSpace(item.ExampleWord))
        {
            Examples.Add(item);
        }

        HasExamples = Examples.Count > 0;
    }

    [RelayCommand]
    private async Task PlaySoundAsync()
    {
        if (_items.Count > 0)
        {
            await audio.PlayAsync(_items[_index].AudioKey);
        }
    }

    [RelayCommand]
    private void Next()
    {
        if (_index < _items.Count - 1)
        {
            _index++;
            ShowCurrent();
        }
    }

    [RelayCommand]
    private void Previous()
    {
        if (_index > 0)
        {
            _index--;
            ShowCurrent();
        }
    }

    [RelayCommand]
    private async Task PlayGameAsync() => await Shell.Current.GoToAsync($"game?lessonId={LessonId}");
}
