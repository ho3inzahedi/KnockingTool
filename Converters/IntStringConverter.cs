using System.Globalization;
using System.Windows.Data;

namespace KnockingTool.Converters;

public class IntStringConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value?.ToString() ?? string.Empty;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string text && int.TryParse(text, out var result))
        {
            return result;
        }

        return 0;
    }
}
