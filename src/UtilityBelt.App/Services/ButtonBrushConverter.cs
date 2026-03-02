using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace UtilityBelt.App.Services;

/// <summary>
/// Lightweight converters for ButtonDefinition look & feel.
/// Implemented as static properties so XAML can reference them without a ResourceDictionary.
/// </summary>
public sealed class ButtonBrushConverter : IValueConverter
{
    private enum Mode
    {
        Background,
        Foreground,
        Border
    }

    private readonly Mode _mode;

    private ButtonBrushConverter(Mode mode)
    {
        _mode = mode;
    }

    public static IValueConverter Background { get; } = new ButtonBrushConverter(Mode.Background);
    public static IValueConverter Foreground { get; } = new ButtonBrushConverter(Mode.Foreground);
    public static IValueConverter Border { get; } = new ButtonBrushConverter(Mode.Border);

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var text = value as string;

        // Defaults align with ToolbarWindow.xaml resources.
        var defaultBack = new SolidColorBrush(Color.FromArgb(0xFF, 0x2B, 0x2B, 0x2B));
        var defaultFore = new SolidColorBrush(Colors.White);
        var defaultBorder = new SolidColorBrush(Color.FromArgb(0xFF, 0x3A, 0x3A, 0x3A));

        return _mode switch
        {
            Mode.Background => ButtonStyleService.GetBackground(text, defaultBack),
            Mode.Foreground => ButtonStyleService.GetForeground(text, defaultFore),
            Mode.Border => ButtonStyleService.GetBorderFromBackground(text, defaultBorder),
            _ => defaultBack
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => Binding.DoNothing;
}
