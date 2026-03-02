using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace UtilityBelt.App.Services;

public sealed class PathDataToGeometryConverter : IValueConverter
{
    public static IValueConverter Instance { get; } = new PathDataToGeometryConverter();

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not string s || string.IsNullOrWhiteSpace(s))
            return Geometry.Empty;

        try
        {
            return Geometry.Parse(s);
        }
        catch
        {
            return Geometry.Empty;
        }
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => Binding.DoNothing;
}
