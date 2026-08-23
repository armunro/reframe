using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Reframe.Converters;

public class BooleanToTextWrappingConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool b && b)
        {
            return TextWrapping.Wrap;
        }
        return TextWrapping.NoWrap;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is TextWrapping tw)
        {
            return tw == TextWrapping.Wrap;
        }
        return false;
    }
}
