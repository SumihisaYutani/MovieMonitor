using LiteDB;
using Microsoft.Extensions.Logging;
using MovieMonitor.Extensions;
using MovieMonitor.Models;
using Serilog;

namespace MovieMonitor.Services;

/// <summary>
/// データベースサービスの実装
/// </summary>
public class DatabaseService : IDatabaseService, IDisposable
{
    private readonly DirectoryPaths _paths;
    private readonly ILogger<DatabaseService> _logger;
    private LiteDatabase? _database;
    private bool _disposed = false;

    public DatabaseService(DirectoryPaths paths, ILogger<DatabaseService> logger)
    {
        _paths = paths;
        _logger = logger;
    }

    public async Task InitializeAsync()
    {
        const int maxRetries = 3;
        const int delayMilliseconds = 1000;

        for (int attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                await Task.Run(() =>
                {
                    // データベースファイルが他のプロセスで使用中の場合を考慮して接続文字列を設定
                    var connectionString = $"Filename={_paths.DatabasePath};Connection=shared";
                    _database = new LiteDatabase(connectionString);
                    
                    // インデックス作成
                    var videoFiles = _database.GetCollection<VideoFile>("video_files");
                    videoFiles.EnsureIndex(x => x.FilePath, unique: true);
                    videoFiles.EnsureIndex(x => x.FileName);
                    videoFiles.EnsureIndex(x => x.FileSize);
                    videoFiles.EnsureIndex(x => x.Duration);
                    videoFiles.EnsureIndex(x => x.ScanDate);
                    videoFiles.EnsureIndex(x => x.IsDeleted);
                    
                    // パフォーマンス向上のための複合インデックス
                    videoFiles.EnsureIndex("IsDeleted_ScanDate", x => new { x.IsDeleted, x.ScanDate });
                    videoFiles.EnsureIndex("IsDeleted_FileName", x => new { x.IsDeleted, x.FileName });
                    videoFiles.EnsureIndex("IsDeleted_FileSize", x => new { x.IsDeleted, x.FileSize });
                    videoFiles.EnsureIndex("IsDeleted_Duration", x => new { x.IsDeleted, x.Duration });

                    _logger.LogInformation("Database initialized at {DatabasePath}", _paths.DatabasePath);
                    Log.Logger.LogInfoWithLocation($"Database initialized at {_paths.DatabasePath}");
                });

                // 成功したらループを抜ける
                return;
            }
            catch (IOException ioEx) when (ioEx.Message.Contains("being used by another process"))
            {
                _logger.LogWarning("Database file is locked, attempt {Attempt}/{MaxRetries}", attempt, maxRetries);
                Log.Logger.LogWarningWithLocation($"Database file is locked, attempt {attempt}/{maxRetries}: {ioEx.Message}");

                if (attempt == maxRetries)
                {
                    _logger.LogError(ioEx, "Failed to initialize database after {MaxRetries} attempts", maxRetries);
                    Log.Logger.LogErrorWithLocation(ioEx, $"Failed to initialize database after {maxRetries} attempts");
                    throw new InvalidOperationException($"データベースファイルが使用中です。他のアプリケーションインスタンスが実行されていないか確認してください。", ioEx);
                }

                // 次の試行前に待機
                await Task.Delay(delayMilliseconds * attempt);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize database on attempt {Attempt}", attempt);
                Log.Logger.LogErrorWithLocation(ex, $"Failed to initialize database on attempt {attempt}");
                throw;
            }
        }
    }

    public async Task SaveVideoFileAsync(VideoFile videoFile)
    {
        ThrowIfDisposed();
        EnsureDatabase();

        try
        {
            await Task.Run(() =>
            {
                var collection = _database!.GetCollection<VideoFile>("video_files");
                collection.Upsert(videoFile);
            });

            _logger.LogDebug("Saved video file: {FilePath}", videoFile.FilePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save video file: {FilePath}", videoFile.FilePath);
            throw;
        }
    }

    public async Task SaveVideoFilesAsync(IEnumerable<VideoFile> videoFiles)
    {
        ThrowIfDisposed();
        EnsureDatabase();

        try
        {
            await Task.Run(() =>
            {
                var collection = _database!.GetCollection<VideoFile>("video_files");
                collection.Upsert(videoFiles);
            });

            var count = videoFiles.Count();
            _logger.LogDebug("Saved {Count} video files", count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save video files");
            throw;
        }
    }

    public async Task<List<VideoFile>> SearchVideoFilesAsync(SearchFilter filter)
    {
        ThrowIfDisposed();
        EnsureDatabase();

        try
        {
            return await Task.Run(() =>
            {
                var sw = System.Diagnostics.Stopwatch.StartNew();
                var collection = _database!.GetCollection<VideoFile>("video_files");
                
                // 基本クエリ: IsDeletedインデックスを利用
                var query = collection.Query().Where(x => !x.IsDeleted);

                // 形式フィルタを先に適用（メモリ使用量削減）
                if (filter.Formats.Count > 0)
                {
                    var extensions = filter.Formats.Select(f => f.GetExtension()).ToHashSet();
                    // ファイルパスの拡張子で事前フィルタ
                    query = query.Where(x => extensions.Any(ext => x.FilePath.EndsWith(ext, StringComparison.OrdinalIgnoreCase)));
                }

                // ファイル名検索（インデックス利用）
                if (!string.IsNullOrEmpty(filter.Query))
                {
                    query = query.Where(x => x.FileName.Contains(filter.Query));
                }

                // ファイルサイズフィルタ（インデックス利用）
                if (filter.MinSize.HasValue)
                {
                    query = query.Where(x => x.FileSize >= filter.MinSize.Value);
                }
                if (filter.MaxSize.HasValue)
                {
                    query = query.Where(x => x.FileSize <= filter.MaxSize.Value);
                }

                // 再生時間フィルタ（インデックス利用）
                if (filter.MinDuration.HasValue)
                {
                    query = query.Where(x => x.Duration >= filter.MinDuration.Value);
                }
                if (filter.MaxDuration.HasValue)
                {
                    query = query.Where(x => x.Duration <= filter.MaxDuration.Value);
                }

                // ソート（複合インデックス利用）
                query = ApplySorting(query, filter.SortBy, filter.SortDirection);

                var results = query.ToList();

                // ページネーション処理（メモリ上で実行）
                if (filter.Offset.HasValue)
                {
                    results = results.Skip(filter.Offset.Value).ToList();
                }
                if (filter.Limit.HasValue)
                {
                    results = results.Take(filter.Limit.Value).ToList();
                }
                
                sw.Stop();
                _logger.LogDebug("Search completed in {ElapsedMs}ms, returned {Count} results", sw.ElapsedMilliseconds, results.Count);

                return results;
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to search video files");
            throw;
        }
    }

    public async Task<List<VideoFile>> GetAllVideoFilesAsync()
    {
        // 全件取得の場合は制限を設けて段階的読み込みを推奨
        return await SearchVideoFilesAsync(new SearchFilter { Limit = 1000 });
    }

    public async Task<VideoFile?> GetVideoFileAsync(string id)
    {
        ThrowIfDisposed();
        EnsureDatabase();

        try
        {
            return await Task.Run(() =>
            {
                var collection = _database!.GetCollection<VideoFile>("video_files");
                return collection.FindById(id);
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get video file: {Id}", id);
            return null;
        }
    }

    public async Task DeleteVideoFileAsync(string id)
    {
        ThrowIfDisposed();
        EnsureDatabase();

        try
        {
            await Task.Run(() =>
            {
                var collection = _database!.GetCollection<VideoFile>("video_files");
                var videoFile = collection.FindById(id);
                if (videoFile != null)
                {
                    videoFile.IsDeleted = true;
                    collection.Update(videoFile);
                }
            });

            _logger.LogDebug("Marked video file as deleted: {Id}", id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete video file: {Id}", id);
            throw;
        }
    }

    public async Task CleanupDeletedFilesAsync()
    {
        ThrowIfDisposed();
        EnsureDatabase();

        try
        {
            var cleanedCount = await Task.Run(() =>
            {
                var collection = _database!.GetCollection<VideoFile>("video_files");
                var allFiles = collection.FindAll();
                int count = 0;

                foreach (var file in allFiles)
                {
                    if (!File.Exists(file.FilePath) && !file.IsDeleted)
                    {
                        file.IsDeleted = true;
                        collection.Update(file);
                        count++;
                    }
                }

                return count;
            });

            if (cleanedCount > 0)
            {
                _logger.LogInformation("Cleaned up {Count} deleted files", cleanedCount);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to cleanup deleted files");
        }
    }

    public async Task CleanupExcludedDirectoriesAsync(IEnumerable<string> scanDirectories)
    {
        ThrowIfDisposed();
        EnsureDatabase();
        try
        {
            var cleanedCount = await Task.Run(() =>
            {
                var collection = _database!.GetCollection<VideoFile>("video_files");
                // ToList()を使って一度にメモリに読み込む
                var allFiles = collection.FindAll().ToList();
                int count = 0;

                foreach (var file in allFiles)
                {
                    if (!file.IsDeleted)
                    {
                        // ファイルのパスがスキャン対象ディレクトリのいずれかに含まれているかチェック
                        bool isInScanDirectory = false;
                        foreach (var scanDir in scanDirectories)
                        {
                            if (file.FilePath.StartsWith(scanDir, StringComparison.OrdinalIgnoreCase))
                            {
                                isInScanDirectory = true;
                                break;
                            }
                        }

                        // スキャン対象外のファイルは削除フラグを立てる
                        if (!isInScanDirectory)
                        {
                            file.IsDeleted = true;
                            collection.Update(file);
                            count++;
                            _logger.LogDebug("Marked as deleted (excluded directory): {FilePath}", file.FilePath);
                        }
                    }
                }
                return count;
            });

            if (cleanedCount > 0)
            {
                _logger.LogInformation("Cleaned up {Count} files from excluded directories", cleanedCount);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to cleanup excluded directories");
        }
    }

    public async Task OptimizeDatabaseAsync()
    {
        ThrowIfDisposed();
        EnsureDatabase();

        try
        {
            await Task.Run(() =>
            {
                // 論理削除されたレコードを物理削除
                var collection = _database!.GetCollection<VideoFile>("video_files");
                var deletedFiles = collection.Query().Where(x => x.IsDeleted).ToList();
                
                foreach (var file in deletedFiles)
                {
                    collection.Delete(file.Id);
                }

                // データベース最適化
                _database.Rebuild();
            });

            _logger.LogInformation("Database optimized");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to optimize database");
            throw;
        }
    }

    /// <summary>
    /// クエリにソート条件を適用
    /// </summary>
    private ILiteQueryable<VideoFile> ApplySorting(ILiteQueryable<VideoFile> query, SortCriteria sortBy, SortDirection direction)
    {
        return sortBy switch
        {
            SortCriteria.FileName => direction == SortDirection.Ascending 
                ? query.OrderBy(x => x.FileName) 
                : query.OrderByDescending(x => x.FileName),
                
            SortCriteria.FileSize => direction == SortDirection.Ascending 
                ? query.OrderBy(x => x.FileSize) 
                : query.OrderByDescending(x => x.FileSize),
                
            SortCriteria.Duration => direction == SortDirection.Ascending 
                ? query.OrderBy(x => x.Duration) 
                : query.OrderByDescending(x => x.Duration),
                
            SortCriteria.Resolution => direction == SortDirection.Ascending 
                ? query.OrderBy(x => x.Width * x.Height) 
                : query.OrderByDescending(x => x.Width * x.Height),
                
            SortCriteria.ScanDate => direction == SortDirection.Ascending 
                ? query.OrderBy(x => x.ScanDate) 
                : query.OrderByDescending(x => x.ScanDate),
                
            SortCriteria.CreatedAt => direction == SortDirection.Ascending 
                ? query.OrderBy(x => x.CreatedAt) 
                : query.OrderByDescending(x => x.CreatedAt),
                
            SortCriteria.ModifiedAt => direction == SortDirection.Ascending 
                ? query.OrderBy(x => x.ModifiedAt) 
                : query.OrderByDescending(x => x.ModifiedAt),
                
            _ => query.OrderByDescending(x => x.ScanDate) // デフォルト
        };
    }

    private void EnsureDatabase()
    {
        if (_database == null)
        {
            throw new InvalidOperationException("Database not initialized. Call InitializeAsync first.");
        }
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(DatabaseService));
        }
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed && disposing)
        {
            _database?.Dispose();
            _database = null;
        }
        _disposed = true;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}