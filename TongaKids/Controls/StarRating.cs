namespace TongaKids.Controls;

/// <summary>
/// Five-star lesson result. DESIGN.md "Progress &amp; Rewards": unearned stars are
/// outline Sky Blue, earned are filled Amber.
/// </summary>
public class StarRating : ContentView
{
    // The only place hex literals are allowed: DESIGN.md states these two colours in
    // prose rather than the token list, and this control has no XAML in which to
    // resolve StaticResource.
    private static readonly Color EarnedColor = Color.FromArgb("#ff8a00");
    private static readonly Color UnearnedColor = Color.FromArgb("#58cafe");

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

    private readonly HorizontalStackLayout _row = new() { Spacing = 4 };

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
            _row.Add(new Label
            {
                // Material Symbols ligatures: filled vs outlined star.
                Text = isEarned ? "star" : "star_outline",
                FontFamily = "MaterialSymbols",
                FontSize = StarSize,
                TextColor = isEarned ? EarnedColor : UnearnedColor,
                VerticalOptions = LayoutOptions.Center
            });
        }
    }
}
