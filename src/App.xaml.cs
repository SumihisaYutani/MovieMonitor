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
    
    public IServiceProvider ServiceProvider => _host?.Services ?? throw new InvalidOperationException("Host is not initialized");

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

            // ログ設定
            ConfigureLogging(paths.LogDirectory);

            Log.Logger.LogInfoWithLocation("Application starting...");

            // ホストとDI設定
            _host = CreateHost();

            // サービス開始
            await _host.StartAsync();

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