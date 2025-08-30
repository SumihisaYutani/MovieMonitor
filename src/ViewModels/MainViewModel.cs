using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using MovieMonitor.Models;
using MovieMonitor.Services;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Input;

namespace MovieMonitor.ViewModels;

/// <summary>
/// メインウィンドウのViewModel
/// </summary>
public partial class MainViewModel : ObservableObject
{
    private readonly IConfigurationService _configurationService;
    private readonly IDatabaseService _databaseService;
    private readonly IVideoScanService _videoScanService;
    private readonly ILogger<MainViewModel> _logger;

    [ObservableProperty]
    private ObservableCollection<VideoFile> _videos = new();

    [ObservableProperty]
    private ObservableCollection<VideoFile> _filteredVideos = new();

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _loadingMessage = "";

    [ObservableProperty]
    private string _searchQuery = "";

    [ObservableProperty]
    private string _statusText = "準備完了";

    [ObservableProperty]
    private ScanProgress? _scanProgress;

    [ObservableProperty]
    private AppSettings _settings = new();

    [ObservableProperty]
    private WindowState _windowState = WindowState.Normal;

    /// <summary>
    /// サムネイルサイズ（設定から取得）
    /// </summary>
    public int ThumbnailSize => Settings.ThumbnailSize;

    partial void OnSettingsChanged(AppSettings value)
    {
        OnPropertyChanged(nameof(ThumbnailSize));
    }

    private CancellationTokenSource? _scanCancellationTokenSource;

    public MainViewModel(
        IConfigurationService configurationService,
        IDatabaseService databaseService,
        IVideoScanService videoScanService,
        ILogger<MainViewModel> logger)
    {
        _configurationService = configurationService;
        _databaseService = databaseService;
        _videoScanService = videoScanService;
        _logger = logger;

        // イベントハンドラー登録
        _videoScanService.ProgressChanged += OnScanProgressChanged;
        
        // 初期化
        _ = InitializeAsync();
    }

    private async Task InitializeAsync()
    {
        try
        {
            IsLoading = true;
            LoadingMessage = "初期化中...";

            // データベース初期化
            await _databaseService.InitializeAsync();

            // 設定読み込み
            Settings = await _configurationService.GetSettingsAsync();

            // 既存データ読み込み
            await LoadExistingVideosAsync();

            StatusText = $"動画ファイル {Videos.Count} 件";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize ViewModel");
            StatusText = "初期化エラー";
            ShowErrorMessage("初期化に失敗しました", ex.Message);
        }
        finally
        {
            IsLoading = false;
            LoadingMessage = "";
        }
    }

    private async Task LoadExistingVideosAsync()
    {
        try
        {
            // 初期ロード時は最新1000件のみ
            await PerformSearchAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load existing videos");
        }
    }

    partial void OnSearchQueryChanged(string value)
    {
        // 検索クエリ変更時はデータベースから直接検索
        _ = PerformSearchAsync();
    }

    private async Task PerformSearchAsync()
    {
        try
        {
            var searchFilter = new SearchFilter
            {
                Query = string.IsNullOrWhiteSpace(SearchQuery) ? null : SearchQuery,
                Limit = 1000 // 一度に表示する最大件数
            };

            var results = await _databaseService.SearchVideoFilesAsync(searchFilter);
            
            Application.Current.Dispatcher.Invoke(() =>
            {
                FilteredVideos.Clear();
                Videos.Clear();
                
                foreach (var video in results)
                {
                    Videos.Add(video);
                    FilteredVideos.Add(video);
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to perform search");
        }
    }

    private void OnScanProgressChanged(object? sender, ScanProgress progress)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            ScanProgress = progress;
            LoadingMessage = $"スキャン中... {progress.CurrentFile}";
        });
    }

    public ICommand ScanCommand => new AsyncRelayCommand(ScanAsync);
    
    private async Task ScanAsync()
    {
        if (IsLoading)
        {
            // スキャン中の場合はキャンセル
            _scanCancellationTokenSource?.Cancel();
            return;
        }

        try
        {
            IsLoading = true;
            LoadingMessage = "スキャン準備中...";
            StatusText = "スキャン実行中...";

            _scanCancellationTokenSource = new CancellationTokenSource();

            // スキャン対象ディレクトリを取得
            var directories = await GetScanDirectoriesAsync();
            if (!directories.Any())
            {
                ShowInfoMessage("スキャン対象が見つかりません", "スキャン対象のディレクトリを設定してください。");
                return;
            }

            // スキャン実行
            var result = await _videoScanService.ScanDirectoriesAsync(directories, _scanCancellationTokenSource.Token);

            // 結果をデータベースに保存
            if (result.VideoFiles.Any())
            {
                LoadingMessage = "データベースに保存中...";
                await _databaseService.SaveVideoFilesAsync(result.VideoFiles);
            }

            // 不要ファイルのクリーンアップ
            await _databaseService.CleanupDeletedFilesAsync();

            // UIを更新
            await LoadExistingVideosAsync();

            // 設定の更新（最後のスキャン日時）
            Settings.LastScanDate = DateTime.Now;
            await _configurationService.SaveSettingsAsync(Settings);

            StatusText = $"スキャン完了: {result.FilesProcessed} 件処理 ({result.FilesFailed} 件失敗)";

            if (result.Errors.Any())
            {
                var errorMessage = string.Join("\n", result.Errors.Take(5));
                if (result.Errors.Count > 5)
                {
                    errorMessage += $"\n... その他 {result.Errors.Count - 5} 件のエラー";
                }
                ShowWarningMessage("スキャン中にエラーが発生しました", errorMessage);
            }
        }
        catch (OperationCanceledException)
        {
            StatusText = "スキャンがキャンセルされました";
            _logger.LogInformation("Video scan cancelled by user");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Video scan failed");
            StatusText = "スキャンエラー";
            ShowErrorMessage("スキャンに失敗しました", ex.Message);
        }
        finally
        {
            IsLoading = false;
            LoadingMessage = "";
            ScanProgress = null;
            _scanCancellationTokenSource?.Dispose();
            _scanCancellationTokenSource = null;
        }
    }

