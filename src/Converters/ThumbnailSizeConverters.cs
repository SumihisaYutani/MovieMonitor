using System.Globalization;
using System.Windows.Data;

namespace MovieMonitor.Converters;

/// <summary>
/// サムネイルサイズからカード幅への変換コンバーター
/// </summary>
public class ThumbnailSizeToWidthConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is int thumbnailSize)
        {
            // サムネイルサイズ + パディング + ボーダー
            return Math.Max(100, thumbnailSize + 40); // 最小100px
        }
        return 280; // デフォルト値
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// サムネイルサイズからカード高さへの変換コンバーター
/// </summary>
public class ThumbnailSizeToHeightConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is int thumbnailSize)
        {
            // サムネイル高さ = サムネイルサイズ * 0.75 (16:12比率)
            // カード高さ = サムネイル高さ + 情報エリア（80px） + ボタンエリア（50px）
            var thumbnailHeight = (int)(thumbnailSize * 0.75);
            return Math.Max(150, thumbnailHeight + 130); // 最小150px
        }
        return 320; // デフォルト値
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// サムネイルサイズから実際のサムネイル表示高さへの変換コンバーター
/// </summary>
public class ThumbnailSizeToImageHeightConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is int thumbnailSize)
        {
            // 16:12比率でサムネイル高さを計算
            return Math.Max(75, (int)(thumbnailSize * 0.75)); // 最小75px
        }
        return 180; // デフォルト値
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}