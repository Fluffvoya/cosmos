using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace app.Converters;

/// <summary>
/// Converts an enum value to Visibility by comparing it to the ConverterParameter.
/// Visible when equal, Collapsed otherwise.
/// </summary>
public class EqualityToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value == null || parameter == null)
            return Visibility.Collapsed;

        return value.ToString() == parameter.ToString()
            ? Visibility.Visible
            : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
