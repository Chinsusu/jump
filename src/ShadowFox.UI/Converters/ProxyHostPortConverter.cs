using System;
using System.Globalization;
using System.Linq;
using System.Windows.Data;

namespace ShadowFox.UI.Converters;

public class ProxyHostPortConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not string address || string.IsNullOrWhiteSpace(address))
            return string.Empty;

        var trimmed = address.Trim();
        trimmed = trimmed.Replace("http://", "", StringComparison.OrdinalIgnoreCase)
                         .Replace("https://", "", StringComparison.OrdinalIgnoreCase);

        // Drop credentials if present
        var hostPart = trimmed.Split('@').Last();

        // If format host:port:user:pass
        var segments = hostPart.Split(':', StringSplitOptions.RemoveEmptyEntries);
        if (segments.Length >= 4)
        {
            hostPart = $"{segments[0]}:{segments[1]}";
        }

        var hostSplit = hostPart.Split(':', StringSplitOptions.RemoveEmptyEntries);
        if (hostSplit.Length >= 2)
            return $"{hostSplit[0]}:{hostSplit[1]}";

        return hostPart;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => value;
}
