using SkiaSharp;
using SkiaSharp.Views.Maui;
using SkiaSharp.Views.Maui.Controls;
using TongaKids.Animations;

namespace TongaKids.Controls;

/// <summary>
/// A transparent overlay that bursts stars once. Sits above page content and
/// never blocks it.
/// </summary>
public class CelebrationView : ContentView
{
    public static readonly BindableProperty StarCountProperty =
        BindableProperty.Create(nameof(StarCount), typeof(int), typeof(CelebrationView), 3);

    public int StarCount
    {
        get => (int)GetValue(StarCountProperty);
        set => SetValue(StarCountProperty, value);
    }

    private readonly SKCanvasView _canvas = new();
    private readonly StarBurstDrawable _drawable = new();
    private readonly IAnimationClock _clock;

    private double _progress;
    private SKColor _primary = SKColors.Orange;
    private SKColor _secondary = SKColors.Gold;

    public CelebrationView(IAnimationClock clock)
    {
        _clock = clock;

        InputTransparent = true;
        Content = _canvas;
        _canvas.PaintSurface += OnPaintSurface;

        ResolveColors();
    }

    private void ResolveColors()
    {
        if (Application.Current?.Resources.TryGetValue("PrimaryContainer", out var p) == true
            && p is Color primary)
        {
            _primary = primary.ToSKColor();
        }

        if (Application.Current?.Resources.TryGetValue("TertiaryContainer", out var s) == true
            && s is Color secondary)
        {
            _secondary = secondary.ToSKColor();
        }
    }

    public void Play() =>
        _clock.Start(
            progress =>
            {
                _progress = progress;
                _canvas.InvalidateSurface();
            },
            TimeSpan.FromMilliseconds(1400));

    public void Skip() => _clock.SkipToEnd();

    public void Stop() => _clock.Stop();

    private void OnPaintSurface(object? sender, SKPaintSurfaceEventArgs e) =>
        _drawable.Draw(
            e.Surface.Canvas,
            new SKRect(0, 0, e.Info.Width, e.Info.Height),
            _progress,
            StarCount,
            _primary,
            _secondary);
}
