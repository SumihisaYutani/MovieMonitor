using System.Diagnostics;
using System.Text;
using MovieMonitor.Models;

namespace MovieMonitor.Services;

/// <summary>
/// コンソール出力をファイルに記録するサービス
/// </summary>
public class ConsoleLogService : IConsoleLogService
{
    private readonly string _consoleLogPath;
    private readonly StreamWriter _logWriter;
    private readonly TextWriter _originalConsoleOut;
    private readonly TextWriter _originalConsoleError;
    private bool _disposed = false;

    public ConsoleLogService(DirectoryPaths paths)
    {
        _consoleLogPath = Path.Combine(paths.LogDirectory, $"console-{DateTime.Now:yyyyMMdd-HHmmss}.log");
        
        // オリジナルのConsole出力を保存
        _originalConsoleOut = Console.Out;
        _originalConsoleError = Console.Error;
        
        // ログファイルのStreamWriterを作成
        _logWriter = new StreamWriter(_consoleLogPath, append: false, Encoding.UTF8)
        {
            AutoFlush = true
        };
        
        // Console出力をリダイレクト
        var combinedWriter = new CombinedTextWriter(_originalConsoleOut, _logWriter);
        var combinedErrorWriter = new CombinedTextWriter(_originalConsoleError, _logWriter);
        
        Console.SetOut(combinedWriter);
        Console.SetError(combinedErrorWriter);
        
        // 開始ログ
        Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] Console logging started - MovieMonitor");
        Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] Log file: {_consoleLogPath}");
        Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] Command line: {Environment.CommandLine}");
        Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] Working directory: {Environment.CurrentDirectory}");
        Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] Process ID: {Environment.ProcessId}");
        Console.WriteLine("".PadRight(80, '='));
    }
    
    /// <summary>
    /// コンソールログファイルのパスを取得
    /// </summary>
    public string ConsoleLogPath => _consoleLogPath;
    
    /// <summary>
    /// ターミナルコマンド実行結果をログに記録
    /// </summary>
    public void LogCommandExecution(string command, string? workingDirectory = null, int? exitCode = null, string? output = null, string? error = null)
    {
        Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] COMMAND EXECUTION:");
        Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}]   Command: {command}");
        if (!string.IsNullOrEmpty(workingDirectory))
            Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}]   WorkingDir: {workingDirectory}");
        
        if (exitCode.HasValue)
            Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}]   ExitCode: {exitCode.Value}");
            
        if (!string.IsNullOrEmpty(output))
        {
            Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}]   STDOUT:");
            foreach (var line in output.Split('\n', StringSplitOptions.RemoveEmptyEntries))
            {
                Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}]     {line}");
            }
        }
        
        if (!string.IsNullOrEmpty(error))
        {
            Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}]   STDERR:");
            foreach (var line in error.Split('\n', StringSplitOptions.RemoveEmptyEntries))
            {
                Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}]     {line}");
            }
        }
        
        Console.WriteLine("".PadRight(80, '-'));
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed && disposing)
        {
            try
            {
                // 終了ログ
                Console.WriteLine("".PadRight(80, '='));
                Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] Console logging ended - MovieMonitor");
                
                // Console出力を元に戻す
                Console.SetOut(_originalConsoleOut);
                Console.SetError(_originalConsoleError);
                
                // ログファイルを閉じる
                _logWriter?.Dispose();
            }
            catch (Exception ex)
            {
                // エラーが発生してもアプリケーション終了を妨げない
                Debug.WriteLine($"Error disposing ConsoleLogService: {ex.Message}");
            }
        }
        _disposed = true;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}

/// <summary>
/// 複数のTextWriterに同時に出力するクラス
/// </summary>
internal class CombinedTextWriter : TextWriter
{
    private readonly TextWriter[] _writers;

    public CombinedTextWriter(params TextWriter[] writers)
    {
        _writers = writers ?? throw new ArgumentNullException(nameof(writers));
    }

    public override Encoding Encoding => _writers[0].Encoding;

    public override void Write(char value)
    {
        foreach (var writer in _writers)
        {
            try
            {
                writer.Write(value);
            }
            catch
            {
                // 書き込みエラーが発生してもアプリケーション実行を継続
            }
        }
    }

    public override void Write(string? value)
    {
        foreach (var writer in _writers)
        {
            try
            {
                writer.Write(value);
            }
            catch
            {
                // 書き込みエラーが発生してもアプリケーション実行を継続
            }
        }
    }

    public override void WriteLine(string? value)
    {
        foreach (var writer in _writers)
        {
            try
            {
                writer.WriteLine(value);
            }
            catch
            {
                // 書き込みエラーが発生してもアプリケーション実行を継続
            }
        }
    }

    public override void Flush()
    {
        foreach (var writer in _writers)
        {
            try
            {
                writer.Flush();
            }
            catch
            {
                // フラッシュエラーが発生してもアプリケーション実行を継続
            }
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            foreach (var writer in _writers)
            {
                try
                {
                    writer?.Dispose();
                }
                catch
                {
                    // 破棄エラーが発生してもアプリケーション終了を妨げない
                }
            }
        }
        base.Dispose(disposing);
    }
}