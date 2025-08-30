using System.Globalization;
using System.Windows.Data;
using MovieMonitor.Models;

namespace MovieMonitor.Converters;

/// <summary>
/// ソート方向をアイコンに変換するコンバーター
/// </summary>
public class SortDirectionToIconConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is SortDirection direction)
        {
            return direction.GetIcon();
        }
        return "↓";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// ソート方向をツールチップに変換するコンバーター
/// </summary>
public class SortDirectionToTooltipConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is SortDirection direction)
        {
            return direction switch
            {
                SortDirection.Ascending => "昇順でソート中 (クリックで降順に変更)",
                SortDirection.Descending => "降順でソート中 (クリックで昇順に変更)",
                _ => "ソート方向を切り替え"
            };
        }
        return "ソート方向を切り替え";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}