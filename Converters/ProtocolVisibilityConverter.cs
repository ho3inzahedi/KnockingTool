using System.Globalization;
using System.Windows.Data;
using KnockingTool.Models;

namespace KnockingTool.Converters;

public class ProtocolVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not KnockProtocol protocol || parameter is not string target)
        {
            return System.Windows.Visibility.Collapsed;
        }

        return protocol.ToString().Equals(target, StringComparison.OrdinalIgnoreCase)
            ? System.Windows.Visibility.Visible
            : System.Windows.Visibility.Collapsed;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
