using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace MovieMonitor.Converters;

/// <summary>
/// Boolean値をVisibility値に変換するコンバーター（逆変換対応）
/// </summary>
public class BooleanToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not bool boolValue)
            return Visibility.Collapsed;

        // パラメータで逆変換指定
        bool inverse = parameter?.ToString()?.Equals("Inverse", StringComparison.OrdinalIgnoreCase) == true;
        
        if (inverse)
        {
            return boolValue ? Visibility.Collapsed : Visibility.Visible;
        }
        else
        {
            return boolValue ? Visibility.Visible : Visibility.Collapsed;
        }
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not Visibility visibility)
            return false;

        bool inverse = parameter?.ToString()?.Equals("Inverse", StringComparison.OrdinalIgnoreCase) == true;
        
        if (inverse)
        {
            return visibility == Visibility.Collapsed;
        }
        else
        {
            return visibility == Visibility.Visible;
        }
    }
}