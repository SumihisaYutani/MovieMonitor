namespace MovieMonitor.Models;

/// <summary>
/// アプリケーション設定
/// </summary>
public class AppSettings
{
    /// <summary>
    /// スキャン対象ディレクトリ一覧
    /// </summary>
    public List<string> ScanDirectories { get; set; } = new();

    /// <summary>
    /// サムネイルサイズ（ピクセル）
    /// </summary>
    public int ThumbnailSize { get; set; } = 320;

    /// <summary>
    /// デフォルト動画プレーヤーのパス
    /// </summary>
    public string? DefaultPlayer { get; set; }

    /// <summary>
    /// FFmpegのパス
    /// </summary>
    public string? FFmpegPath { get; set; } = @"C:\ffmpeg\bin";

    /// <summary>
    /// UIテーマ
    /// </summary>
    public AppTheme Theme { get; set; } = AppTheme.Light;

    /// <summary>
    /// 最後のスキャン実行日時
    /// </summary>
    public DateTime? LastScanDate { get; set; }

    /// <summary>
    /// 対応動画形式一覧
    /// </summary>
    public List<VideoFormat> SupportedFormats { get; set; } = new()
    {
        VideoFormat.Mp4,
        VideoFormat.Avi,
        VideoFormat.Mkv,
        VideoFormat.Ts
    };

    /// <summary>
    /// 自動スキャン有効フラグ
    /// </summary>
    public bool AutoScan { get; set; } = false;

    /// <summary>
    /// 自動スキャン間隔（分）
    /// </summary>
    public int AutoScanInterval { get; set; } = 60;

    /// <summary>
    /// ウィンドウの幅
    /// </summary>
    public double WindowWidth { get; set; } = 1200;

    /// <summary>
    /// ウィンドウの高さ
    /// </summary>
    public double WindowHeight { get; set; } = 800;

    /// <summary>
    /// ウィンドウの左端位置
    /// </summary>
    public double WindowLeft { get; set; } = 100;

    /// <summary>
    /// ウィンドウの上端位置
    /// </summary>
    public double WindowTop { get; set; } = 100;

    /// <summary>
    /// ウィンドウの最大化状態
    /// </summary>
    public bool WindowMaximized { get; set; } = false;
}

/// <summary>
/// アプリケーションテーマ
/// </summary>
public enum AppTheme
{
    Light,
    Dark
}

/// <summary>
/// ディレクトリパス設定
/// </summary>
public class DirectoryPaths
{
    /// <summary>
    /// アプリケーションのベースディレクトリ
    /// </summary>
    public string BaseDirectory { get; }

    /// <summary>
    /// データベースファイルパス
    /// </summary>
    public string DatabasePath { get; }

    /// <summary>
    /// サムネイルディレクトリ
    /// </summary>
    public string ThumbnailDirectory { get; }

    /// <summary>
    /// ログディレクトリ
    /// </summary>
    public string LogDirectory { get; }

    /// <summary>
    /// 一時ファイルディレクトリ
    /// </summary>
    public string TempDirectory { get; }

    /// <summary>
    /// 設定ファイルディレクトリ
    /// </summary>
    public string ConfigDirectory { get; }

    /// <summary>
    /// 設定ファイルパス
    /// </summary>
    public string ConfigFilePath { get; }

    public DirectoryPaths()
    {
        // 実行ファイルの場所をベースディレクトリとする
        BaseDirectory = Path.GetDirectoryName(Environment.ProcessPath) 
            ?? AppContext.BaseDirectory;

        DatabasePath = Path.Combine(BaseDirectory, "data", "database.db");
        ThumbnailDirectory = Path.Combine(BaseDirectory, "thumbnails");
        LogDirectory = Path.Combine(BaseDirectory, "logs");
        TempDirectory = Path.Combine(BaseDirectory, "temp");
        ConfigDirectory = Path.Combine(BaseDirectory, "config");
        ConfigFilePath = Path.Combine(ConfigDirectory, "appsettings.json");
    }

    /// <summary>
    /// 必要なディレクトリを作成
    /// </summary>
    public void EnsureDirectoriesExist()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(DatabasePath)!);
        Directory.CreateDirectory(ThumbnailDirectory);
        Directory.CreateDirectory(LogDirectory);
        Directory.CreateDirectory(TempDirectory);
        Directory.CreateDirectory(ConfigDirectory);
    }
}