using System.Globalization;
using System.Windows;
using System.Windows.Data;
using KnockingTool.Models;

namespace KnockingTool.Converters;

public class ProtocolPortEnabledConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is KnockProtocol protocol && protocol != KnockProtocol.Icmp;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

public class PortDisplayConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not KnockStep step)
        {
            return string.Empty;
        }

        return step.Protocol == KnockProtocol.Icmp ? "—" : step.Port.ToString();
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
