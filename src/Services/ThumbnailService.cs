using FFMpegCore;
using FFMpegCore.Enums;
using Microsoft.Extensions.Logging;
using MovieMonitor.Extensions;
using MovieMonitor.Models;
using Serilog;
using System.Security.Cryptography;
using System.Text;

namespace MovieMonitor.Services;

/// <summary>
/// サムネイルサービスの実装
/// </summary>
public class ThumbnailService : IThumbnailService
{
    private readonly DirectoryPaths _paths;
    private readonly IConfigurationService _config;
    private readonly ILogger<ThumbnailService> _logger;
    private bool _ffmpegConfigured = false;

    public ThumbnailService(DirectoryPaths paths, IConfigurationService config, ILogger<ThumbnailService> logger)
    {
        _paths = paths;
        _config = config;
        _logger = logger;
    }

    public async Task InitializeThumbnailDirectoryAsync()
    {
        try
        {
            await ConfigureFFmpegAsync();

            if (!Directory.Exists(_paths.ThumbnailDirectory))
            {
                Directory.CreateDirectory(_paths.ThumbnailDirectory);
                _logger.LogInformation("Created thumbnail directory: {ThumbnailDirectory}", _paths.ThumbnailDirectory);
                Log.Logger.LogInfoWithLocation($"Created thumbnail directory: {_paths.ThumbnailDirectory}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize thumbnail directory");
            Log.Logger.LogErrorWithLocation(ex, "Failed to initialize thumbnail directory");
            throw;
        }
    }

    private async Task ConfigureFFmpegAsync()
    {
        if (_ffmpegConfigured) return;

        try
        {
            var settings = await _config.GetSettingsAsync();
            
            if (!string.IsNullOrEmpty(settings.FFmpegPath) && Directory.Exists(settings.FFmpegPath))
            {
                GlobalFFOptions.Configure(new FFOptions { BinaryFolder = settings.FFmpegPath });
                _ffmpegConfigured = true;
                _logger.LogInformation("FFmpeg configured with path: {FFmpegPath}", settings.FFmpegPath);
                Log.Logger.LogInfoWithLocation($"FFmpeg configured with path: {settings.FFmpegPath}");
            }
            else
            {
                _logger.LogWarning("FFmpeg path not configured or directory not found: {FFmpegPath}", settings.FFmpegPath);
                Log.Logger.LogWarningWithLocation($"FFmpeg path not configured or directory not found: {settings.FFmpegPath}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to configure FFmpeg");
            Log.Logger.LogErrorWithLocation(ex, "Failed to configure FFmpeg");
        }
    }

    public async Task<string?> GenerateThumbnailAsync(string videoPath, double duration, CancellationToken cancellationToken = default)
    {
        try
        {
            await InitializeThumbnailDirectoryAsync();

            var thumbnailPath = GetThumbnailPath(videoPath);
            
            // 既存のサムネイルがあればそれを使用
            if (File.Exists(thumbnailPath))
            {
                Log.Logger.LogDebugWithLocation($"Using existing thumbnail: {thumbnailPath}");
                return thumbnailPath;
            }

            Log.Logger.LogInfoWithLocation($"Generating thumbnail for video: {videoPath}");

            // サムネイル生成（動画の50%地点から）
            var timeSpan = TimeSpan.FromSeconds(duration / 2);
            
            Log.Logger.LogDebugWithLocation($"Attempting FFMpeg.SnapshotAsync - Video: {videoPath}, Output: {thumbnailPath}, TimeSpan: {timeSpan}");

            // FFmpegの実行可能ファイルが存在するか確認
            var settings = await _config.GetSettingsAsync();
            var ffmpegExePath = Path.Combine(settings.FFmpegPath!, "ffmpeg.exe");
            if (!File.Exists(ffmpegExePath))
            {
                var ex = new FileNotFoundException($"FFmpeg executable not found at: {ffmpegExePath}");
                Log.Logger.LogErrorWithLocation(ex, $"FFmpeg executable not found at: {ffmpegExePath}");
                return null;
            }

            Log.Logger.LogDebugWithLocation($"FFmpeg executable found at: {ffmpegExePath}");

            // 動画ファイルの存在と読み取り可能性を確認
            if (!File.Exists(videoPath))
            {
                var ex = new FileNotFoundException($"Video file not found: {videoPath}");
                Log.Logger.LogErrorWithLocation(ex, $"Video file not found: {videoPath}");
                return null;
            }

            var videoInfo = new FileInfo(videoPath);
            Log.Logger.LogDebugWithLocation($"Video file info - Size: {videoInfo.Length} bytes, Path: {videoPath}");

            // FFmpegの詳細オプションを設定
            bool result;
            try
            {
                result = await FFMpeg.SnapshotAsync(
                    videoPath, 
                    thumbnailPath, 
                    new System.Drawing.Size(320, 240), 
                    timeSpan
                );
            }
            catch (Exception ffmpegEx)
            {
                Log.Logger.LogErrorWithLocation(ffmpegEx, $"FFMpeg.SnapshotAsync threw exception for video: {videoPath}");
                throw; // 元の例外処理に渡す
            }

            Log.Logger.LogDebugWithLocation($"FFMpeg.SnapshotAsync returned: {result}");

            if (File.Exists(thumbnailPath))
            {
                var fileInfo = new FileInfo(thumbnailPath);
                _logger.LogDebug("Generated thumbnail: {ThumbnailPath}", thumbnailPath);
                Log.Logger.LogInfoWithLocation($"Successfully generated thumbnail: {thumbnailPath} (Size: {fileInfo.Length} bytes)");
                return thumbnailPath;
            }
            else
            {
                _logger.LogWarning("Thumbnail generation failed for: {VideoPath}", videoPath);
                Log.Logger.LogWarningWithLocation($"Thumbnail file not created for: {videoPath}, FFMpeg result: {result}");
                
                // サムネイルディレクトリの権限をチェック
                try
                {
                    var testFile = Path.Combine(_paths.ThumbnailDirectory, "test.tmp");
                    await File.WriteAllTextAsync(testFile, "test");
                    File.Delete(testFile);
                    Log.Logger.LogDebugWithLocation("Thumbnail directory write test successful");
                }
                catch (Exception dirEx)
                {
                    Log.Logger.LogErrorWithLocation(dirEx, $"Cannot write to thumbnail directory: {_paths.ThumbnailDirectory}");
                }
                
                return null;
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogDebug("Thumbnail generation cancelled for: {VideoPath}", videoPath);
            Log.Logger.LogDebugWithLocation($"Thumbnail generation cancelled for: {videoPath}");
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate thumbnail for: {VideoPath}", videoPath);
            Log.Logger.LogErrorWithLocation(ex, $"Failed to generate thumbnail for: {videoPath}");
            return null;
        }
    }

    public string GetThumbnailPath(string videoPath)
    {
        var fileName = GetThumbnailFileName(videoPath);
        return Path.Combine(_paths.ThumbnailDirectory, fileName);
    }

    public bool ThumbnailExists(string videoPath)
    {
        var thumbnailPath = GetThumbnailPath(videoPath);
        return File.Exists(thumbnailPath);
    }

    public async Task CleanupThumbnailsAsync(IEnumerable<string> validVideoPaths)
    {
        try
        {
            var validThumbnails = validVideoPaths
                .Select(GetThumbnailFileName)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var thumbnailFiles = Directory.GetFiles(_paths.ThumbnailDirectory, "*.png");
            var deletedCount = 0;

            await Task.Run(() =>
            {
                foreach (var thumbnailFile in thumbnailFiles)
                {
                    var fileName = Path.GetFileName(thumbnailFile);
                    
                    if (!validThumbnails.Contains(fileName))
                    {
                        try
                        {
                            File.Delete(thumbnailFile);
                            deletedCount++;
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Failed to delete thumbnail: {ThumbnailFile}", thumbnailFile);
                        }
                    }
                }
            });

            if (deletedCount > 0)
            {
                _logger.LogInformation("Cleaned up {Count} unused thumbnails", deletedCount);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to cleanup thumbnails");
        }
    }

    private string GetThumbnailFileName(string videoPath)
    {
        using var md5 = MD5.Create();
        var hash = md5.ComputeHash(Encoding.UTF8.GetBytes(videoPath.ToLowerInvariant()));
        var hashString = Convert.ToHexString(hash).ToLowerInvariant();
        return $"{hashString}.png";
    }
}