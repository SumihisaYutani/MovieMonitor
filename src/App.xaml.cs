using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MovieMonitor.Extensions;
using MovieMonitor.Models;
using MovieMonitor.Services;
using MovieMonitor.ViewModels;
using Serilog;
using System.Diagnostics;
using System.Windows;

namespace MovieMonitor;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    private IHost? _host;
    private ConsoleLogService? _consoleLogService;
    
    public IServiceProvider ServiceProvider => _host?.Services ?? throw new InvalidOperationException("Host is not initialized");

    /// <summary>
    /// テーマを変更します
    /// </summary>
    /// <param name="theme">適用するテーマ</param>
    public void ApplyTheme(AppTheme theme)
    {
        try
        {
            Console.WriteLine($"[DEBUG] ApplyTheme called with theme: {theme}");
            Log.Logger?.LogInfoWithLocation($"ApplyTheme called with theme: {theme}");
            
            var resourceDict = new ResourceDictionary();
            
            switch (theme)
            {
                case AppTheme.Light:
                    resourceDict.Source = new Uri("pack://application:,,,/Resources/Styles.xaml");
                    Console.WriteLine("[DEBUG] Loading Light theme: pack://application:,,,/Resources/Styles.xaml");
                    break;
                case AppTheme.Dark:
                    resourceDict.Source = new Uri("pack://application:,,,/Resources/DarkTheme.xaml");
                    Console.WriteLine("[DEBUG] Loading Dark theme: pack://application:,,,/Resources/DarkTheme.xaml");
                    break;
                default:
                    resourceDict.Source = new Uri("pack://application:,,,/Resources/Styles.xaml");
                    Console.WriteLine("[DEBUG] Loading default Light theme: pack://application:,,,/Resources/Styles.xaml");
                    break;
            }

            Console.WriteLine($"[DEBUG] Current MergedDictionaries count before clear: {Resources.MergedDictionaries.Count}");
            
            // 既存のテーマリソースをクリア
            Resources.MergedDictionaries.Clear();
            
            Console.WriteLine($"[DEBUG] MergedDictionaries cleared, count: {Resources.MergedDictionaries.Count}");
            
            // 新しいテーマを適用
            Resources.MergedDictionaries.Add(resourceDict);
            
            Console.WriteLine($"[DEBUG] New theme added, MergedDictionaries count: {Resources.MergedDictionaries.Count}");
            Console.WriteLine($"[DEBUG] Resource dictionary source: {resourceDict.Source}");
            
            Log.Logger?.LogInfoWithLocation($"Theme applied successfully: {theme}");
            
            // リソース内容を確認
            if (resourceDict.Contains("BackgroundBrush"))
            {
                var bgBrush = resourceDict["BackgroundBrush"];
                Console.WriteLine($"[DEBUG] BackgroundBrush found: {bgBrush}");
            }
            else
            {
                Console.WriteLine("[DEBUG] BackgroundBrush not found in resource dictionary");
            }
            
            // 現在のアプリケーションリソースからもBackgroundBrushを確認
            if (Resources.Contains("BackgroundBrush"))
            {
                var currentBgBrush = Resources["BackgroundBrush"];
                Console.WriteLine($"[DEBUG] Application.Resources BackgroundBrush after update: {currentBgBrush}");
            }
            else
            {
                Console.WriteLine("[DEBUG] BackgroundBrush not found in Application.Resources after update");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[DEBUG] Exception in ApplyTheme: {ex.Message}");
            Console.WriteLine($"[DEBUG] Exception stack trace: {ex.StackTrace}");
            Log.Logger?.LogErrorWithLocation(ex, $"Failed to apply theme: {theme}");
        }
    }

    protected override async void OnStartup(StartupEventArgs e)
    {
        try
        {
            // 重複起動チェック
            if (IsAnotherInstanceRunning())
            {
                MessageBox.Show("MovieMonitorは既に起動しています。", "重複起動", MessageBoxButton.OK, MessageBoxImage.Information);
                Shutdown();
                return;
            }

            // 未処理例外のハンドリングを設定
            AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
            DispatcherUnhandledException += OnDispatcherUnhandledException;

            // ディレクトリ作成
            var paths = new DirectoryPaths();
            paths.EnsureDirectoriesExist();

            // コンソールログサービス開始（標準出力・標準エラー出力をファイルに記録）
            _consoleLogService = new ConsoleLogService(paths);
            
            // コンソールログテスト
            Console.WriteLine("MovieMonitor アプリケーション開始");
            Console.WriteLine($"開始時刻: {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}");
            Console.WriteLine($"作業ディレクトリ: {Environment.CurrentDirectory}");
            
            // ログ設定
            ConfigureLogging(paths.LogDirectory);

            Log.Logger.LogInfoWithLocation("Application starting...");

            // ホストとDI設定
            _host = CreateHost();

            // サービス開始
            await _host.StartAsync();

            // 初期テーマの適用
            Console.WriteLine("[DEBUG] Loading initial theme settings...");
            var themeConfigService = _host.Services.GetRequiredService<IConfigurationService>();
            var settings = await themeConfigService.GetSettingsAsync();
            Console.WriteLine($"[DEBUG] Initial theme setting loaded: {settings.Theme}");
            ApplyTheme(settings.Theme);

            // メインウィンドウ表示
            Log.Logger.LogInfoWithLocation("About to create MainWindow");
            try
            {
                // 依存関係を個別にテスト
                Log.Logger.LogInfoWithLocation("Testing MainViewModel dependencies...");
                var configService = _host.Services.GetRequiredService<IConfigurationService>();
                Log.Logger.LogInfoWithLocation("IConfigurationService resolved successfully");
                
                var dbService = _host.Services.GetRequiredService<IDatabaseService>();
                Log.Logger.LogInfoWithLocation("IDatabaseService resolved successfully");
                
                var videoScanService = _host.Services.GetRequiredService<IVideoScanService>();
                Log.Logger.LogInfoWithLocation("IVideoScanService resolved successfully");
                
                var logger = _host.Services.GetRequiredService<Microsoft.Extensions.Logging.ILogger<MainViewModel>>();
                Log.Logger.LogInfoWithLocation("ILogger<MainViewModel> resolved successfully");
                
                // MainViewModelを直接作成してテスト
                Log.Logger.LogInfoWithLocation("Creating MainViewModel directly...");
                var mainViewModel = _host.Services.GetRequiredService<MainViewModel>();
                Log.Logger.LogInfoWithLocation("MainViewModel created successfully");
                Log.Logger.LogInfoWithLocation($"MainViewModel type: {mainViewModel.GetType().FullName}, HashCode: {mainViewModel.GetHashCode()}");
                
                // MainViewModelが実際に期待される型であることを確認
                if (mainViewModel is MainViewModel actualMainViewModel)
                {
                    Log.Logger.LogInfoWithLocation("MainViewModel is correct type, checking if constructor was called properly");
                    // 簡単なプロパティアクセスでインスタンスが有効か確認
                    var isLoadingValue = actualMainViewModel.IsLoading;
                    Log.Logger.LogInfoWithLocation($"MainViewModel.IsLoading: {isLoadingValue}");
                }
                else
                {
                    Log.Logger.LogInfoWithLocation("MainViewModel is not the expected type!");
                }
                
                // MainViewModelを引数として渡してMainWindowを作成
                Log.Logger.LogInfoWithLocation("Creating MainWindow with MainViewModel...");
                var mainWindow = new MainWindow(mainViewModel);
                Log.Logger.LogInfoWithLocation("MainWindow created successfully with MainViewModel, showing window");
                mainWindow.Show();
                Log.Logger.LogInfoWithLocation("MainWindow.Show() completed");
            }
            catch (Exception ex)
            {
                Log.Logger.LogErrorWithLocation(ex, "Failed to create dependencies or MainWindow: {Message}", ex.Message);
                throw;
            }

            Log.Logger.LogInfoWithLocation("Application startup completed successfully");

            base.OnStartup(e);
        }
        catch (Exception ex)
        {
            Log.Logger.LogFatalWithLocation(ex, "Failed to start application");
            MessageBox.Show($"アプリケーションの起動に失敗しました。\n\nエラー内容:\n{ex.Message}\n\n詳細:\n{ex}", 
                "MovieMonitor - 起動エラー", MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown(1);
        }
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        try
        {
            if (_host != null)
            {
                await _host.StopAsync(TimeSpan.FromSeconds(5));
                _host.Dispose();
            }

            Log.Logger.LogInfoWithLocation("Application shutdown complete");
        }
        catch (Exception ex)
        {
            Log.Logger.LogErrorWithLocation(ex, "Error during application shutdown");
        }
        finally
        {
            // コンソールログサービスを終了
            _consoleLogService?.Dispose();
            
            Log.CloseAndFlush();
        }

        base.OnExit(e);
    }

    private static IHost CreateHost()
    {
        return Host.CreateDefaultBuilder()
            .ConfigureServices((context, services) =>
            {
                // Models
                services.AddSingleton<DirectoryPaths>();

                // Services
                services.AddSingleton<IConfigurationService, ConfigurationService>();
                services.AddSingleton<IDatabaseService, DatabaseService>();
                services.AddSingleton<IVideoScanService, VideoScanService>();
                services.AddSingleton<IThumbnailService, ThumbnailService>();
                services.AddSingleton<IConsoleLogService>(provider => 
                    ((App)Current)._consoleLogService!);

                // ViewModels  
                services.AddSingleton<MainViewModel>();
                services.AddTransient<SettingsViewModel>();

                // Views
                services.AddTransient<MainWindow>();
            })
            .Build();
    }

    private bool IsAnotherInstanceRunning()
    {
        try
        {
            var currentProcess = Process.GetCurrentProcess();
            var processes = Process.GetProcessesByName(currentProcess.ProcessName);
            
            // 現在のプロセス以外に同名のプロセスがあるかチェック
            return processes.Length > 1;
        }
        catch
        {
            // プロセス情報取得に失敗した場合は、重複チェックをスキップ
            return false;
        }
    }

    private void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        var exception = (Exception)e.ExceptionObject;
        Log.Logger.LogFatalWithLocation(exception, $"Unhandled exception occurred. Terminating: {e.IsTerminating}");
        
        MessageBox.Show($"予期しないエラーが発生しました。\n\nエラー内容:\n{exception.Message}\n\n詳細:\n{exception}", 
            "MovieMonitor - 予期しないエラー", MessageBoxButton.OK, MessageBoxImage.Error);
    }

    private void OnDispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
    {
        Log.Logger.LogErrorWithLocation(e.Exception, "Unhandled dispatcher exception occurred");
        
        MessageBox.Show($"UIスレッドでエラーが発生しました。\n\nエラー内容:\n{e.Exception.Message}\n\n詳細:\n{e.Exception}", 
            "MovieMonitor - UIエラー", MessageBoxButton.OK, MessageBoxImage.Warning);
        
        // 継続実行を試みる
        e.Handled = true;
    }

    private static void ConfigureLogging(string logDirectory)
    {
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .Enrich.WithEnvironmentName()
            .Enrich.WithThreadId()
            .WriteTo.File(
                path: Path.Combine(logDirectory, "app-.log"),
                rollingInterval: Serilog.RollingInterval.Day,
                retainedFileCountLimit: 7,
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] [Thread:{ThreadId}] {Message:lj}{NewLine}{Exception}")
            .CreateLogger();
    }
}