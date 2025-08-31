using System.Diagnostics;
using System.Globalization;
using System.Windows.Data;

namespace MovieMonitor.Converters;

/// <summary>
/// デバッグ用コンバーター - 値をコンソールに出力
/// </summary>
public class DebugConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var paramName = parameter?.ToString() ?? "Value";
        Debug.WriteLine($"[DEBUG] {paramName}: {value}");
        return value;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value;
    }
}

/// <summary>
/// ファイル名の長さを表示するコンバーター
/// </summary>
public class FileNameLengthConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string fileName)
        {
            Debug.WriteLine($"[DEBUG] FileName Length: {fileName.Length}, Content: {fileName}");
            return $"{fileName} (Length: {fileName.Length})";
        }
        return value;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value;
    }
}