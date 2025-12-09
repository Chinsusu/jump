using System;
using System.Globalization;
using System.Windows.Data;

namespace ShadowFox.UI.Converters;

public sealed class StatusToActionConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var status = value?.ToString()?.ToLowerInvariant() ?? string.Empty;
        return status.Contains("launch") ? "Stop" : "Start";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
