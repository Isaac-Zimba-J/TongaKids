using System.Globalization;

namespace TongaKids.Converters;

/// <summary>Locked levels render dimmed, per the mockup.</summary>
public sealed class LockedOpacityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is true ? 0.45 : 1.0;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
