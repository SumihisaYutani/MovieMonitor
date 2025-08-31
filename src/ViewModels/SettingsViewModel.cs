using Microsoft.Extensions.Logging;
using MovieMonitor.Models;
using MovieMonitor.Services;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using Microsoft.Win32;
using System.Windows.Interop;

namespace MovieMonitor.ViewModels;

public class SettingsViewModel : INotifyPropertyChanged
{
    private readonly IConfigurationService _configurationService;
    private readonly ILogger<SettingsViewModel> _logger;
    private AppSettings _settings;

    public event PropertyChangedEventHandler? PropertyChanged;

    public SettingsViewModel(IConfigurationService configurationService, ILogger<SettingsViewModel> logger)
    {
        _configurationService = configurationService;
        _logger = logger;
        _settings = new AppSettings();
        ScanDirectories = new ObservableCollection<string>();
        
        _logger.LogInformation("SettingsViewModel created - Constructor called");
        System.Diagnostics.Debug.WriteLine("SettingsViewModel constructor called");
        Console.WriteLine("SettingsViewModel constructor called - CONSOLE OUTPUT");
        LoadSettingsAsync();
    }

    private async void LoadSettingsAsync()
    {
        try
        {
            _settings = await _configurationService.GetSettingsAsync();
            
            // プロパティに値を設定
            ThumbnailSize = _settings.ThumbnailSize;
            FFmpegPath = _settings.FFmpegPath ?? "";
            Theme = _settings.Theme;
            AutoScan = _settings.AutoScan;
            AutoScanInterval = _settings.AutoScanInterval;
            DefaultPlayer = _settings.DefaultPlayer ?? "";

            // スキャンディレクトリを設定
            ScanDirectories.Clear();
            foreach (var dir in _settings.ScanDirectories)
            {
                ScanDirectories.Add(dir);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load settings");
        }
    }

    // スキャンディレクトリ
    public ObservableCollection<string> ScanDirectories { get; }

    // サムネイルサイズ
    private int _thumbnailSize = 320;
    public int ThumbnailSize
    {
        get => _thumbnailSize;
        set
        {
            _thumbnailSize = value;
            OnPropertyChanged();
        }
    }

    // FFmpegパス
    private string _ffmpegPath = "";
    public string FFmpegPath
    {
        get => _ffmpegPath;
        set
        {
            _ffmpegPath = value;
            OnPropertyChanged();
        }
    }

    // テーマ
    private AppTheme _theme = AppTheme.Light;
    public AppTheme Theme
    {
        get => _theme;
        set
        {
            _theme = value;
            OnPropertyChanged();
        }
    }

    // 自動スキャン
    private bool _autoScan;
    public bool AutoScan
    {
        get => _autoScan;
        set
        {
            _autoScan = value;
            OnPropertyChanged();
        }
    }

    // 自動スキャン間隔
    private int _autoScanInterval = 60;
    public int AutoScanInterval
    {
        get => _autoScanInterval;
        set
        {
            _autoScanInterval = value;
            OnPropertyChanged();
        }
    }

    // デフォルトプレーヤー
    private string _defaultPlayer = "";
    public string DefaultPlayer
    {
        get => _defaultPlayer;
        set
        {
            _defaultPlayer = value;
            OnPropertyChanged();
        }
    }

    // コマンド
    public ICommand AddDirectoryCommand => new RelayCommand(AddDirectory);
    public ICommand RemoveDirectoryCommand => new RelayCommand<string>(RemoveDirectory);
    public ICommand BrowseFFmpegCommand => new RelayCommand(BrowseFFmpeg);
    public ICommand BrowsePlayerCommand => new RelayCommand(BrowsePlayer);
    private RelayCommand? _saveCommand;
    public ICommand SaveCommand => _saveCommand ??= new RelayCommand(() => 
    {
        _logger.LogInformation("SaveCommand executed");
        System.Diagnostics.Debug.WriteLine("SaveCommand executed - DEBUG");
        Console.WriteLine("SaveCommand executed - CONSOLE OUTPUT");
        SaveSettings();
    });
    public ICommand CancelCommand => new RelayCommand(Cancel);

    private void AddDirectory()
    {
        _logger.LogInformation("AddDirectory method called");
        
        try
        {
            _logger.LogInformation("Checking UI thread access");
            _logger.LogInformation("Current thread ID: {ThreadId}", Thread.CurrentThread.ManagedThreadId);
            _logger.LogInformation("Is on UI thread: {IsUIThread}", Application.Current.Dispatcher.CheckAccess());
            
            // WPFのDispatcher.Invokeを使用してUIスレッドで実行
            Application.Current.Dispatcher.Invoke(() =>
            {
                _logger.LogInformation("Inside Dispatcher.Invoke - Thread ID: {ThreadId}", Thread.CurrentThread.ManagedThreadId);
                
                try
                {
                    _logger.LogInformation("Creating FolderBrowserDialog");
                    using (var dialog = new System.Windows.Forms.FolderBrowserDialog())
                    {
                        _logger.LogInformation("Setting dialog properties");
                        dialog.Description = "Scan target folder selection";
                        dialog.ShowNewFolderButton = false;
                        dialog.UseDescriptionForTitle = true;
                        
                        _logger.LogInformation("Getting active window");
                        var activeWindow = Application.Current.Windows.OfType<Window>().FirstOrDefault(w => w.IsActive);
                        var owner = activeWindow ?? Application.Current.MainWindow;
                        
                        _logger.LogInformation("Active window found: {WindowType}", owner?.GetType().Name ?? "null");
                        
                        System.Windows.Forms.IWin32Window? win32Owner = null;
                        
                        if (owner != null)
                        {
                            try
                            {
                                _logger.LogInformation("Getting window handle");
                                var helper = new System.Windows.Interop.WindowInteropHelper(owner);
                                var handle = helper.Handle;
                                _logger.LogInformation("Window handle: {Handle}", handle);
                                
                                if (handle != IntPtr.Zero)
                                {
                                    var nativeWindow = new System.Windows.Forms.NativeWindow();
                                    nativeWindow.AssignHandle(handle);
                                    win32Owner = nativeWindow;
                                    _logger.LogInformation("NativeWindow created successfully");
                                }
                            }
                            catch (Exception handleEx)
                            {
                                _logger.LogError(handleEx, "Failed to get window handle");
                            }
                        }

                        _logger.LogInformation("About to show dialog");
                        System.Windows.Forms.DialogResult result;
                        
                        if (win32Owner != null)
                        {
                            result = dialog.ShowDialog(win32Owner);
                        }
                        else
                        {
                            result = dialog.ShowDialog();
                        }
                        
                        _logger.LogInformation("Dialog result: {Result}", result);
                        
                        if (result == System.Windows.Forms.DialogResult.OK && !string.IsNullOrEmpty(dialog.SelectedPath))
                        {
                            var selectedPath = dialog.SelectedPath;
                            _logger.LogInformation("Selected path: {Path}", selectedPath);
                            
                            if (!ScanDirectories.Contains(selectedPath))
                            {
                                _logger.LogInformation("Adding directory to collection");
                                ScanDirectories.Add(selectedPath);
                                _logger.LogInformation("Added scan directory: {Directory}", selectedPath);
                            }
                            else
                            {
                                _logger.LogWarning("Directory already exists in scan list: {Directory}", selectedPath);
                            }
                        }
                        else
                        {
                            _logger.LogInformation("Dialog cancelled or no path selected");
                        }
                    }
                    
                    _logger.LogInformation("Dialog disposed successfully");
                }
                catch (Exception innerEx)
                {
                    _logger.LogError(innerEx, "Exception inside Dispatcher.Invoke");
                    throw;
                }
            });
            
            _logger.LogInformation("AddDirectory method completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to add directory - outer exception");
            
            try
            {
                Application.Current.Dispatcher.BeginInvoke(() =>
                {
                    MessageBox.Show($"フォルダの選択でエラーが発生しました。\n\nエラー詳細:\n{ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Warning);
                });
            }
            catch (Exception msgEx)
            {
                _logger.LogError(msgEx, "Failed to show error message");
            }
        }
    }

    private void RemoveDirectory(string? directory)
    {
        try
        {
            if (directory != null && ScanDirectories.Contains(directory))
            {
                ScanDirectories.Remove(directory);
                _logger.LogInformation("Removed scan directory: {Directory}", directory);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to remove directory: {Directory}", directory);
        }
    }

    private void BrowseFFmpeg()
    {
        try
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                using (var dialog = new System.Windows.Forms.FolderBrowserDialog())
                {
                    dialog.Description = "Select FFmpeg bin folder";
                    dialog.ShowNewFolderButton = false;
                    dialog.UseDescriptionForTitle = true;
                    dialog.SelectedPath = !string.IsNullOrEmpty(FFmpegPath) ? FFmpegPath : @"C:\ffmpeg\bin";
                    
                    // WPFのメインウィンドウをオーナーとして設定
                    var activeWindow = Application.Current.Windows.OfType<Window>().FirstOrDefault(w => w.IsActive);
                    var owner = activeWindow ?? Application.Current.MainWindow;
                    var win32Window = new System.Windows.Forms.NativeWindow();
                    
                    if (owner != null)
                    {
                        var helper = new System.Windows.Interop.WindowInteropHelper(owner);
                        win32Window.AssignHandle(helper.Handle);
                    }

                    var result = dialog.ShowDialog(win32Window);
                    if (result == System.Windows.Forms.DialogResult.OK && !string.IsNullOrEmpty(dialog.SelectedPath))
                    {
                        FFmpegPath = dialog.SelectedPath;
                        _logger.LogInformation("Selected FFmpeg path: {Path}", dialog.SelectedPath);
                    }
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to browse FFmpeg path");
            Application.Current.Dispatcher.BeginInvoke(() =>
            {
                MessageBox.Show("FFmpegフォルダの選択でエラーが発生しました。", "エラー", MessageBoxButton.OK, MessageBoxImage.Warning);
            });
        }
    }

    private void BrowsePlayer()
    {
        try
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                var dialog = new OpenFileDialog
                {
                    Title = "Select default video player",
                    Filter = "Executable files (*.exe)|*.exe|All files (*.*)|*.*",
                    CheckFileExists = true,
                    CheckPathExists = true
                };

                if (!string.IsNullOrEmpty(DefaultPlayer))
                {
                    dialog.InitialDirectory = Path.GetDirectoryName(DefaultPlayer);
                    dialog.FileName = Path.GetFileName(DefaultPlayer);
                }

                var result = dialog.ShowDialog();
                if (result == true && !string.IsNullOrEmpty(dialog.FileName))
                {
                    DefaultPlayer = dialog.FileName;
                    _logger.LogInformation("Selected default player: {Player}", dialog.FileName);
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to browse default player");
            Application.Current.Dispatcher.BeginInvoke(() =>
            {
                MessageBox.Show("プレーヤーの選択でエラーが発生しました。", "エラー", MessageBoxButton.OK, MessageBoxImage.Warning);
            });
        }
    }

    private async void SaveSettings()
    {
        try
        {
            _logger.LogInformation("SaveSettings called. Current ThumbnailSize: {Size}", ThumbnailSize);
            Console.WriteLine($"[DEBUG] SaveSettings - Current Theme property: {Theme}");
            Console.WriteLine($"[DEBUG] SaveSettings - Current ThumbnailSize property: {ThumbnailSize}");
            
            // 設定オブジェクトに値を設定
            _settings.ScanDirectories = ScanDirectories.ToList();
            _settings.ThumbnailSize = ThumbnailSize;
            _settings.FFmpegPath = FFmpegPath;
            _settings.Theme = Theme;
            _settings.AutoScan = AutoScan;
            _settings.AutoScanInterval = AutoScanInterval;
            _settings.DefaultPlayer = DefaultPlayer;

            Console.WriteLine($"[DEBUG] SaveSettings - _settings.Theme after assignment: {_settings.Theme}");
            Console.WriteLine($"[DEBUG] SaveSettings - _settings.ThumbnailSize after assignment: {_settings.ThumbnailSize}");

            _logger.LogInformation("About to call ConfigurationService.SaveSettingsAsync with ThumbnailSize: {Size}", _settings.ThumbnailSize);
            await _configurationService.SaveSettingsAsync(_settings);
            
            _logger.LogInformation("Settings saved successfully");
            CloseWindow();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save settings");
            MessageBox.Show("設定の保存に失敗しました。", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void Cancel()
    {
        CloseWindow();
    }

    // ウィンドウを閉じる処理（ViewModelからViewを操作するためのイベント）
    public event EventHandler? RequestClose;

    private void CloseWindow()
    {
        RequestClose?.Invoke(this, EventArgs.Empty);
    }

    protected virtual void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

public class RelayCommand : ICommand
{
    private readonly Action _execute;
    private readonly Func<bool>? _canExecute;

    public RelayCommand(Action execute, Func<bool>? canExecute = null)
    {
        _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        _canExecute = canExecute;
    }

    public event EventHandler? CanExecuteChanged
    {
        add => CommandManager.RequerySuggested += value;
        remove => CommandManager.RequerySuggested -= value;
    }

    public bool CanExecute(object? parameter) => _canExecute?.Invoke() ?? true;

    public void Execute(object? parameter) => _execute();
}

public class RelayCommand<T> : ICommand
{
    private readonly Action<T?> _execute;
    private readonly Func<T?, bool>? _canExecute;

    public RelayCommand(Action<T?> execute, Func<T?, bool>? canExecute = null)
    {
        _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        _canExecute = canExecute;
    }

    public event EventHandler? CanExecuteChanged
    {
        add => CommandManager.RequerySuggested += value;
        remove => CommandManager.RequerySuggested -= value;
    }

    public bool CanExecute(object? parameter) => _canExecute?.Invoke((T?)parameter) ?? true;

    public void Execute(object? parameter) => _execute((T?)parameter);
}