using SkiaSharp;

namespace TongaKids.Animations;

/// <summary>
/// Three dots that rise and fall in sequence, like something breathing. Replaces
/// the flat progress bar on the splash screen, which read as a solid slab
/// against the warm gradient and made the app feel stalled rather than starting.
/// </summary>
public sealed class LoadingDotsDrawable
{
    private const int DotCount = 3;

    private readonly SKPaint _paint = new()
    {
        IsAntialias = true,
        Style = SKPaintStyle.Fill
    };

    /// <param name="progress">Loops 0 to 1; the dots cycle continuously.</param>
    public void Draw(SKCanvas canvas, SKRect bounds, double progress, SKColor color)
    {
        canvas.Clear(SKColors.Transparent);

        var radius = Math.Min(bounds.Height * 0.28f, bounds.Width / (DotCount * 4f));
        var spacing = radius * 3.2f;
        var totalWidth = spacing * (DotCount - 1);
        var startX = bounds.MidX - totalWidth / 2;
        var baseY = bounds.MidY;
        var lift = radius * 1.4f;

        for (var i = 0; i < DotCount; i++)
        {
            // Each dot trails the one before it, so the motion reads left to right.
            var phase = (progress * 2 - i * 0.18) % 1.0;
            if (phase < 0)
            {
                phase += 1.0;
            }

            // A single smooth hop, then rest: sin over the first half of the cycle.
            var hop = phase < 0.5 ? Math.Sin(phase * 2 * Math.PI * 0.5) : 0;

            _paint.Color = color.WithAlpha((byte)(160 + 95 * hop));

            var y = baseY - (float)(hop * lift);
            var r = radius * (float)(0.85 + 0.25 * hop);

            canvas.DrawCircle(startX + spacing * i, y, r, _paint);
        }
    }
}
