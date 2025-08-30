using LiteDB;

namespace MovieMonitor.Models;

/// <summary>
/// 動画ファイル情報を表すモデルクラス
/// </summary>
public class VideoFile
{
    /// <summary>
    /// ファイルの一意識別子
    /// </summary>
    [BsonId]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// ファイルの絶対パス
    /// </summary>
    [BsonField("file_path")]
    public string FilePath { get; set; } = string.Empty;

    /// <summary>
    /// ファイル名（拡張子含む）
    /// </summary>
    [BsonField("file_name")]
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// ファイルサイズ（バイト）
    /// </summary>
    [BsonField("file_size")]
    public long FileSize { get; set; }

    /// <summary>
    /// 再生時間（秒）
    /// </summary>
    [BsonField("duration")]
    public double Duration { get; set; }

    /// <summary>
    /// 動画の幅（ピクセル）
    /// </summary>
    [BsonField("width")]
    public int Width { get; set; }

    /// <summary>
    /// 動画の高さ（ピクセル）
    /// </summary>
    [BsonField("height")]
    public int Height { get; set; }

    /// <summary>
    /// サムネイル画像のパス
    /// </summary>
    [BsonField("thumbnail_path")]
    public string? ThumbnailPath { get; set; }

    /// <summary>
    /// ファイル作成日時
    /// </summary>
    [BsonField("created_at")]
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// ファイル最終更新日時
    /// </summary>
    [BsonField("modified_at")]
    public DateTime ModifiedAt { get; set; }

    /// <summary>
    /// データベースへのスキャン日時
    /// </summary>
    [BsonField("scan_date")]
    public DateTime ScanDate { get; set; }

    /// <summary>
    /// 論理削除フラグ
    /// </summary>
    [BsonField("is_deleted")]
    public bool IsDeleted { get; set; } = false;

    /// <summary>
    /// 解像度文字列を取得
    /// </summary>
    public string Resolution => $"{Width}x{Height}";

    /// <summary>
    /// ファイルサイズをフォーマットした文字列を取得
    /// </summary>
    public string FormattedFileSize => FormatFileSize(FileSize);

    /// <summary>
    /// 再生時間をフォーマットした文字列を取得
    /// </summary>
    public string FormattedDuration => FormatDuration(Duration);

    /// <summary>
    /// ファイルサイズをフォーマット
    /// </summary>
    private static string FormatFileSize(long bytes)
    {
        string[] units = { "B", "KB", "MB", "GB", "TB" };
        double size = bytes;
        int unitIndex = 0;

        while (size >= 1024 && unitIndex < units.Length - 1)
        {
            size /= 1024;
            unitIndex++;
        }

        return $"{size:F1} {units[unitIndex]}";
    }

    /// <summary>
    /// 再生時間をフォーマット
    /// </summary>
    private static string FormatDuration(double seconds)
    {
        var timeSpan = TimeSpan.FromSeconds(seconds);
        
        if (timeSpan.TotalHours >= 1)
        {
            return $"{(int)timeSpan.TotalHours}:{timeSpan.Minutes:D2}:{timeSpan.Seconds:D2}";
        }
        else
        {
            return $"{timeSpan.Minutes}:{timeSpan.Seconds:D2}";
        }
    }
}