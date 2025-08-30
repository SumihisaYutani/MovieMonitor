# MovieMonitor

Windows PC内の動画ファイルを効率的に検索・整理するスタンドアロンアプリケーション

## 🚀 特徴

- **動画ファイル自動検索**: MP4、AVI、MKV、TS形式に対応
- **サムネイル自動生成**: 動画の中間地点からサムネイル作成（PNG形式）
- **高速検索・データベース最適化**: インデックス付きLiteDBによる高速検索
- **カスタムアイコン**: 映画フィルムモチーフのオリジナルアイコン
- **レスポンシブUI**: サムネイルサイズ可変（100-600px）、タグ形式の情報表示
- **ファイル操作**: 再生、削除機能
- **ポータブル**: 単一exeファイルで動作、インストール不要（164MB）

## 📋 システム要件

- Windows 10/11 (64bit)
- .NET 8.0 Runtime (埋め込み済み)

## 🛠️ 開発環境

### 必要なソフトウェア

- **Visual Studio 2022** または **Visual Studio Code**
- **.NET 8.0 SDK**
- **FFmpeg** (動画処理用)

### 技術スタック

- **フレームワーク**: WPF + .NET 8
- **データベース**: LiteDB (軽量NoSQL) - インデックス最適化済み
- **動画処理**: FFMpegCore (PNG サムネイル生成)
- **ログ**: Serilog (ファイル・行番号付きログ)
- **MVVM**: CommunityToolkit.Mvvm + カスタムRelayCommand
- **UI**: Material Design風カスタムスタイル + レスポンシブレイアウト
- **コンバーター**: カスタム値変換器（サムネイルサイズ連動）

## 📁 プロジェクト構造

```
MovieMonitor/
├── src/                          # ソースコード
│   ├── MovieMonitor.csproj       # プロジェクトファイル
│   ├── Program.cs                # エントリーポイント
│   ├── App.xaml                  # アプリケーション設定
│   ├── MainWindow.xaml           # メインウィンドウ
│   ├── Models/                   # データモデル
│   │   ├── VideoFile.cs          # 動画ファイル情報
│   │   ├── SearchFilter.cs       # 検索フィルター
│   │   ├── ScanProgress.cs       # スキャン進行状況
│   │   └── AppSettings.cs        # アプリケーション設定
│   ├── Services/                 # サービス層
│   │   ├── IConfigurationService.cs  # 設定サービス
│   │   ├── ConfigurationService.cs
│   │   ├── IDatabaseService.cs       # データベースサービス
│   │   ├── DatabaseService.cs
│   │   ├── IVideoScanService.cs      # 動画スキャンサービス
│   │   ├── VideoScanService.cs
│   │   ├── IThumbnailService.cs      # サムネイルサービス
│   │   └── ThumbnailService.cs
│   ├── ViewModels/               # MVVM ViewModels
│   │   ├── MainViewModel.cs      # メインViewModel
│   │   └── SettingsViewModel.cs  # 設定ViewModel
│   ├── Views/                    # ビュー
│   │   └── SettingsWindow.xaml   # 設定ウィンドウ
│   ├── Converters/               # 値コンバーター
│   │   ├── BooleanToVisibilityConverter.cs
│   │   └── ThumbnailSizeConverters.cs  # サムネイルサイズ変換
│   ├── Extensions/               # 拡張メソッド
│   │   └── SerilogExtensions.cs  # ログ拡張
│   └── Resources/                # リソースファイル
│       ├── Styles.xaml           # UIスタイル (Material Design)
│       └── Icons/                # アプリケーションアイコン
│           └── app-icon.ico
├── docs/                         # ドキュメント
│   ├── 要件定義書.md
│   ├── 基本設計書.md
│   ├── 詳細設計書.md
│   ├── ER図.md
│   ├── クラス図.md
│   └── データ仕様書.md
└── claude.md                     # 開発ガイドライン
```

## ⚡ ビルドと実行

### 開発モード

```bash
# プロジェクトをビルド
cd src
dotnet build

# デバッグ実行
dotnet run
```

### リリースビルド

```bash
# リリースビルド
dotnet build -c Release

# Single File Executable作成（アイコン付き）
dotnet publish -c Release --self-contained -r win-x64 -p:PublishSingleFile=true -p:DebugType=None
```

## 🗃️ 実行時ファイル配置

アプリケーション実行時、以下の構造で必要なファイルが実行ファイル直下に作成されます：

