using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using UtilityBelt.App.Models;

namespace UtilityBelt.App.Services;

/// <summary>
/// Small helper converters for binding UI sizing settings from <see cref="UiSettings"/>.
/// </summary>
public sealed class UiSettingsToThicknessConverter : IValueConverter
{
    public static UiSettingsToThicknessConverter ButtonPadding { get; } = new();

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not UiSettings ui)
            return new Thickness(10, 8, 10, 8);

        return new Thickness(
            left: ui.ButtonPaddingX,
            top: ui.ButtonPaddingY,
            right: ui.ButtonPaddingX,
            bottom: ui.ButtonPaddingY);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => Binding.DoNothing;
}
