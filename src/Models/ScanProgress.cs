namespace MovieMonitor.Models;

/// <summary>
/// スキャン進行状況を表すモデル
/// </summary>
public class ScanProgress
{
    /// <summary>
    /// 総ファイル数
    /// </summary>
    public int TotalFiles { get; set; }

    /// <summary>
    /// 処理済みファイル数
    /// </summary>
    public int ProcessedFiles { get; set; }

    /// <summary>
    /// 現在処理中のファイル名
    /// </summary>
    public string CurrentFile { get; set; } = string.Empty;

    /// <summary>
    /// 進行率（0-100）
    /// </summary>
    public double Percentage => TotalFiles > 0 ? (double)ProcessedFiles / TotalFiles * 100 : 0;

    /// <summary>
    /// スキャン完了かどうか
    /// </summary>
    public bool IsCompleted => ProcessedFiles >= TotalFiles && TotalFiles > 0;

    /// <summary>
    /// 進行状況の文字列表現
    /// </summary>
    public string ProgressText => $"{ProcessedFiles} / {TotalFiles} ({Percentage:F1}%)";
}

/// <summary>
/// スキャン結果を表すモデル
/// </summary>
public class ScanResult
{
    /// <summary>
    /// スキャンされた動画ファイル一覧
    /// </summary>
    public List<VideoFile> VideoFiles { get; set; } = new();

    /// <summary>
    /// スキャン開始時刻
    /// </summary>
    public DateTime StartTime { get; set; }

    /// <summary>
    /// スキャン終了時刻
    /// </summary>
    public DateTime EndTime { get; set; }

    /// <summary>
    /// 発見されたファイル数
    /// </summary>
    public int FilesFound { get; set; }

    /// <summary>
    /// 正常に処理されたファイル数
    /// </summary>
    public int FilesProcessed { get; set; }

    /// <summary>
    /// 処理に失敗したファイル数
    /// </summary>
    public int FilesFailed { get; set; }

    /// <summary>
    /// エラーメッセージ一覧
    /// </summary>
    public List<string> Errors { get; set; } = new();

    /// <summary>
    /// スキャン時間
    /// </summary>
    public TimeSpan Duration => EndTime - StartTime;

    /// <summary>
    /// 成功率（0-100）
    /// </summary>
    public double SuccessRate => FilesFound > 0 ? (double)FilesProcessed / FilesFound * 100 : 100;
}