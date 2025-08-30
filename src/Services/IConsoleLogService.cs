namespace MovieMonitor.Services;

/// <summary>
/// コンソール出力をファイルに記録するサービスのインターフェイス
/// </summary>
public interface IConsoleLogService : IDisposable
{
    /// <summary>
    /// コンソールログファイルのパスを取得
    /// </summary>
    string ConsoleLogPath { get; }
    
    /// <summary>
    /// ターミナルコマンド実行結果をログに記録
    /// </summary>
    /// <param name="command">実行されたコマンド</param>
    /// <param name="workingDirectory">作業ディレクトリ</param>
    /// <param name="exitCode">終了コード</param>
    /// <param name="output">標準出力</param>
    /// <param name="error">標準エラー出力</param>
    void LogCommandExecution(string command, string? workingDirectory = null, int? exitCode = null, string? output = null, string? error = null);
}