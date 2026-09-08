using Microsoft.Maui.Controls.Shapes;
using Path = Microsoft.Maui.Controls.Shapes.Path;

namespace TongaKids.Controls;

/// <summary>
/// Five-star lesson result. DESIGN.md "Progress &amp; Rewards": unearned stars are
/// outline Sky Blue, earned are filled Amber.
/// </summary>
/// <remarks>
/// Drawn as a vector Path rather than a Material Symbols glyph. In the Outlined
/// symbol font the "star" ligature is itself an outline — filling it needs the
/// variable font's FILL axis, which MAUI does not expose — so earned and unearned
/// stars rendered identically and the reward conveyed nothing.
/// </remarks>
public class StarRating : ContentView
{
    // The only place hex literals are allowed: DESIGN.md states these two colours in
    // prose rather than the token list, and this control has no XAML in which to
    // resolve StaticResource.
    private static readonly Color EarnedColor = Color.FromArgb("#ff8a00");
    private static readonly Color UnearnedColor = Color.FromArgb("#58cafe");

    /// <summary>Standard five-point star on a 24x24 canvas.</summary>
    private const string StarPath =
        "M12 17.27L18.18 21l-1.64-7.03L22 9.24l-7.19-.61L12 2 9.19 8.63 2 9.24l5.46 4.73L5.82 21z";

    public static readonly BindableProperty EarnedProperty =
        BindableProperty.Create(nameof(Earned), typeof(int), typeof(StarRating), 0,
            propertyChanged: (b, _, _) => ((StarRating)b).Rebuild());

    public static readonly BindableProperty TotalProperty =
        BindableProperty.Create(nameof(Total), typeof(int), typeof(StarRating), 5,
            propertyChanged: (b, _, _) => ((StarRating)b).Rebuild());

    public static readonly BindableProperty StarSizeProperty =
        BindableProperty.Create(nameof(StarSize), typeof(double), typeof(StarRating), 32d,
            propertyChanged: (b, _, _) => ((StarRating)b).Rebuild());

    public int Earned
    {
        get => (int)GetValue(EarnedProperty);
        set => SetValue(EarnedProperty, value);
    }

    public int Total
    {
        get => (int)GetValue(TotalProperty);
        set => SetValue(TotalProperty, value);
    }

    public double StarSize
    {
        get => (double)GetValue(StarSizeProperty);
        set => SetValue(StarSizeProperty, value);
    }

    private readonly HorizontalStackLayout _row = new()
    {
        Spacing = 6,
        HorizontalOptions = LayoutOptions.Center
    };

    public StarRating()
    {
        Content = _row;
        Rebuild();
    }

    private void Rebuild()
    {
        _row.Clear();

        for (var i = 0; i < Total; i++)
        {
            var isEarned = i < Earned;

            _row.Add(new Path
            {
                Data = (Geometry)new PathGeometryConverter()
                    .ConvertFromInvariantString(StarPath)!,
                Aspect = Stretch.Uniform,
                WidthRequest = StarSize,
                HeightRequest = StarSize,
                Fill = isEarned ? EarnedColor : Colors.Transparent,
                Stroke = isEarned ? EarnedColor : UnearnedColor,
                StrokeThickness = isEarned ? 0 : 1.5,
                VerticalOptions = LayoutOptions.Center
            });
        }
    }
}
