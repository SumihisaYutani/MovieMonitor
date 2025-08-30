using FFMpegCore;
using Microsoft.Extensions.Logging;
using MovieMonitor.Models;
using System.Security.Cryptography;
using System.Text;

namespace MovieMonitor.Services;

/// <summary>
/// 動画スキャンサービスの実装
/// </summary>
public class VideoScanService : IVideoScanService
{
    private readonly ILogger<VideoScanService> _logger;
    private readonly IThumbnailService _thumbnailService;
    
    public event EventHandler<ScanProgress>? ProgressChanged;

    public VideoScanService(ILogger<VideoScanService> logger, IThumbnailService thumbnailService)
    {
        _logger = logger;
        _thumbnailService = thumbnailService;
    }

    public async Task<ScanResult> ScanDirectoriesAsync(IEnumerable<string> directories, CancellationToken cancellationToken = default)
    {
        var result = new ScanResult
        {
            StartTime = DateTime.Now
        };

        try
        {
            _logger.LogInformation("Starting video scan in {DirectoryCount} directories", directories.Count());

            var allFiles = new List<string>();
            
            // 全ディレクトリをスキャンしてファイルパス収集
            foreach (var directory in directories)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;

                if (!Directory.Exists(directory))
                {
                    _logger.LogWarning("Directory not found: {Directory}", directory);
                    continue;
                }

                var files = await ScanDirectoryAsync(directory, cancellationToken);
                allFiles.AddRange(files);
            }

            result.FilesFound = allFiles.Count;
            _logger.LogInformation("Found {FileCount} video files", allFiles.Count);

            // プログレス通知
            var progress = new ScanProgress
            {
                TotalFiles = allFiles.Count,
                ProcessedFiles = 0
            };

            // 各ファイルを処理
            var videoFiles = new List<VideoFile>();
            var processedCount = 0;

            foreach (var filePath in allFiles)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;

                try
                {
                    progress.CurrentFile = Path.GetFileName(filePath);
                    ProgressChanged?.Invoke(this, progress);

                    var videoFile = await ProcessVideoFileAsync(filePath, cancellationToken);
                    if (videoFile != null)
                    {
                        videoFiles.Add(videoFile);
                        result.FilesProcessed++;
                    }
                    else
                    {
                        result.FilesFailed++;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to process video file: {FilePath}", filePath);
                    result.Errors.Add($"{Path.GetFileName(filePath)}: {ex.Message}");
                    result.FilesFailed++;
                }

                processedCount++;
                progress.ProcessedFiles = processedCount;
                ProgressChanged?.Invoke(this, progress);
            }

            result.VideoFiles = videoFiles;
            result.EndTime = DateTime.Now;

            _logger.LogInformation("Scan completed. Processed: {Processed}, Failed: {Failed}, Duration: {Duration}",
                result.FilesProcessed, result.FilesFailed, result.Duration);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Video scan failed");
            result.EndTime = DateTime.Now;
            result.Errors.Add($"Scan failed: {ex.Message}");
            throw;
        }
    }

    public async Task<List<string>> ScanDirectoryAsync(string directory, CancellationToken cancellationToken = default)
    {
        var videoFiles = new List<string>();
        
        try
        {
            await Task.Run(() => ScanDirectoryRecursive(directory, videoFiles, cancellationToken), cancellationToken);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Directory scan cancelled: {Directory}", directory);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to scan directory: {Directory}", directory);
        }

        return videoFiles;
    }

    private void ScanDirectoryRecursive(string directory, List<string> videoFiles, CancellationToken cancellationToken)
    {
        try
        {
            // ファイルをチェック
            var files = Directory.GetFiles(directory);
            foreach (var file in files)
            {
                cancellationToken.ThrowIfCancellationRequested();
                
                if (IsVideoFile(file))
                {
                    videoFiles.Add(file);
                }
            }

            // サブディレクトリを再帰的にスキャン
            var subdirectories = Directory.GetDirectories(directory);
            foreach (var subdirectory in subdirectories)
            {
                cancellationToken.ThrowIfCancellationRequested();
                
                try
                {
                    ScanDirectoryRecursive(subdirectory, videoFiles, cancellationToken);
                }
                catch (UnauthorizedAccessException ex)
                {
                    _logger.LogWarning(ex, "Access denied to directory: {Directory}", subdirectory);
                }
                catch (DirectoryNotFoundException ex)
                {
                    _logger.LogWarning(ex, "Directory not found: {Directory}", subdirectory);
                }
            }
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Access denied to directory: {Directory}", directory);
        }
        catch (DirectoryNotFoundException ex)
        {
            _logger.LogWarning(ex, "Directory not found: {Directory}", directory);
        }
    }

    public async Task<VideoFile?> ProcessVideoFileAsync(string filePath, CancellationToken cancellationToken = default)
    {
        try
        {
            if (!File.Exists(filePath))
            {
                return null;
            }

            var fileInfo = new FileInfo(filePath);
            
            // FFmpegでメタデータ取得
            var mediaInfo = await FFProbe.AnalyseAsync(filePath, cancellationToken: cancellationToken);
            
            if (mediaInfo?.PrimaryVideoStream == null)
            {
                _logger.LogWarning("No video stream found in file: {FilePath}", filePath);
                return null;
            }

            var videoStream = mediaInfo.PrimaryVideoStream;

            // サムネイル生成
            string? thumbnailPath = null;
            try
            {
                thumbnailPath = await _thumbnailService.GenerateThumbnailAsync(
                    filePath, mediaInfo.Duration.TotalSeconds, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to generate thumbnail for: {FilePath}", filePath);
            }

            var videoFile = new VideoFile
            {
                Id = GenerateFileId(filePath),
                FilePath = filePath,
                FileName = fileInfo.Name,
                FileSize = fileInfo.Length,
                Duration = mediaInfo.Duration.TotalSeconds,
                Width = videoStream.Width,
                Height = videoStream.Height,
                ThumbnailPath = thumbnailPath,
                CreatedAt = fileInfo.CreationTime,
                ModifiedAt = fileInfo.LastWriteTime,
                ScanDate = DateTime.Now
            };

            return videoFile;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process video file: {FilePath}", filePath);
            return null;
        }
    }

    public bool IsVideoFile(string filePath)
    {
        var extension = Path.GetExtension(filePath).ToLowerInvariant();
        return VideoFormatExtensions.SupportedExtensions.Contains(extension);
    }

    public VideoFormat[] GetSupportedFormats()
    {
        return VideoFormatExtensions.AllFormats;
    }

    private static string GenerateFileId(string filePath)
    {
        using var md5 = MD5.Create();
        var hash = md5.ComputeHash(Encoding.UTF8.GetBytes(filePath.ToLowerInvariant()));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}