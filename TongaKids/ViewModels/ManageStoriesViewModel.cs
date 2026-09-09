using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TongaKids.Data;
using TongaKids.Models;
using TongaKids.Services;

namespace TongaKids.ViewModels;

/// <summary>A story in the manage list, with its page count.</summary>
public sealed record StoryRowModel(int Id, string Title, string Subtitle, bool IsUserCreated);

public sealed partial class ManageStoriesViewModel(
    IStoryRepository stories,
    IDialogService dialogs) : ObservableObject
{
    public ObservableCollection<StoryRowModel> Stories { get; } = [];

    [RelayCommand]
    public async Task LoadAsync()
    {
        Stories.Clear();

        foreach (var story in await stories.GetStoriesAsync())
        {
            var pages = await stories.GetPagesAsync(story.Id);
            var written = pages.Count(p => !string.IsNullOrWhiteSpace(p.Text));

            Stories.Add(new StoryRowModel(
                story.Id,
                story.Title,
                story.IsUserCreated
                    ? $"{pages.Count} pages, {written} written"
                    : $"{pages.Count} pages - from the app",
                story.IsUserCreated));
        }
    }

    [RelayCommand]
    private async Task CreateAsync()
    {
        var title = await dialogs.PromptAsync(
            "New story", "What is the story called?", "Story title", "Create");

        if (string.IsNullOrWhiteSpace(title))
        {
            return;
        }

        var story = await stories.CreateStoryAsync(title.Trim());
        await Shell.Current.GoToAsync($"storyeditor?storyId={story.Id}");
    }

    [RelayCommand]
    private static async Task OpenAsync(StoryRowModel? row)
    {
        if (row is not null)
        {
            await Shell.Current.GoToAsync($"storyeditor?storyId={row.Id}");
        }
    }
}
