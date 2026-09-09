using System.Collections.ObjectModel;
using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TongaKids.Data;
using TongaKids.Services;

namespace TongaKids.ViewModels;

[QueryProperty(nameof(LessonId), "lessonId")]
public sealed partial class MatchingGameViewModel(
    IContentRepository content,
    IPhonicsEngine engine,
    IAudioService audio) : ObservableObject, ILeavingAware
{
    private const int OptionCount = 4;

    [ObservableProperty] private int _lessonId;
    [ObservableProperty] private string _prompt = string.Empty;
    [ObservableProperty] private bool _canSubmit;

    public ObservableCollection<ChoiceCardModel> Choices { get; } = [];

    private IReadOnlyList<PhonicsQuestion> _questions = [];
    private int _questionIndex;
    private int _correctCount;
    private readonly Stopwatch _timer = new();

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
            var items = await content.GetItemsAsync(LessonId);
            var pool = await content.GetAllItemsAsync();

            // A fresh seed each session so the order is not memorised.
            _questions = engine.BuildQuiz(items, pool, OptionCount, Environment.TickCount);
            _questionIndex = 0;
            _correctCount = 0;
            _timer.Restart();

            ShowQuestion();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Game] load failed: {ex}");
        }
    }

    private void ShowQuestion()
    {
        Choices.Clear();
        CanSubmit = false;

        if (_questionIndex >= _questions.Count)
        {
            return;
        }

        var question = _questions[_questionIndex];
        Prompt = $"Which one says '{question.Target.Grapheme}'?";

        foreach (var option in question.Options)
        {
            Choices.Add(new ChoiceCardModel(option));
        }
    }

    [RelayCommand]
    private void Select(ChoiceCardModel? card)
    {
        if (card is null)
        {
            return;
        }

        foreach (var choice in Choices)
        {
            choice.IsSelected = ReferenceEquals(choice, card);
        }

        CanSubmit = true;
    }

    [RelayCommand]
    private async Task PlayPromptAsync()
    {
        if (_questionIndex < _questions.Count)
        {
            await audio.PlayAsync(_questions[_questionIndex].Target.AudioKey);
        }
    }

    [RelayCommand]
    private async Task SubmitAsync()
    {
        if (!CanSubmit || _questionIndex >= _questions.Count)
        {
            return;
        }

        var selected = Choices.FirstOrDefault(c => c.IsSelected);
        if (selected is null)
        {
            return;
        }

        if (selected.Item.Id == _questions[_questionIndex].Target.Id)
        {
            _correctCount++;
        }

        _questionIndex++;

        if (_questionIndex < _questions.Count)
        {
            ShowQuestion();
            return;
        }

        _timer.Stop();
        await Shell.Current.GoToAsync(
            $"complete?lessonId={LessonId}&correct={_correctCount}" +
            $"&total={_questions.Count}&durationMs={_timer.ElapsedMilliseconds}");
    }

    public async Task OnLeavingAsync()
    {
        // Abandoning a quiz records nothing: a partial attempt is not a result.
        _timer.Stop();
        await audio.StopAsync();
    }
}
