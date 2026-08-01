using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace app.Converters;

/// <summary>
/// Converts a boolean value to a Visibility value.
/// true -> Visible, false -> Collapsed.
/// </summary>
public class BoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        bool flag = value is bool b && b;
        bool invert = parameter is string s && s.Equals("Invert", StringComparison.OrdinalIgnoreCase);

        if (invert) flag = !flag;
        return flag ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        bool flag = value is Visibility v && v == Visibility.Visible;
        bool invert = parameter is string s && s.Equals("Invert", StringComparison.OrdinalIgnoreCase);

        return invert ? !flag : flag;
    }
}
