using MovieMonitor.Models;

namespace MovieMonitor.Services;

/// <summary>
/// データベースサービスのインターフェース
/// </summary>
public interface IDatabaseService
{
    /// <summary>
    /// データベース初期化
    /// </summary>
    Task InitializeAsync();

    /// <summary>
    /// 動画ファイル保存
    /// </summary>
    Task SaveVideoFileAsync(VideoFile videoFile);

    /// <summary>
    /// 動画ファイル一括保存
    /// </summary>
    Task SaveVideoFilesAsync(IEnumerable<VideoFile> videoFiles);

    /// <summary>
    /// 動画ファイル検索
    /// </summary>
    Task<List<VideoFile>> SearchVideoFilesAsync(SearchFilter filter);

    /// <summary>
    /// 全動画ファイル取得
    /// </summary>
    Task<List<VideoFile>> GetAllVideoFilesAsync();

    /// <summary>
    /// 動画ファイル取得（ID指定）
    /// </summary>
    Task<VideoFile?> GetVideoFileAsync(string id);

    /// <summary>
    /// 動画ファイル削除
    /// </summary>
    Task DeleteVideoFileAsync(string id);

    /// <summary>
    /// 物理削除されたファイルの確認・削除
    /// </summary>
    Task CleanupDeletedFilesAsync();

    /// <summary>
    /// データベース最適化
    /// </summary>
    Task OptimizeDatabaseAsync();
}