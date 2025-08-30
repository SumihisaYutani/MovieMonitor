namespace MovieMonitor.Models;

/// <summary>
/// 動画ファイル検索のフィルター条件
/// </summary>
public class SearchFilter
{
    /// <summary>
    /// ファイル名による検索クエリ
    /// </summary>
    public string? Query { get; set; }

    /// <summary>
    /// 検索対象ディレクトリ一覧
    /// </summary>
    public List<string> Directories { get; set; } = new();

    /// <summary>
    /// 対象ファイル形式一覧
    /// </summary>
    public List<VideoFormat> Formats { get; set; } = new();

    /// <summary>
    /// 最小ファイルサイズ（バイト）
    /// </summary>
    public long? MinSize { get; set; }

    /// <summary>
    /// 最大ファイルサイズ（バイト）
    /// </summary>
    public long? MaxSize { get; set; }

    /// <summary>
    /// 最小再生時間（秒）
    /// </summary>
    public double? MinDuration { get; set; }

    /// <summary>
    /// 最大再生時間（秒）
    /// </summary>
    public double? MaxDuration { get; set; }

    /// <summary>
    /// 取得件数制限
    /// </summary>
    public int? Limit { get; set; }

    /// <summary>
    /// 取得開始位置
    /// </summary>
    public int? Offset { get; set; }

    /// <summary>
    /// ソート条件
    /// </summary>
    public SortCriteria SortBy { get; set; } = SortCriteria.ScanDate;

    /// <summary>
    /// ソート順序
    /// </summary>
    public SortDirection SortDirection { get; set; } = SortDirection.Descending;

    /// <summary>
    /// 空のフィルターかどうか（ソート条件は除く）
    /// </summary>
    public bool IsEmpty =>
        string.IsNullOrEmpty(Query) &&
        Directories.Count == 0 &&
        Formats.Count == 0 &&
        MinSize == null &&
        MaxSize == null &&
        MinDuration == null &&
        MaxDuration == null;
}

/// <summary>
/// ソート条件
/// </summary>
public enum SortCriteria
{
    FileName,       // ファイル名
    FileSize,       // ファイルサイズ
    Duration,       // 再生時間
    Resolution,     // 解像度（幅×高さ）
    ScanDate,       // スキャン日時
    CreatedAt,      // 作成日時
    ModifiedAt      // 更新日時
}

/// <summary>
/// ソート順序
/// </summary>
public enum SortDirection
{
    Ascending,      // 昇順
    Descending      // 降順
}

/// <summary>
/// ソート条件の拡張メソッド
/// </summary>
public static class SortCriteriaExtensions
{
    /// <summary>
    /// ソート条件の表示名を取得
    /// </summary>
    public static string GetDisplayName(this SortCriteria criteria)
    {
        return criteria switch
        {
            SortCriteria.FileName => "ファイル名",
            SortCriteria.FileSize => "ファイルサイズ",
            SortCriteria.Duration => "再生時間",
            SortCriteria.Resolution => "解像度",
            SortCriteria.ScanDate => "スキャン日時",
            SortCriteria.CreatedAt => "作成日時",
            SortCriteria.ModifiedAt => "更新日時",
            _ => throw new ArgumentOutOfRangeException(nameof(criteria))
        };
    }

    /// <summary>
    /// ソート順序の表示名を取得
    /// </summary>
    public static string GetDisplayName(this SortDirection direction)
    {
        return direction switch
        {
            SortDirection.Ascending => "昇順",
            SortDirection.Descending => "降順",
            _ => throw new ArgumentOutOfRangeException(nameof(direction))
        };
    }

    /// <summary>
    /// ソート順序のアイコンを取得
    /// </summary>
    public static string GetIcon(this SortDirection direction)
    {
        return direction switch
        {
            SortDirection.Ascending => "↑",
            SortDirection.Descending => "↓",
            _ => ""
        };
    }

    /// <summary>
    /// すべてのソート条件を取得
    /// </summary>
    public static readonly SortCriteriaItem[] AllCriteria = Enum.GetValues<SortCriteria>()
        .Select(c => new SortCriteriaItem { Value = c, DisplayName = c.GetDisplayName() })
        .ToArray();
}

/// <summary>
/// ソート条件の表示用アイテム
/// </summary>
public class SortCriteriaItem
{
    public SortCriteria Value { get; set; }
    public string DisplayName { get; set; } = string.Empty;
}

/// <summary>
/// サポートされる動画ファイル形式
/// </summary>
public enum VideoFormat
{
    Mp4,
    Avi,
    Mkv,
    Ts
}

/// <summary>
/// VideoFormat列挙型の拡張メソッド
/// </summary>
public static class VideoFormatExtensions
{
    /// <summary>
    /// ファイル拡張子を取得
    /// </summary>
    public static string GetExtension(this VideoFormat format)
    {
        return format switch
        {
            VideoFormat.Mp4 => ".mp4",
            VideoFormat.Avi => ".avi",
            VideoFormat.Mkv => ".mkv",
            VideoFormat.Ts => ".ts",
            _ => throw new ArgumentOutOfRangeException(nameof(format))
        };
    }

    /// <summary>
    /// 表示名を取得
    /// </summary>
    public static string GetDisplayName(this VideoFormat format)
    {
        return format switch
        {
            VideoFormat.Mp4 => "MP4",
            VideoFormat.Avi => "AVI",
            VideoFormat.Mkv => "MKV", 
            VideoFormat.Ts => "TS",
            _ => throw new ArgumentOutOfRangeException(nameof(format))
        };
    }

    /// <summary>
    /// ファイルパスから動画形式を判定
    /// </summary>
    public static VideoFormat? FromFilePath(string filePath)
    {
        var extension = Path.GetExtension(filePath).ToLowerInvariant();
        
        return extension switch
        {
            ".mp4" => VideoFormat.Mp4,
            ".avi" => VideoFormat.Avi,
            ".mkv" => VideoFormat.Mkv,
            ".ts" => VideoFormat.Ts,
            _ => null
        };
    }

    /// <summary>
    /// サポートされているファイル形式一覧
    /// </summary>
    public static readonly VideoFormat[] AllFormats = Enum.GetValues<VideoFormat>();

    /// <summary>
    /// サポートされているファイル拡張子一覧
    /// </summary>
    public static readonly string[] SupportedExtensions = AllFormats
        .Select(f => f.GetExtension())
        .ToArray();
}