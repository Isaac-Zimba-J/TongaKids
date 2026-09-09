using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TongaKids.Data;
using TongaKids.Services;

namespace TongaKids.ViewModels;

/// <summary>A lesson row nested under its level.</summary>
public sealed record LessonRowModel(int Id, string Title, string Subtitle);

public sealed partial class LevelGroupModel(
    int id, string title, string subtitle, bool isUserCreated) : ObservableObject
{
    public int Id { get; } = id;
    public string Title { get; } = title;
    public string Subtitle { get; } = subtitle;
    public bool IsUserCreated { get; } = isUserCreated;

    public ObservableCollection<LessonRowModel> Lessons { get; } = [];
}

public sealed partial class ManagePhonicsViewModel(
    IContentRepository content,
    IDialogService dialogs) : ObservableObject
{
    public ObservableCollection<LevelGroupModel> Levels { get; } = [];

    [ObservableProperty] private bool _isEmpty;

    [RelayCommand]
    public async Task LoadAsync()
    {
        Levels.Clear();

        foreach (var level in await content.GetLevelsAsync())
        {
            var lessons = await content.GetLessonsAsync(level.Id);

            var group = new LevelGroupModel(
                level.Id,
                level.Title,
                level.IsUserCreated ? "Added by you" : "Came with the app",
                level.IsUserCreated);

            foreach (var lesson in lessons)
            {
                var items = await content.GetItemsAsync(lesson.Id);
                group.Lessons.Add(new LessonRowModel(
                    lesson.Id, lesson.Title,
                    items.Count == 1 ? "1 sound" : $"{items.Count} sounds"));
            }

            Levels.Add(group);
        }

        IsEmpty = Levels.Count == 0;
    }

    [RelayCommand]
    private async Task AddLevelAsync()
    {
        var title = await dialogs.PromptAsync(
            "New level", "What is this level called?", "Level title", "Create");

        if (string.IsNullOrWhiteSpace(title))
        {
            return;
        }

        await content.CreateLevelAsync(title.Trim(), "Added in the app");
        await LoadAsync();
    }

    [RelayCommand]
    private async Task AddLessonAsync(LevelGroupModel? level)
    {
        if (level is null)
        {
            return;
        }

        var title = await dialogs.PromptAsync(
            "New lesson", $"A new lesson in {level.Title}", "Lesson title", "Create");

        if (string.IsNullOrWhiteSpace(title))
        {
            return;
        }

        var lesson = await content.CreateLessonAsync(level.Id, title.Trim());
        await Shell.Current.GoToAsync($"lessoneditor?lessonId={lesson.Id}");
    }

    [RelayCommand]
    private async Task DeleteLevelAsync(LevelGroupModel? level)
    {
        if (level is null)
        {
            return;
        }

        var page = Application.Current?.Windows[0].Page;
        if (page is not null)
        {
            var confirmed = await page.DisplayAlert(
                $"Delete \"{level.Title}\"?",
                level.IsUserCreated
                    ? "This level, its lessons and their sounds will be removed."
                    : "This level came with the app. Deleting it removes it for good; it will not come back with future updates.",
                "Delete", "Cancel");

            if (!confirmed)
            {
                return;
            }
        }

        await content.DeleteLevelAsync(level.Id);
        await LoadAsync();
        await dialogs.ToastAsync($"Deleted \"{level.Title}\"");
    }

    [RelayCommand]
    private static async Task OpenLessonAsync(LessonRowModel? lesson)
    {
        if (lesson is not null)
        {
            await Shell.Current.GoToAsync($"lessoneditor?lessonId={lesson.Id}");
        }
    }
}
