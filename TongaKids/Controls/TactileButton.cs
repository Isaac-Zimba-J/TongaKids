using System.Windows.Input;
using Microsoft.Maui.Controls.Shapes;

namespace TongaKids.Controls;

/// <summary>
/// A "squishy" button: a coloured face sitting on a darker plinth. Pressing it
/// translates the face down onto the plinth, which reads as physical depression.
/// DESIGN.md, "Elevation &amp; Depth", Level 2.
/// </summary>
public class TactileButton : ContentView
{
    private const double DepthPixels = 4;

    public static readonly BindableProperty TextProperty =
        BindableProperty.Create(nameof(Text), typeof(string), typeof(TactileButton), string.Empty);

    public static readonly BindableProperty BackgroundFillProperty =
        BindableProperty.Create(nameof(BackgroundFill), typeof(Color), typeof(TactileButton), Colors.Orange);

    public static readonly BindableProperty TextColorProperty =
        BindableProperty.Create(nameof(TextColor), typeof(Color), typeof(TactileButton), Colors.White);

    public static readonly BindableProperty DepthColorProperty =
        BindableProperty.Create(nameof(DepthColor), typeof(Color), typeof(TactileButton), Colors.DarkOrange);

    public static readonly BindableProperty CornerRadiusProperty =
        BindableProperty.Create(nameof(CornerRadius), typeof(double), typeof(TactileButton), 16d);

    public static readonly BindableProperty CommandProperty =
        BindableProperty.Create(nameof(Command), typeof(ICommand), typeof(TactileButton));

    public static readonly BindableProperty CommandParameterProperty =
        BindableProperty.Create(nameof(CommandParameter), typeof(object), typeof(TactileButton));

    public string Text
    {
        get => (string)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public Color BackgroundFill
    {
        get => (Color)GetValue(BackgroundFillProperty);
        set => SetValue(BackgroundFillProperty, value);
    }

    public Color TextColor
    {
        get => (Color)GetValue(TextColorProperty);
        set => SetValue(TextColorProperty, value);
    }

    public Color DepthColor
    {
        get => (Color)GetValue(DepthColorProperty);
        set => SetValue(DepthColorProperty, value);
    }

    public double CornerRadius
    {
        get => (double)GetValue(CornerRadiusProperty);
        set => SetValue(CornerRadiusProperty, value);
    }

    public ICommand? Command
    {
        get => (ICommand?)GetValue(CommandProperty);
        set => SetValue(CommandProperty, value);
    }

    public object? CommandParameter
    {
        get => GetValue(CommandParameterProperty);
        set => SetValue(CommandParameterProperty, value);
    }

    private readonly Border _plinth;
    private readonly Border _face;
    private readonly Label _label;

    public TactileButton()
    {
        // 48dp is the DESIGN.md minimum hit area; the plinth adds the depth below it.
        MinimumHeightRequest = 48 + DepthPixels;

        _label = new Label
        {
            HorizontalOptions = LayoutOptions.Center,
            VerticalOptions = LayoutOptions.Center,
            FontFamily = "NunitoBold",
            FontSize = 18
        };

        _face = new Border
        {
            Content = _label,
            Padding = new Thickness(24, 12),
            StrokeThickness = 0,
            HorizontalOptions = LayoutOptions.Fill,
            VerticalOptions = LayoutOptions.Fill
        };

        _plinth = new Border
        {
            StrokeThickness = 0,
            HorizontalOptions = LayoutOptions.Fill,
            VerticalOptions = LayoutOptions.Fill
        };

        // The plinth sits DepthPixels lower; the face covers all but that sliver.
        var grid = new Grid();
        grid.Add(_plinth);
        grid.Add(_face);
        _plinth.Margin = new Thickness(0, DepthPixels, 0, 0);
        _face.Margin = new Thickness(0, 0, 0, DepthPixels);

        Content = grid;

        var tap = new TapGestureRecognizer();
        tap.Tapped += OnTapped;
        GestureRecognizers.Add(tap);

        ApplyVisual();
    }

    protected override void OnPropertyChanged(string? propertyName = null)
    {
        base.OnPropertyChanged(propertyName);
        if (propertyName is nameof(Text) or nameof(BackgroundFill) or nameof(TextColor)
            or nameof(DepthColor) or nameof(CornerRadius))
        {
            ApplyVisual();
        }
    }

    private void ApplyVisual()
    {
        _label.Text = Text;
        _label.TextColor = TextColor;
        _face.BackgroundColor = BackgroundFill;
        _plinth.BackgroundColor = DepthColor;
        _face.StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(CornerRadius) };
        _plinth.StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(CornerRadius) };
    }

    private async void OnTapped(object? sender, TappedEventArgs e)
    {
        if (!IsEnabled)
        {
            return;
        }

        // Press: drop the face onto the plinth. Release: spring back.
        await _face.TranslateToAsync(0, DepthPixels, 60, Easing.CubicOut);
        await _face.TranslateToAsync(0, 0, 90, Easing.CubicOut);

        if (Command?.CanExecute(CommandParameter) == true)
        {
            Command.Execute(CommandParameter);
        }
    }
}
