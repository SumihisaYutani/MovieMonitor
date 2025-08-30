using MovieMonitor.Models;

namespace MovieMonitor.Services;

/// <summary>
/// 動画スキャンサービスのインターフェース
/// </summary>
public interface IVideoScanService
{
    /// <summary>
    /// プログレス通知イベント
    /// </summary>
    event EventHandler<ScanProgress>? ProgressChanged;

    /// <summary>
    /// ディレクトリをスキャンして動画ファイルを検索
    /// </summary>
    Task<ScanResult> ScanDirectoriesAsync(IEnumerable<string> directories, CancellationToken cancellationToken = default);

    /// <summary>
    /// 単一ディレクトリのスキャン
    /// </summary>
    Task<List<string>> ScanDirectoryAsync(string directory, CancellationToken cancellationToken = default);

    /// <summary>
    /// 動画ファイル情報の処理
    /// </summary>
    Task<VideoFile?> ProcessVideoFileAsync(string filePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// ファイルが動画ファイルかどうかを判定
    /// </summary>
    bool IsVideoFile(string filePath);

    /// <summary>
    /// サポート形式を取得
    /// </summary>
    VideoFormat[] GetSupportedFormats();
}