using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TongaKids.Data;
using TongaKids.Models;

namespace TongaKids.ViewModels;

public sealed partial class StoryLibraryViewModel(IStoryRepository stories) : ObservableObject
{
    public ObservableCollection<Story> Stories { get; } = [];

    [ObservableProperty] private bool _isEmpty;

    [RelayCommand]
    public async Task LoadAsync()
    {
        try
        {
            Stories.Clear();
            foreach (var story in await stories.GetStoriesAsync())
            {
                Stories.Add(story);
            }

            IsEmpty = Stories.Count == 0;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Library] load failed: {ex}");
        }
    }

    [RelayCommand]
    private static async Task OpenAsync(Story? story)
    {
        if (story is not null)
        {
            await Shell.Current.GoToAsync($"reader?storyId={story.Id}");
        }
    }
}
