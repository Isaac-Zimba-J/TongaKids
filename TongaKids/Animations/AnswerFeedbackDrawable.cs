using SkiaSharp;

namespace TongaKids.Animations;

/// <summary>
/// A tick or a cross that strokes itself on then fades. Deliberately quiet: a
/// wrong answer must read as information, never as punishment.
/// </summary>
public sealed class AnswerFeedbackDrawable
{
    private readonly SKPaint _paint = new()
    {
        IsAntialias = true,
        Style = SKPaintStyle.Stroke,
        StrokeCap = SKStrokeCap.Round,
        StrokeJoin = SKStrokeJoin.Round
    };

    public void Draw(SKCanvas canvas, SKRect bounds, double progress,
        bool isCorrect, SKColor correctColor, SKColor wrongColor)
    {
        canvas.Clear(SKColors.Transparent);

        if (progress <= 0)
        {
            return;
        }

        // Dim the page slightly so the mark is clearly about the answer given,
        // not about the card it happens to overlap. Without this a cross landing
        // across the correct card reads as "this card is wrong".
        using (var scrim = new SKPaint
               {
                   Color = SKColors.White.WithAlpha((byte)(200 * fadeScrim(progress))),
                   Style = SKPaintStyle.Fill
               })
        {
            canvas.DrawRect(bounds, scrim);
        }

        var size = Math.Min(bounds.Width, bounds.Height) * 0.22f;
        var cx = bounds.MidX;
        var cy = bounds.MidY;
        var fade = (float)Math.Clamp(1.0 - Math.Max(0, progress - 0.65) / 0.35, 0, 1);

        var draw = (float)Math.Clamp(progress / 0.45, 0, 1);

        _paint.Color = (isCorrect ? correctColor : wrongColor).WithAlpha((byte)(255 * fade));
        _paint.StrokeWidth = size * 0.16f;

        using var path = new SKPath();

        if (isCorrect)
        {
            var p0 = new SKPoint(cx - size * 0.6f, cy);
            var p1 = new SKPoint(cx - size * 0.15f, cy + size * 0.45f);
            var p2 = new SKPoint(cx + size * 0.65f, cy - size * 0.5f);

            path.MoveTo(p0);
            if (draw <= 0.5f)
            {
                path.LineTo(Lerp(p0, p1, draw / 0.5f));
            }
            else
            {
                path.LineTo(p1);
                path.LineTo(Lerp(p1, p2, (draw - 0.5f) / 0.5f));
            }
        }
        else
        {
            var r = size * 0.5f;
            path.MoveTo(cx - r, cy - r);
            path.LineTo(cx - r + 2 * r * draw, cy - r + 2 * r * draw);
            path.MoveTo(cx + r, cy - r);
            path.LineTo(cx + r - 2 * r * draw, cy - r + 2 * r * draw);
        }

        canvas.DrawPath(path, _paint);
    }

    private static SKPoint Lerp(SKPoint a, SKPoint b, float t) =>
        new(a.X + (b.X - a.X) * t, a.Y + (b.Y - a.Y) * t);

    /// <summary>Scrim rises quickly, then clears with the mark.</summary>
    private static float fadeScrim(double progress) =>
        (float)Math.Clamp(Math.Min(progress / 0.15, 1.0 - Math.Max(0, progress - 0.6) / 0.4), 0, 1);
}
