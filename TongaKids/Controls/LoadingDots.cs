using SkiaSharp;
using SkiaSharp.Views.Maui;
using SkiaSharp.Views.Maui.Controls;
using TongaKids.Animations;

namespace TongaKids.Controls;

/// <summary>A looping three-dot loading indicator in the app's palette.</summary>
public class LoadingDots : ContentView
{
    public static readonly BindableProperty DotColorProperty =
        BindableProperty.Create(nameof(DotColor), typeof(Color), typeof(LoadingDots),
            Colors.Orange, propertyChanged: (b, _, _) => ((LoadingDots)b).ReadColor());

    public Color DotColor
    {
        get => (Color)GetValue(DotColorProperty);
        set => SetValue(DotColorProperty, value);
    }

    private readonly SKCanvasView _canvas = new();
    private readonly LoadingDotsDrawable _drawable = new();
    private IDispatcherTimer? _timer;
    private DateTime _startedAt;
    private SKColor _color = SKColors.Orange;

    public LoadingDots()
    {
        InputTransparent = true;
        HeightRequest = 44;
        Content = _canvas;
        _canvas.PaintSurface += OnPaintSurface;
        ReadColor();
    }

    private void ReadColor() => _color = DotColor.ToSKColor();

    public void Start()
    {
        Stop();
        _startedAt = DateTime.UtcNow;

        _timer = Application.Current?.Dispatcher.CreateTimer();
        if (_timer is null)
        {
            return;
        }

        // 30fps, matching the rest of the app's motion budget.
        _timer.Interval = TimeSpan.FromMilliseconds(33);
        _timer.Tick += OnTick;
        _timer.Start();
    }

    private void OnTick(object? sender, EventArgs e) => _canvas.InvalidateSurface();

    public void Stop()
    {
        if (_timer is not null)
        {
            _timer.Tick -= OnTick;
            _timer.Stop();
            _timer = null;
        }
    }

    private void OnPaintSurface(object? sender, SKPaintSurfaceEventArgs e)
    {
        // A 1.1s loop, wrapped so it never stops.
        var elapsed = (DateTime.UtcNow - _startedAt).TotalMilliseconds;
        var progress = elapsed % 1100 / 1100;

        _drawable.Draw(
            e.Surface.Canvas,
            new SKRect(0, 0, e.Info.Width, e.Info.Height),
            progress,
            _color);
    }
}
