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
        try
        {
            await Task.Run(() =>
            {
                _database = new LiteDatabase(_paths.DatabasePath);
                
                // インデックス作成
                var videoFiles = _database.GetCollection<VideoFile>("video_files");
                videoFiles.EnsureIndex(x => x.FilePath, unique: true);
                videoFiles.EnsureIndex(x => x.FileName);
                videoFiles.EnsureIndex(x => x.FileSize);
                videoFiles.EnsureIndex(x => x.Duration);
                videoFiles.EnsureIndex(x => x.ScanDate);
                videoFiles.EnsureIndex(x => x.IsDeleted);

                _logger.LogInformation("Database initialized at {DatabasePath}", _paths.DatabasePath);
                Log.Logger.LogInfoWithLocation($"Database initialized at {_paths.DatabasePath}");
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize database");
            Log.Logger.LogErrorWithLocation(ex, "Failed to initialize database");
            throw;
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
                var collection = _database!.GetCollection<VideoFile>("video_files");
                var query = collection.Query().Where(x => !x.IsDeleted);

                // ファイル名検索
                if (!string.IsNullOrEmpty(filter.Query))
                {
                    query = query.Where(x => x.FileName.Contains(filter.Query));
                }

                // ファイルサイズフィルタ
                if (filter.MinSize.HasValue)
                {
                    query = query.Where(x => x.FileSize >= filter.MinSize.Value);
                }
                if (filter.MaxSize.HasValue)
                {
                    query = query.Where(x => x.FileSize <= filter.MaxSize.Value);
                }

                // 再生時間フィルタ
                if (filter.MinDuration.HasValue)
                {
                    query = query.Where(x => x.Duration >= filter.MinDuration.Value);
                }
                if (filter.MaxDuration.HasValue)
                {
                    query = query.Where(x => x.Duration <= filter.MaxDuration.Value);
                }

                // ソート
                query = query.OrderByDescending(x => x.ScanDate);

                var results = query.ToList();

                // 件数制限（LiteDBの制限後に適用）
                if (filter.Offset.HasValue)
                {
                    results = results.Skip(filter.Offset.Value).ToList();
                }
                if (filter.Limit.HasValue)
                {
                    results = results.Take(filter.Limit.Value).ToList();
                }

                // 形式フィルタ（LiteDB側でできないため後処理）
                if (filter.Formats.Count > 0)
                {
                    var extensions = filter.Formats.Select(f => f.GetExtension()).ToHashSet();
                    results = results.Where(x =>
                    {
                        var ext = Path.GetExtension(x.FilePath).ToLowerInvariant();
                        return extensions.Contains(ext);
                    }).ToList();
                }

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
        return await SearchVideoFilesAsync(new SearchFilter());
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