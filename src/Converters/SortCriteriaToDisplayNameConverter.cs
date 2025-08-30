using System.Globalization;
using System.Windows.Data;
using MovieMonitor.Models;

namespace MovieMonitor.Converters;

/// <summary>
/// ソート条件を表示名に変換するコンバーター
/// </summary>
public class SortCriteriaToDisplayNameConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is SortCriteria criteria)
        {
            return criteria.GetDisplayName();
        }
        return value?.ToString() ?? string.Empty;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}