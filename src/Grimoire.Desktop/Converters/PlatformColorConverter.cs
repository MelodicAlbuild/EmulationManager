using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;
using Grimoire.Shared.Enums;

namespace Grimoire.Desktop.Converters;

public class PlatformColorConverter : IValueConverter
{
    public static readonly PlatformColorConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is PlatformType platform)
        {
            var hex = platform switch
            {
                PlatformType.NintendoSwitch => "#e74c3c",
                PlatformType.NintendoDS => "#4a9e6e",
                PlatformType.Nintendo3DS => "#5cacce",
                PlatformType.GameBoy => "#bb8fce",
                _ => "#8a8078"
            };
            return SolidColorBrush.Parse(hex);
        }
        return SolidColorBrush.Parse("#8a8078");
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

public class PlatformBgConverter : IValueConverter
{
    public static readonly PlatformBgConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is PlatformType platform)
        {
            var hex = platform switch
            {
                PlatformType.NintendoSwitch => "#c0392b25",
                PlatformType.NintendoDS => "#4a9e6e25",
                PlatformType.Nintendo3DS => "#5cacce25",
                PlatformType.GameBoy => "#9b59b625",
                _ => "#8a807825"
            };
            return SolidColorBrush.Parse(hex);
        }
        return SolidColorBrush.Parse("#8a807825");
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

public class PlatformBorderConverter : IValueConverter
{
    public static readonly PlatformBorderConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is PlatformType platform)
        {
            var hex = platform switch
            {
                PlatformType.NintendoSwitch => "#c0392b50",
                PlatformType.NintendoDS => "#4a9e6e50",
                PlatformType.Nintendo3DS => "#5cacce50",
                PlatformType.GameBoy => "#9b59b650",
                _ => "#8a807850"
            };
            return SolidColorBrush.Parse(hex);
        }
        return SolidColorBrush.Parse("#8a807850");
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
