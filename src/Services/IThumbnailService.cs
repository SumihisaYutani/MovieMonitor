namespace MovieMonitor.Services;

/// <summary>
/// サムネイルサービスのインターフェース
/// </summary>
public interface IThumbnailService
{
    /// <summary>
    /// サムネイル生成
    /// </summary>
    Task<string?> GenerateThumbnailAsync(string videoPath, double duration, CancellationToken cancellationToken = default);

    /// <summary>
    /// サムネイルパスを取得
    /// </summary>
    string GetThumbnailPath(string videoPath);

    /// <summary>
    /// サムネイルが存在するかチェック
    /// </summary>
    bool ThumbnailExists(string videoPath);

    /// <summary>
    /// 不要なサムネイルファイルを削除
    /// </summary>
    Task CleanupThumbnailsAsync(IEnumerable<string> validVideoPaths);

    /// <summary>
    /// サムネイルディレクトリの初期化
    /// </summary>
    Task InitializeThumbnailDirectoryAsync();
}