```
MovieMonitor.exe
├── data/
│   └── database.db              # 動画ファイル情報DB
├── thumbnails/                  # サムネイル画像
│   ├── {hash1}.png
│   └── {hash2}.png
├── logs/                        # ログファイル
│   ├── app-2024-01-20.log
│   └── app-2024-01-21.log
├── temp/                        # 一時ファイル
└── config/                      # 設定ファイル
    └── appsettings.json
```

## 🎯 主要機能

### 動画ファイルスキャン
- 指定ディレクトリの再帰的検索
- 対応形式: MP4, AVI, MKV, TS
- メタデータ自動抽出 (解像度、再生時間、ファイルサイズ)
- サムネイル自動生成

### 検索・フィルタリング
- ファイル名による部分一致検索
- ファイルサイズ範囲指定
- 再生時間範囲指定
- ファイル形式絞り込み

### ファイル操作
- **再生**: システムデフォルトプレーヤーで開く
- **削除**: ごみ箱に移動
- **移動**: 指定フォルダに移動 (実装予定)

### データ管理
- LiteDB による高速なローカルデータベース
- サムネイルキャッシュ管理
- 削除ファイルの自動クリーンアップ

## 🔧 設定

設定は `config/appsettings.json` で管理されます：

```json
{
  "ScanDirectories": ["C:\\Users\\username\\Videos", "D:\\Movies"],
  "ThumbnailSize": 320,
  "FFmpegPath": "C:\\ffmpeg\\bin",
  "Theme": "Light",
  "AutoScan": false,
  "AutoScanInterval": 60,
  "DefaultPlayer": "",
  "SupportedFormats": ["Mp4", "Avi", "Mkv", "Ts"],
  "WindowWidth": 1200,
  "WindowHeight": 800,
  "WindowLeft": 100,
  "WindowTop": 100,
  "WindowMaximized": false
}
```

## 📊 パフォーマンス

- **スキャン速度**: 約1000ファイル/分 (SSD環境)
- **検索速度**: インデックス使用により数ミリ秒で結果表示
- **メモリ使用量**: 最大512MB (1000件表示時)
- **データベースサイズ**: 動画1000件で約10MB
- **サムネイルサイズ**: PNG形式、1件あたり約50KB
- **実行ファイルサイズ**: 164MB (全依存関係内包)

## ✅ 実装状況

### 完了済み機能
- ✅ **Material Design UI**: モダンなカードベースのインターフェース
- ✅ **レスポンシブレイアウト**: サムネイルサイズ100-600px可変対応
- ✅ **動画スキャン**: 指定ディレクトリの再帰的検索・重複起動防止
- ✅ **サムネイル生成**: FFmpeg使用による自動PNG生成（中間フレーム）
- ✅ **高速データベース**: LiteDB・複合インデックス・クエリ最適化
- ✅ **リアルタイム検索**: インデックス利用による高速検索
- ✅ **再生機能**: システムデフォルト/カスタムプレーヤー対応
- ✅ **設定システム**: GUI設定画面・JSON永続化・リアルタイム反映
- ✅ **ログシステム**: Serilog・ファイル名行番号付きログ
- ✅ **ファイル操作**: ごみ箱削除・安全な操作
- ✅ **カスタムアイコン**: 映画フィルムモチーフ・単一実行ファイル
- ✅ **エラーハンドリング**: 包括的例外処理・ユーザーフレンドリー

### 実装予定機能
- 📋 **ファイル移動**: 指定フォルダへのファイル移動
- 📋 **高度な検索**: ファイルサイズ・再生時間による絞り込み
- 📋 **自動スキャン**: 定期的なディレクトリ監視
- 📋 **ソート機能**: 各項目による並び替え

## 🐛 トラブルシューティング

### よくある問題

1. **スキャンが遅い**
   - HDDの場合は処理時間が長くなります
   - ネットワークドライブはアクセスが遅い場合があります

2. **サムネイルが生成されない**
   - FFmpegが `C:\ffmpeg\bin` にインストールされているか確認
   - 設定ファイル `config/appsettings.json` の `FFmpegPath` を確認
   - 動画ファイルが破損していないか確認
   - ログファイルでFFmpeg関連のエラーを確認

3. **データベースエラー**
   - `data/database.db` を削除して再スキャン
   - アプリケーションの書き込み権限を確認

### ログファイル

詳細なログは `logs/app-{日付}.log` で確認できます。

## 🤝 開発ガイド

開発時のコーディングルールや実装方針は [`claude.md`](claude.md) を参照してください。

## 📄 ライセンス

このプロジェクトはMITライセンスの下で公開されています。

## 👥 貢献

バグ報告や機能要望は GitHub Issues でお知らせください。

---

**MovieMonitor v1.0.0** - Windows 動画ファイル管理アプリケーション