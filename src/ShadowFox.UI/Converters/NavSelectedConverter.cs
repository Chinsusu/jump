using System;
using System.Globalization;
using System.Windows.Data;

namespace ShadowFox.UI.Converters;

public sealed class NavSelectedConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values.Length < 2) return false;
        var current = values[0]?.ToString();
        var tag = values[1]?.ToString();
        return string.Equals(current, tag, StringComparison.OrdinalIgnoreCase);
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
