using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TongaKids.Data;
using TongaKids.Models;
using TongaKids.Services;

namespace TongaKids.ViewModels;

public sealed partial class ProfileSelectionViewModel(
    ILearnerRepository learners,
    ILearnerSession session) : ObservableObject
{
    /// <summary>Avatar art shipped with the app, cycled for new learners.</summary>
    private static readonly string[] AvatarKeys =
        ["avatar_chipo", "avatar_mwaka", "avatar_twaambo"];

    private static readonly string[] AccentKeys =
        ["Tertiary", "SecondaryContainer", "PrimaryContainer"];

    public ObservableCollection<Learner> Learners { get; } = [];

    [ObservableProperty]
    private bool _isBusy;

    [RelayCommand]
    public async Task LoadAsync()
    {
        IsBusy = true;
        try
        {
            Learners.Clear();
            foreach (var learner in await learners.GetAllAsync())
            {
                Learners.Add(learner);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Profiles] load failed: {ex}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task SelectAsync(Learner? learner)
    {
        if (learner is null)
        {
            return;
        }

        session.SetCurrent(learner);
        await learners.TouchAsync(learner.Id);
        await Shell.Current.GoToAsync("//main/home");
    }

    [RelayCommand]
    private async Task AddAsync()
    {
        var page = Application.Current?.Windows[0].Page;
        if (page is null)
        {
            return;
        }

        var name = await page.DisplayPromptAsync(
            "Add a learner", "What is your name?", "Save", "Cancel", maxLength: 20);

        if (string.IsNullOrWhiteSpace(name))
        {
            return;
        }

        var index = Learners.Count;
        var created = await learners.AddAsync(new Learner
        {
            Name = name.Trim(),
            AvatarKey = AvatarKeys[index % AvatarKeys.Length],
            AccentColorKey = AccentKeys[index % AccentKeys.Length]
        });

        Learners.Add(created);
    }
}
