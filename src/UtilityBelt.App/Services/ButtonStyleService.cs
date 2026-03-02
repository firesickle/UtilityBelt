using System.Globalization;
using System.Windows.Media;

namespace UtilityBelt.App.Services;

public static class ButtonStyleService
{
    public static Brush GetBackground(string? color, Brush fallback)
        => TryParseColor(color, out var c) ? new SolidColorBrush(c) : fallback;

    public static Brush GetForeground(string? color, Brush fallback)
        => TryParseColor(color, out var c) ? new SolidColorBrush(c) : fallback;

    public static Brush GetBorderFromBackground(string? backgroundColor, Brush fallback)
    {
        if (!TryParseColor(backgroundColor, out var c))
            return fallback;

        // Slightly lighter border so it "highlights" the button.
        var border = AdjustBrightness(c, +0.08);
        return new SolidColorBrush(border);
    }

    private static bool TryParseColor(string? text, out Color color)
    {
        color = default;
        if (string.IsNullOrWhiteSpace(text)) return false;

        try
        {
            // Supports named colors and #RGB/#ARGB/#RRGGBB/#AARRGGBB
            var obj = new ColorConverter().ConvertFromString(null, CultureInfo.InvariantCulture, text.Trim());
            if (obj is Color c)
            {
                color = c;
                return true;
            }

            return false;
        }
        catch
        {
            return false;
        }
    }

    private static Color AdjustBrightness(Color c, double delta)
    {
        // delta in [-1, +1]
        byte Adj(byte v)
        {
            var nv = v / 255.0;
            nv = Math.Clamp(nv + delta, 0, 1);
            return (byte)Math.Round(nv * 255);
        }

        return Color.FromArgb(c.A, Adj(c.R), Adj(c.G), Adj(c.B));
    }
}
