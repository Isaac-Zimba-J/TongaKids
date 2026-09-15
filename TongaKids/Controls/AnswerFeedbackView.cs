using SkiaSharp;
using SkiaSharp.Views.Maui;
using SkiaSharp.Views.Maui.Controls;
using TongaKids.Animations;

namespace TongaKids.Controls;

/// <summary>
/// Draws a tick or a cross over the cards after each answer. Never blocks input.
/// </summary>
public class AnswerFeedbackView : ContentView
{
    private readonly SKCanvasView _canvas = new();
    private readonly AnswerFeedbackDrawable _drawable = new();
    private readonly IAnimationClock _clock;

    private double _progress;
    private bool _isCorrect;
    private SKColor _correct = SKColors.Green;
    private SKColor _wrong = SKColors.Red;

    public AnswerFeedbackView(IAnimationClock clock)
    {
        _clock = clock;

        InputTransparent = true;
        Content = _canvas;
        _canvas.PaintSurface += OnPaintSurface;

        if (Application.Current?.Resources.TryGetValue("Tertiary", out var c) == true
            && c is Color correct)
        {
            _correct = correct.ToSKColor();
        }

        if (Application.Current?.Resources.TryGetValue("Error", out var w) == true
            && w is Color wrong)
        {
            _wrong = wrong.ToSKColor();
        }
    }

    public void Show(bool isCorrect)
    {
        _isCorrect = isCorrect;
        _clock.Start(
            progress =>
            {
                _progress = progress;
                _canvas.InvalidateSurface();
            },
            TimeSpan.FromMilliseconds(600));
    }

    public void Stop() => _clock.Stop();

    private void OnPaintSurface(object? sender, SKPaintSurfaceEventArgs e) =>
        _drawable.Draw(
            e.Surface.Canvas,
            new SKRect(0, 0, e.Info.Width, e.Info.Height),
            _progress,
            _isCorrect,
            _correct,
            _wrong);
}
