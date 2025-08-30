# MovieMonitor 開発ガイドライン

## プロジェクト概要

WindowsPC内の動画ファイルを検索・整理するスタンドアロンexeアプリケーション。

### 技術スタック
- **UI**: WPF + .NET 8
- **データベース**: LiteDB
- **動画処理**: FFMpegCore  
- **配布形式**: Single File Executable (.exe)

## コーディングルール

### 1. 命名規則

#### C#クラス・メソッド
```csharp
// パスカルケース
public class VideoScanService { }
public async Task ScanDirectoryAsync() { }
public string FilePath { get; set; }
```

#### 変数・フィールド
```csharp
// キャメルケース
private string filePath;
var videoFiles = new List<VideoFile>();

// プライベートフィールドにはアンダースコア
private readonly ILogger _logger;
private readonly DatabaseService _database;
```

#### 定数
```csharp
// 全て大文字・アンダースコア区切り
public const int MAX_FILE_SIZE = 10737418240; // 10GB
public const string DEFAULT_THUMBNAIL_SIZE = "320x240";
```

### 2. コーディング規約

#### 非同期処理
```csharp
// 必ずAsyncサフィックス + Task戻り値
public async Task<List<VideoFile>> ScanDirectoryAsync(string path)
{
    // ConfigureAwait(false)を使用
    var files = await GetFilesAsync().ConfigureAwait(false);
    return files;
}
```

#### エラーハンドリング
```csharp
// 具体的な例外をキャッチ
try
{
    await ScanVideoAsync(filePath);
}
catch (FileNotFoundException ex)
{
    _logger.LogError(ex, "Video file not found: {FilePath}", filePath);
    // ユーザーに分かりやすいメッセージ
    throw new VideoProcessingException("ファイルが見つかりません", ex);
}
catch (UnauthorizedAccessException ex)
{
    _logger.LogError(ex, "Access denied: {FilePath}", filePath);
    throw new VideoProcessingException("ファイルにアクセスできません", ex);
}
```

#### LINQ使用方針
```csharp
// 可読性を重視、メソッドチェーンは適度に改行
var filteredVideos = videos
    .Where(v => v.Duration > minDuration)
    .Where(v => v.FileSize < maxFileSize)
    .OrderByDescending(v => v.ScanDate)
    .ToList();
```

#### リソース管理
```csharp
// usingステートメント必須
using var database = new LiteDatabase(connectionString);
using var stream = File.OpenRead(filePath);

// IDisposableの適切な実装
public class VideoScanService : IDisposable
{
    private bool _disposed = false;

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed && disposing)
        {
            // リソース解放
        }
        _disposed = true;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}
```

### 3. XAML規約

#### 命名
```xml
<!-- パスカルケース、機能を表現 -->
<Grid x:Name="VideoListGrid">
    <Button x:Name="ScanButton" Content="スキャン開始" />
    <TextBox x:Name="SearchTextBox" />
</Grid>
```

#### レイアウト
```xml
<!-- 適切なインデント（2スペース） -->
<Grid>
  <Grid.RowDefinitions>
    <RowDefinition Height="Auto" />
    <RowDefinition Height="*" />
  </Grid.RowDefinitions>
  
  <StackPanel Grid.Row="0" Orientation="Horizontal">
    <Button Content="スキャン" Command="{Binding ScanCommand}" />
  </StackPanel>
</Grid>
```

## 実装方針

### 1. アーキテクチャパターン

**MVVM (Model-View-ViewModel)** を採用

```csharp
// Model: データとビジネスロジック
public class VideoFile { }
public class DatabaseService { }

// ViewModel: UIロジックとデータバインディング  
public class MainViewModel : INotifyPropertyChanged { }

// View: XAML UI
// MainWindow.xaml
```

### 2. 依存性注入

```csharp
// Program.cs
var services = new ServiceCollection();
services.AddSingleton<IDatabaseService, DatabaseService>();
services.AddSingleton<IVideoScanService, VideoScanService>();
services.AddTransient<MainViewModel>();

var serviceProvider = services.BuildServiceProvider();
```

### 3. エラーハンドリング戦略

```csharp
// カスタム例外の階層
public abstract class MovieMonitorException : Exception
{
    protected MovieMonitorException(string message) : base(message) { }
    protected MovieMonitorException(string message, Exception innerException) 
        : base(message, innerException) { }
}

public class VideoProcessingException : MovieMonitorException { }
public class DatabaseException : MovieMonitorException { }
```

### 4. パフォーマンス方針

- **UI仮想化**: 大量データ表示にVirtualizingStackPanel使用
- **非同期処理**: UIスレッドブロック防止
- **メモリ管理**: WeakReferenceやDispose適切実装
- **キャッシュ戦略**: サムネイル画像のメモリキャッシュ

## ディレクトリ・ファイル配置

### 1. 実行時ファイル配置

```
MovieMonitor.exe (実行ファイル)
├── data/
│   └── database.db              # LiteDBファイル  
├── thumbnails/                  # サムネイル画像
│   ├── {hash1}.png
│   ├── {hash2}.png  
│   └── ...
├── logs/                        # ログファイル
│   ├── app-2024-01-20.log
│   ├── app-2024-01-21.log
│   └── ...
├── temp/                        # 一時ファイル
│   ├── thumb_temp_{guid}.png
│   └── ...
└── config/                      # 設定ファイル
    └── appsettings.json
```

