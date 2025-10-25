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

    // メモリ最適化: Videosコレクションを削除してFilteredVideosのみ使用
    [ObservableProperty]
    private ObservableCollection<VideoFile> _filteredVideos = new();
    
    // ソート処理の重複実行を防ぐ
    private bool _isApplyingFilters = false;
    private CancellationTokenSource? _filterCancellationTokenSource;

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

    [ObservableProperty]
    private SortCriteria _sortCriteria = SortCriteria.FileName;

    [ObservableProperty]
    private SortDirection _sortDirection = SortDirection.Ascending;

    [ObservableProperty]
    private long? _minFileSize;

    [ObservableProperty]
    private long? _maxFileSize;

    [ObservableProperty]
    private double? _minDuration;

    [ObservableProperty]
    private double? _maxDuration;

    // ランダム再生機能
    private List<VideoFile> _randomPlaylist = new();
    
    [ObservableProperty]
    private int _currentPlayIndex = -1;

    [ObservableProperty]
    private bool _isRandomPlayModeEnabled = false;

    [ObservableProperty]
    private VideoFile? _currentlyPlayingVideo;

    /// <summary>
    /// サムネイルサイズ（設定から取得）
    /// </summary>
    public int ThumbnailSize => Settings.ThumbnailSize;

    partial void OnSettingsChanged(AppSettings value)
    {
        Console.WriteLine($"[DEBUG] OnSettingsChanged called, Theme: {value.Theme}, ThumbnailSize: {value.ThumbnailSize}");
        Console.WriteLine($"[DEBUG] Previous Theme: {Settings.Theme}, New Theme: {value.Theme}");
        OnPropertyChanged(nameof(ThumbnailSize));
        
        // テーマ変更の適用
        if (Application.Current is App app)
        {
            Console.WriteLine("[DEBUG] Application.Current is App - calling ApplyTheme");
            app.ApplyTheme(value.Theme);
        }
        else
        {
            Console.WriteLine("[DEBUG] Application.Current is NOT App - cannot apply theme");
        }
    }

    private CancellationTokenSource? _scanCancellationTokenSource;

    public MainViewModel(
        IConfigurationService configurationService,
        IDatabaseService databaseService,
        IVideoScanService videoScanService,
        ILogger<MainViewModel> logger)
    {
        // 一番最初にログを出力してコンストラクタ呼び出しを証明
        try
        {
            System.Diagnostics.Debug.WriteLine("MainViewModel constructor: ENTERING CONSTRUCTOR");
            Console.WriteLine("MainViewModel constructor: ENTERING CONSTRUCTOR - CONSOLE OUTPUT");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine("Exception in initial logging: " + ex.Message);
        }
        
        System.Diagnostics.Debug.WriteLine("MainViewModel constructor: START");
        _configurationService = configurationService;
        System.Diagnostics.Debug.WriteLine("MainViewModel constructor: configurationService assigned");
        
        _databaseService = databaseService;
        System.Diagnostics.Debug.WriteLine("MainViewModel constructor: databaseService assigned");
        
        _videoScanService = videoScanService;
        System.Diagnostics.Debug.WriteLine("MainViewModel constructor: videoScanService assigned");
        
        _logger = logger;
        System.Diagnostics.Debug.WriteLine("MainViewModel constructor: logger assigned");

        _logger.LogInformation("MainViewModel constructor called - NEW VERSION WITH SETTINGS EVENT HANDLING");
        System.Diagnostics.Debug.WriteLine("MainViewModel constructor: After first LogInformation");

        // イベントハンドラー登録
        _videoScanService.ProgressChanged += OnScanProgressChanged;
        System.Diagnostics.Debug.WriteLine("MainViewModel constructor: ProgressChanged event registered");
        
        _configurationService.SettingsChanged += OnSettingsChanged;
        System.Diagnostics.Debug.WriteLine("MainViewModel constructor: SettingsChanged event registered");
        
        // プロパティ変更時のフィルタリング（SearchQueryは除外）
        PropertyChanged += async (sender, args) => 
        {
            if (args.PropertyName == nameof(SortCriteria) ||
                args.PropertyName == nameof(SortDirection) ||
                args.PropertyName == nameof(MinFileSize) ||
                args.PropertyName == nameof(MaxFileSize) ||
                args.PropertyName == nameof(MinDuration) ||
                args.PropertyName == nameof(MaxDuration))
            {
                // 重複実行を防ぎ、デバウンス処理を追加
                await ApplyFiltersWithDebounceAsync();
            }
        };

        // 初期化
        _ = InitializeAsync();
        System.Diagnostics.Debug.WriteLine("MainViewModel constructor: InitializeAsync called - CONSTRUCTOR COMPLETE");
    }

    private async Task InitializeAsync()
    {
        System.Diagnostics.Debug.WriteLine("InitializeAsync: START");
        try
        {
            System.Diagnostics.Debug.WriteLine("InitializeAsync: Setting IsLoading = true");
            IsLoading = true;
            LoadingMessage = "初期化中...";

            System.Diagnostics.Debug.WriteLine("InitializeAsync: About to initialize database");
            // データベース初期化
            await _databaseService.InitializeAsync();
            System.Diagnostics.Debug.WriteLine("InitializeAsync: Database initialized");

            System.Diagnostics.Debug.WriteLine("InitializeAsync: About to get settings");
            // 設定読み込み
            Settings = await _configurationService.GetSettingsAsync();
            System.Diagnostics.Debug.WriteLine("InitializeAsync: Settings loaded");

            System.Diagnostics.Debug.WriteLine("InitializeAsync: About to load existing videos");
            // 既存データ読み込み
            await LoadExistingVideosAsync();
            System.Diagnostics.Debug.WriteLine("InitializeAsync: Existing videos loaded");

            StatusText = $"動画ファイル {FilteredVideos.Count} 件";
            System.Diagnostics.Debug.WriteLine("InitializeAsync: Status text set, FilteredVideos.Count = {0}", FilteredVideos.Count);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine("InitializeAsync: Exception occurred - {0}", ex.Message);
            _logger.LogError(ex, "Failed to initialize ViewModel");
            StatusText = "初期化エラー";
            ShowErrorMessage("初期化に失敗しました", ex.Message);
        }
        finally
        {
            System.Diagnostics.Debug.WriteLine("InitializeAsync: Setting IsLoading = false");
            IsLoading = false;
            LoadingMessage = "";
            System.Diagnostics.Debug.WriteLine("InitializeAsync: COMPLETE");
        }
    }

    private async Task LoadExistingVideosAsync()
    {
        try
        {
            // 初期ロード時は最新10000件
            await PerformSearchAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load existing videos");
        }
    }

    partial void OnSearchQueryChanged(string value)
    {
        // 検索クエリ変更時は即座に実行せず、手動適用に変更
        // 重い処理を避けるため、ApplySearchCommand実行時のみフィルタ適用
    }

    /// <summary>
    /// フィルタ条件をすべて適用して動画リストを更新（デバウンス付き）
    /// </summary>
    private async Task ApplyFiltersWithDebounceAsync()
    {
        // 進行中の処理をキャンセル
        _filterCancellationTokenSource?.Cancel();
        _filterCancellationTokenSource = new CancellationTokenSource();
        
        var token = _filterCancellationTokenSource.Token;
        
        try
        {
            // 100ms待機してデバウンス効果を得る
            await Task.Delay(100, token);
            
            if (token.IsCancellationRequested) return;
            
            await ApplyFiltersAsync(token);
        }
        catch (OperationCanceledException)
        {
            // キャンセルは正常な処理
        }
    }

    /// <summary>
    /// フィルタ条件をすべて適用して動画リストを更新
    /// </summary>
    private async Task ApplyFiltersAsync(CancellationToken cancellationToken = default)
    {
        if (_isApplyingFilters) return;
        
        try
        {
            _isApplyingFilters = true;
            
            var searchFilter = new SearchFilter
            {
                Query = string.IsNullOrWhiteSpace(SearchQuery) ? null : SearchQuery,
                MinSize = MinFileSize,
                MaxSize = MaxFileSize,
                MinDuration = MinDuration,
                MaxDuration = MaxDuration,
                SortBy = SortCriteria,
                SortDirection = SortDirection,
                Limit = 10000 // 一度に表示する最大件数
            };

            var results = await _databaseService.SearchVideoFilesAsync(searchFilter);
            
            if (cancellationToken.IsCancellationRequested) return;
            
            // UIの更新を非同期で実行
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                // UIの更新を最小限に抑制
                UpdateVideoCollections(results);
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to apply filters");
        }
        finally
        {
            _isApplyingFilters = false;
        }
    }
    
    /// <summary>
    /// 動画コレクションの更新を最適化（メモリ効率を向上）
    /// </summary>
    private void UpdateVideoCollections(List<VideoFile> newResults)
    {
        // 既存データと比較して変更があるかチェック
        if (FilteredVideos.Count == newResults.Count && FilteredVideos.SequenceEqual(newResults))
        {
            return; // 同じデータなので更新不要
        }
        
        // メモリ効率化: ObservableCollectionの通知を一時停止
        FilteredVideos.Clear();
        
        // バッチでデータを追加（UIスレッドでの処理時間を短縮）
        foreach (var video in newResults)
        {
            FilteredVideos.Add(video);
        }
        
        // GCを促進して不要メモリを回収
        if (newResults.Count > 1000)
        {
            GC.Collect(0, GCCollectionMode.Optimized);
        }
    }

    /// <summary>
    /// 互換性のために残す（既存の呼び出し用）
    /// </summary>
    private async Task PerformSearchAsync()
    {
        await ApplyFiltersAsync();
    }

    private void OnScanProgressChanged(object? sender, ScanProgress progress)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            ScanProgress = progress;
            LoadingMessage = $"スキャン中... {progress.CurrentFile}";
        });
    }

    private void OnSettingsChanged(object? sender, AppSettings newSettings)
    {
        _logger.LogInformation("OnSettingsChanged called. New ThumbnailSize: {Size}, Current Thread: {ThreadId}", 
            newSettings.ThumbnailSize, Thread.CurrentThread.ManagedThreadId);
        Console.WriteLine($"[DEBUG] Private OnSettingsChanged called, Theme: {newSettings.Theme}, ThumbnailSize: {newSettings.ThumbnailSize}");
            
        Application.Current.Dispatcher.Invoke(() =>
        {
            var oldScanDirectories = Settings.ScanDirectories.ToList();
            var oldSize = Settings.ThumbnailSize;
            var oldTheme = Settings.Theme;
            
            Settings = newSettings;
            
            _logger.LogInformation("Settings updated in MainViewModel. Old: {OldSize}, New: {NewSize}", 
                oldSize, newSettings.ThumbnailSize);
            Console.WriteLine($"[DEBUG] Settings object updated. Old Theme: {oldTheme}, New Theme: {newSettings.Theme}");
            
            // テーマ変更の適用（強制適用も含む）
            if (oldTheme != newSettings.Theme)
            {
                Console.WriteLine($"[DEBUG] Theme change detected, applying new theme: {newSettings.Theme}");
                if (Application.Current is App app)
                {
                    Console.WriteLine("[DEBUG] Application.Current is App - calling ApplyTheme from private OnSettingsChanged");
                    app.ApplyTheme(newSettings.Theme);
                }
                else
                {
                    Console.WriteLine("[DEBUG] Application.Current is NOT App - cannot apply theme from private OnSettingsChanged");
                }
            }
            else
            {
                Console.WriteLine($"[DEBUG] No theme change detected, but forcing theme application: {oldTheme}");
                // テーマが同じでも、設定保存時には強制的に適用する（UIリフレッシュのため）
                if (Application.Current is App app)
                {
                    Console.WriteLine("[DEBUG] Forcing theme application even though theme hasn't changed");
                    app.ApplyTheme(newSettings.Theme);
                }
            }
            
            // ThumbnailSizeプロパティの変更通知を明示的に送信
            OnPropertyChanged(nameof(ThumbnailSize));
            _logger.LogInformation("ThumbnailSize property change notification sent");
            
            // スキャンフォルダが変更された場合、除外フォルダのクリーンアップを実行
            if (!oldScanDirectories.SequenceEqual(newSettings.ScanDirectories))
            {
                _logger.LogInformation("Scan directories changed, cleaning up excluded directories");
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await _databaseService.CleanupExcludedDirectoriesAsync(newSettings.ScanDirectories);
                        
                        // UIを更新（メインスレッドで実行）
                        await Application.Current.Dispatcher.InvokeAsync(async () =>
                        {
                            await LoadExistingVideosAsync();
                        });
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to cleanup excluded directories after settings change");
                    }
                });
            }
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
            
            // スキャン対象外フォルダのクリーンアップ
            await _databaseService.CleanupExcludedDirectoriesAsync(directories);

            // UIを更新（強制的にUIスレッドで実行）
            await Application.Current.Dispatcher.InvokeAsync(async () =>
            {
                await LoadExistingVideosAsync();
                _logger.LogInformation("UI updated after scan completed");
            });

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

    public ICommand MoveVideoCommand => new AsyncRelayCommand<VideoFile>(OpenVideoLocation);
    
    private async Task OpenVideoLocation(VideoFile? video)
    {
        if (video == null) return;

        try
        {
            if (!File.Exists(video.FilePath))
            {
                ShowWarningMessage("ファイルが見つかりません", $"ファイルが存在しません：\n{video.FilePath}");
                return;
            }

            // エクスプローラーでファイルを選択状態で開く
            var startInfo = new ProcessStartInfo
            {
                FileName = "explorer.exe",
                Arguments = $"/select,\"{video.FilePath}\"",
                UseShellExecute = false
            };

            Process.Start(startInfo);
            
            _logger.LogDebug("Opened file location in explorer: {FilePath}", video.FilePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to open video location: {FilePath}", video.FilePath);
            ShowErrorMessage("フォルダを開くエラー", $"動画ファイルの場所を開けませんでした：\n{ex.Message}");
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

    /// <summary>
    /// 検索適用コマンド
    /// </summary>
    public ICommand ApplySearchCommand => new AsyncRelayCommand(ApplySearchAsync);
    
    /// <summary>
    /// 検索を適用
    /// </summary>
    private async Task ApplySearchAsync()
    {
        await ApplyFiltersAsync();
    }
    
    private void ClearSearch()
    {
        SearchQuery = "";
        // 検索クリア時は即座にフィルタ適用
        _ = ApplyFiltersWithDebounceAsync();
    }

    public ICommand ClearFiltersCommand => new RelayCommand(ClearFilters);

    private void ClearFilters()
    {
        SearchQuery = "";
        MinFileSize = null;
        MaxFileSize = null;
        MinDuration = null;
        MaxDuration = null;
        SortCriteria = SortCriteria.ScanDate;
        SortDirection = SortDirection.Descending;
    }

    public ICommand ToggleSortDirectionCommand => new RelayCommand(ToggleSortDirection);

    private void ToggleSortDirection()
    {
        SortDirection = SortDirection == SortDirection.Ascending ? SortDirection.Descending : SortDirection.Ascending;
    }

    public ICommand ShowSettingsCommand => new RelayCommand(ShowSettings);
    
    private void ShowSettings()
    {
        try
        {
            _logger.LogInformation("ShowSettings called - opening settings window");
            var settingsWindow = new Views.SettingsWindow
            {
                Owner = Application.Current.MainWindow
            };
            _logger.LogInformation("Settings window created, showing dialog");
            settingsWindow.ShowDialog();
            _logger.LogInformation("Settings window closed");
            
            // 設定変更はイベント通知で自動的に更新される
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

    // ランダム再生コマンド
    public ICommand CreateRandomPlaylistCommand => new RelayCommand(CreateRandomPlaylist);
    public ICommand NextVideoCommand => new AsyncRelayCommand(PlayNextVideo);
    public ICommand PreviousVideoCommand => new AsyncRelayCommand(PlayPreviousVideo);
    public ICommand StopRandomPlayCommand => new RelayCommand(StopRandomPlay);

    private void CreateRandomPlaylist()
    {
        if (!FilteredVideos.Any())
        {
            ShowInfoMessage("ランダムリスト作成", "再生する動画がありません。");
            return;
        }

        // 現在のFilteredVideosからランダムプレイリストを作成
        var random = new Random();
        _randomPlaylist = FilteredVideos.OrderBy(x => random.Next()).ToList();
        CurrentPlayIndex = -1;
        IsRandomPlayModeEnabled = true;
        CurrentlyPlayingVideo = null;

        StatusText = $"ランダムプレイリストを作成しました（{_randomPlaylist.Count}件）";
        _logger.LogInformation("Random playlist created with {Count} videos", _randomPlaylist.Count);
    }

    private async Task PlayNextVideo()
    {
        if (!IsRandomPlayModeEnabled || !_randomPlaylist.Any())
        {
            ShowInfoMessage("次の動画", "ランダムプレイリストが作成されていません。");
            return;
        }

        CurrentPlayIndex++;
        if (CurrentPlayIndex >= _randomPlaylist.Count)
        {
            // プレイリスト終了
            ShowInfoMessage("再生完了", "すべての動画の再生が完了しました。");
            StopRandomPlay();
            return;
        }

        var videoToPlay = _randomPlaylist[CurrentPlayIndex];
        CurrentlyPlayingVideo = videoToPlay;
        await PlayVideo(videoToPlay);
        
        StatusText = $"再生中: {videoToPlay.FileName} ({CurrentPlayIndex + 1}/{_randomPlaylist.Count})";
    }

    private async Task PlayPreviousVideo()
    {
        if (!IsRandomPlayModeEnabled || !_randomPlaylist.Any())
        {
            ShowInfoMessage("前の動画", "ランダムプレイリストが作成されていません。");
            return;
        }

        if (CurrentPlayIndex <= 0)
        {
            ShowInfoMessage("前の動画", "最初の動画です。");
            return;
        }

        CurrentPlayIndex--;
        var videoToPlay = _randomPlaylist[CurrentPlayIndex];
        CurrentlyPlayingVideo = videoToPlay;
        await PlayVideo(videoToPlay);
        
        StatusText = $"再生中: {videoToPlay.FileName} ({CurrentPlayIndex + 1}/{_randomPlaylist.Count})";
    }

    private void StopRandomPlay()
    {
        IsRandomPlayModeEnabled = false;
        CurrentlyPlayingVideo = null;
        CurrentPlayIndex = -1;
        _randomPlaylist.Clear();
        StatusText = "ランダム再生を停止しました";
        _logger.LogInformation("Random play stopped");
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
            
            // フィルタ処理をキャンセル
            _filterCancellationTokenSource?.Cancel();

            // 設定保存
            Settings.WindowMaximized = WindowState == WindowState.Maximized;
            _ = _configurationService.SaveSettingsAsync(Settings);
            
            // リソースのクリーンアップ
            _scanCancellationTokenSource?.Dispose();
            _filterCancellationTokenSource?.Dispose();
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