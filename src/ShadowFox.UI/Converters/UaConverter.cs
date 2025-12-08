using System;
using System.Globalization;
using System.Text.Json;
using System.Windows.Data;
using ShadowFox.Core.Models;

namespace ShadowFox.UI.Converters;

public sealed class UaConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string json)
        {
            try
            {
                var fp = JsonSerializer.Deserialize<Fingerprint>(json);
                return fp?.UserAgent ?? string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }

        return string.Empty;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => Binding.DoNothing;
}