**実際の保存先**:
```csharp
// ベースディレクトリ（実行ファイルの場所）
var baseDirectory = Path.GetDirectoryName(Environment.ProcessPath) 
    ?? AppContext.BaseDirectory;

// 各サブディレクトリ
var databasePath = Path.Combine(baseDirectory, "data", "database.db");
var thumbnailDir = Path.Combine(baseDirectory, "thumbnails");
var logDir = Path.Combine(baseDirectory, "logs");
var tempDir = Path.Combine(baseDirectory, "temp");
var configDir = Path.Combine(baseDirectory, "config");
```

### 2. 開発時ディレクトリ構造

```
MovieMonitor/
├── src/                         # ソースコード
│   ├── MovieMonitor.csproj
│   ├── Program.cs
│   ├── App.xaml
│   ├── ViewModels/
│   ├── Views/
│   ├── Services/
│   ├── Models/
│   └── Resources/
├── tests/                       # テストコード
│   ├── MovieMonitor.Tests.csproj
│   ├── Services/
│   └── ViewModels/
├── docs/                        # ドキュメント
│   ├── 要件定義書.md
│   ├── 基本設計書.md
│   └── ...
├── build/                       # ビルド出力
│   ├── Debug/
│   ├── Release/
│   └── Publish/
├── assets/                      # 静的リソース
│   ├── icons/
│   └── images/
├── tools/                       # 開発ツール
│   └── ffmpeg.exe              # 動画処理用
├── claude.md                    # このファイル
└── README.md
```

### 3. ビルド出力先

```xml
<!-- MovieMonitor.csproj -->
<PropertyGroup>
  <OutputPath>..\..\build\$(Configuration)\</OutputPath>
  <PublishDir>..\..\build\Publish\</PublishDir>
</PropertyGroup>
```

**具体的なパス**:
- **デバッグビルド**: `build/Debug/MovieMonitor.exe`
- **リリースビルド**: `build/Release/MovieMonitor.exe`  
- **単一ファイル発行**: `build/Publish/MovieMonitor.exe`

## ログ・ファイル管理

### 1. ログ設定

```csharp
// Serilogを使用
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.File(
        path: Path.Combine(logDir, "app-.log"),
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 7,
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss} [{Level:u3}] {SourceContext}: {Message}{NewLine}{Exception}")
    .CreateLogger();
```

### 2. ログレベル運用

- **Information**: 正常な処理フロー
- **Warning**: 異常だが継続可能  
- **Error**: エラーが発生したが回復可能
- **Fatal**: アプリケーション停止レベル

### 3. 一時ファイル管理

```csharp
public class TempFileManager : IDisposable
{
    private readonly string _tempDir;
    private readonly List<string> _tempFiles = new();

    public string CreateTempFile(string extension = ".tmp")
    {
        var tempFile = Path.Combine(_tempDir, $"temp_{Guid.NewGuid()}{extension}");
        _tempFiles.Add(tempFile);
        return tempFile;
    }

    public void Dispose()
    {
        // 一時ファイルをクリーンアップ
        foreach (var file in _tempFiles)
        {
            try { File.Delete(file); } catch { /* ignore */ }
        }
    }
}
```

## ビルド・配布

### 1. 開発時ビルドコマンド

```bash
# デバッグビルド
dotnet build -c Debug

# リリースビルド  
dotnet build -c Release
```

### 2. Single File Executable作成

```bash
# Windows x64向けスタンドアロンexe
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true
```

### 3. 配布ファイル

最終的な配布物は1つのexeファイルのみ:
- `MovieMonitor.exe` (約50-100MB)
- インストール不要、.NET Runtime不要
- 初回実行時に実行ファイル直下に必要フォルダを自動作成
- ポータブル仕様：フォルダごと移動・コピー可能

## テスト方針

### 1. 単体テスト
- **xUnit** 使用
- **Moq** でモック作成
- **FluentAssertions** で可読性向上

### 2. UI テスト  
- **FlaUI** でWPF UI自動テスト
- 主要なユーザーシナリオをカバー

### 3. テストカバレッジ
- 目標: 80%以上
- ビジネスロジック: 90%以上

## ドキュメント管理

### 重要な注意事項
**ソースコードを変更した場合は、必ずドキュメントも更新してください。**

### ドキュメント更新対象
1. **README.md** - 機能追加・変更時は実装状況セクションを更新
2. **要件定義書.md** - 要件変更時に更新
3. **基本設計書.md** - アーキテクチャ変更時に更新
4. **詳細設計書.md** - クラス・メソッド追加時に更新
5. **claude.md** - 開発方針変更時に更新

### 更新タイミング
- ✅ 新機能実装完了後
- ✅ 既存機能の大幅修正後  
- ✅ バグ修正で仕様変更があった場合
- ✅ 設定項目追加・変更後
- ✅ ファイル構造変更後

### 更新内容例
```markdown
## 実装履歴

### v1.1.0 (2024-XX-XX)
- ✅ サムネイル形式をPNGに変更
- ✅ オーバーレイボタン表示問題を修正
- ✅ FFmpeg設定の詳細ログ追加

### v1.0.0 (2024-XX-XX)
- ✅ 基本機能実装完了
```

この開発ガイドラインに従って実装を進めてください。