    public ICommand PlayVideoCommand => new AsyncRelayCommand<VideoFile>(PlayVideo);
    
    private async Task PlayVideo(VideoFile? video)
    {
        if (video == null) return;

        try
        {
            if (!File.Exists(video.FilePath))
            {
                ShowWarningMessage("ファイルが見つかりません", $"ファイルが存在しません:\n{video.FilePath}");
                return;
            }

            // デフォルトプレーヤーで再生
            var startInfo = new ProcessStartInfo
            {
                FileName = video.FilePath,
                UseShellExecute = true
            };

            Process.Start(startInfo);
            
            _logger.LogDebug("Opened video: {FilePath}", video.FilePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to play video: {FilePath}", video.FilePath);
            ShowErrorMessage("動画再生エラー", $"動画を再生できませんでした:\n{ex.Message}");
        }
    }

    public ICommand MoveVideoCommand => new AsyncRelayCommand<VideoFile>(MoveVideo);
    
    private async Task MoveVideo(VideoFile? video)
    {
        if (video == null) return;

        try
        {
            // フォルダ選択ダイアログ表示（実装簡略化のため、一旦メッセージ表示のみ）
            ShowInfoMessage("機能準備中", "ファイル移動機能は現在実装中です。");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to move video: {FilePath}", video.FilePath);
            ShowErrorMessage("ファイル移動エラー", ex.Message);
        }
    }

    public ICommand DeleteVideoCommand => new AsyncRelayCommand<VideoFile>(DeleteVideo);
    
    private async Task DeleteVideo(VideoFile? video)
    {
        if (video == null) return;

        try
        {
            var result = MessageBox.Show(
                $"以下のファイルを削除しますか？\n\n{video.FileName}",
                "ファイル削除確認",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                // ごみ箱に移動
                Microsoft.VisualBasic.FileIO.FileSystem.DeleteFile(
                    video.FilePath,
                    Microsoft.VisualBasic.FileIO.UIOption.OnlyErrorDialogs,
                    Microsoft.VisualBasic.FileIO.RecycleOption.SendToRecycleBin);

                // データベースからも削除
                await _databaseService.DeleteVideoFileAsync(video.Id);

                // UIから削除
                Videos.Remove(video);
                FilteredVideos.Remove(video);

                StatusText = $"ファイルを削除しました: {video.FileName}";
                _logger.LogInformation("Deleted video file: {FilePath}", video.FilePath);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete video: {FilePath}", video.FilePath);
            ShowErrorMessage("ファイル削除エラー", ex.Message);
        }
    }

    public ICommand RefreshCommand => new AsyncRelayCommand(Refresh);
    
    private async Task Refresh()
    {
        await LoadExistingVideosAsync();
        StatusText = "表示を更新しました";
    }

    public ICommand ClearSearchCommand => new RelayCommand(ClearSearch);
    
    private void ClearSearch()
    {
        SearchQuery = "";
    }

    public ICommand ShowSettingsCommand => new RelayCommand(ShowSettings);
    
    private void ShowSettings()
    {
        try
        {
            var settingsWindow = new Views.SettingsWindow
            {
                Owner = Application.Current.MainWindow
            };
            settingsWindow.ShowDialog();

            // 設定が変更された可能性があるので、設定を再読み込み
            _ = Task.Run(async () => 
            {
                Settings = await _configurationService.GetSettingsAsync();
                OnPropertyChanged(nameof(Settings));
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to open settings window");
            ShowErrorMessage("エラー", "設定画面を開けませんでした。");
        }
    }

    public ICommand ShowAboutCommand => new RelayCommand(ShowAbout);
    
    private void ShowAbout()
    {
        ShowInfoMessage("MovieMonitor について", 
            "MovieMonitor v1.0.0\n\n" +
            "動画ファイル管理アプリケーション\n" +
            "Windows PC内の動画ファイルを効率的に管理できます。");
    }

    public ICommand ExitCommand => new RelayCommand(Exit);
    
    private void Exit()
    {
        Application.Current.Shutdown();
    }

    private async Task<List<string>> GetScanDirectoriesAsync()
    {
        // 設定からスキャン対象ディレクトリを取得
        if (Settings.ScanDirectories.Any())
        {
            return Settings.ScanDirectories.Where(Directory.Exists).ToList();
        }

        // デフォルトのスキャン対象（動画フォルダやダウンロードフォルダ）
        var defaultDirectories = new List<string>();

        try
        {
            var videosFolder = Environment.GetFolderPath(Environment.SpecialFolder.MyVideos);
            if (Directory.Exists(videosFolder))
            {
                defaultDirectories.Add(videosFolder);
            }

            var downloadsFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
            if (Directory.Exists(downloadsFolder))
            {
                defaultDirectories.Add(downloadsFolder);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get default scan directories");
        }

        return defaultDirectories;
    }

    public void OnWindowClosing()
    {
        try
        {
            // スキャンをキャンセル
            _scanCancellationTokenSource?.Cancel();

            // 設定保存
            Settings.WindowMaximized = WindowState == WindowState.Maximized;
            _ = _configurationService.SaveSettingsAsync(Settings);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during window closing");
        }
    }

    private static void ShowInfoMessage(string title, string message)
    {
        MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private static void ShowWarningMessage(string title, string message)
    {
        MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Warning);
    }

    private static void ShowErrorMessage(string title, string message)
    {
        MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Error);
    }
}