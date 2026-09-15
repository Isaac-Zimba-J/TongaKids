using SkiaSharp;

namespace TongaKids.Animations;

/// <summary>
/// Stars fly outward from the centre, spinning and fading. Paths and paints are
/// built once and reused every frame: allocating inside a draw loop is what
/// makes Skia stutter on cheap hardware.
/// </summary>
public sealed class StarBurstDrawable
{
    private const int MaxStars = 24;

    private readonly SKPath _star = BuildStar();
    private readonly SKPaint _paint = new()
    {
        IsAntialias = true,
        Style = SKPaintStyle.Fill
    };

    private readonly (float Angle, float Distance, float Spin, float Scale)[] _seeds;

    public StarBurstDrawable(int seed = 7)
    {
        var rng = new Random(seed);
        _seeds = new (float, float, float, float)[MaxStars];
        for (var i = 0; i < MaxStars; i++)
        {
            _seeds[i] = (
                Angle: (float)(rng.NextDouble() * Math.PI * 2),
                Distance: 0.35f + (float)rng.NextDouble() * 0.55f,
                Spin: (float)(rng.NextDouble() * 720 - 360),
                Scale: 0.5f + (float)rng.NextDouble() * 0.8f);
        }
    }

    public void Draw(SKCanvas canvas, SKRect bounds, double progress, int starCount,
        SKColor primary, SKColor secondary)
    {
        canvas.Clear(SKColors.Transparent);

        if (progress <= 0)
        {
            return;
        }

        var count = Math.Clamp(starCount * 4, 4, MaxStars);
        var centre = new SKPoint(bounds.MidX, bounds.MidY);
        var reach = Math.Min(bounds.Width, bounds.Height) * 0.5f;

        // Ease out: fast burst, slow drift.
        var eased = (float)(1 - Math.Pow(1 - progress, 3));
        var fade = (float)Math.Clamp(1.0 - Math.Max(0, progress - 0.6) / 0.4, 0, 1);

        for (var i = 0; i < count; i++)
        {
            var (angle, distance, spin, scale) = _seeds[i];

            var x = centre.X + (float)Math.Cos(angle) * reach * distance * eased;
            var y = centre.Y + (float)Math.Sin(angle) * reach * distance * eased;

            _paint.Color = (i % 2 == 0 ? primary : secondary)
                .WithAlpha((byte)(255 * fade));

            canvas.Save();
            canvas.Translate(x, y);
            canvas.RotateDegrees(spin * eased);
            var s = scale * (0.6f + 0.4f * eased) * reach * 0.06f;
            canvas.Scale(s, s);
            canvas.DrawPath(_star, _paint);
            canvas.Restore();
        }
    }

    /// <summary>A five-point star on a 1x1 canvas centred at the origin.</summary>
    private static SKPath BuildStar()
    {
        var path = new SKPath();
        const int points = 5;
        const float outer = 1f;
        const float inner = 0.42f;

        for (var i = 0; i < points * 2; i++)
        {
            var radius = i % 2 == 0 ? outer : inner;
            var angle = Math.PI / points * i - Math.PI / 2;
            var x = (float)(Math.Cos(angle) * radius);
            var y = (float)(Math.Sin(angle) * radius);

            if (i == 0)
            {
                path.MoveTo(x, y);
            }
            else
            {
                path.LineTo(x, y);
            }
        }

        path.Close();
        return path;
    }
}
