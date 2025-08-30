using MovieMonitor.Models;

namespace MovieMonitor.Services;

/// <summary>
/// 設定サービスのインターフェース
/// </summary>
public interface IConfigurationService
{
    /// <summary>
    /// 設定変更時のイベント
    /// </summary>
    event EventHandler<AppSettings>? SettingsChanged;

    /// <summary>
    /// アプリケーション設定を取得
    /// </summary>
    Task<AppSettings> GetSettingsAsync();

    /// <summary>
    /// アプリケーション設定を保存
    /// </summary>
    Task SaveSettingsAsync(AppSettings settings);

    /// <summary>
    /// 設定値を取得
    /// </summary>
    Task<T?> GetValueAsync<T>(string key);

    /// <summary>
    /// 設定値を保存
    /// </summary>
    Task SetValueAsync<T>(string key, T value);

    /// <summary>
    /// 設定ファイルが存在するかチェック
    /// </summary>
    bool ConfigurationExists();
}