# NightShade

モニターの最小輝度でも眩しいときに、画面全体へ半透明の黒いオーバーレイを重ねて暗くする
タスクトレイ常駐アプリ（Windows 10 / 11、WPF + .NET 9）。

## 使い方

| 操作 | 動作 |
| --- | --- |
| トレイアイコンを左クリック | 暗化オーバーレイの ON / OFF |
| トレイアイコンを右クリック | クイックメニュー（ON/OFF ボタン + 暗さスライダーのみ） |
| トレイアイコンを中クリック | アプリを終了 |

- クイックメニューは情報量を最小限にしており、タイトルや設定・終了ボタンは持たない。
  終了はトレイアイコンの中クリックから行う。
- オーバーレイはマウス操作を完全に透過するため、下のアプリはそのまま操作できる。
- 起動時は **常に OFF**。暗さの設定値だけを次回起動時に引き継ぐ。
- 設定は `%APPDATA%\NightShade\settings.json` に保存される（UI 上に設定画面はない）。

## インストール

### インストーラーを使う場合（推奨）

1. [Release ページ](https://github.com/kihi78/NightShade/releases) から `NightShade_Setup_1.0.0.exe` をダウンロード
2. ダブルクリックして実行し、案内に従ってインストール
3. インストール後、スタートメニューまたはデスクトップのショートカットから起動

### zip 版（インストール不要）を使う場合

1. [Release ページ](https://github.com/kihi78/NightShade/releases) から `NightShade_v1.0.0.zip` をダウンロード
2. 任意のフォルダに展開
3. 展開したフォルダ内の `NightShade.exe` を実行

いずれの方法でも、起動するとタスクトレイにアイコンが表示され常駐します。
設定は `%APPDATA%\NightShade\settings.json` に自動保存されます。

### スタートアップに追加したい場合

```
Windows キー + R → shell:startup
```

スタートアップフォルダを開き、`NightShade.exe` へのショートカットを配置してください
（インストーラー版はインストール時に自動で登録するオプションを選べます）。

## ビルドと実行

ソースコードからビルドする場合:

```bash
dotnet build NightShade.sln -c Release
```

```bash
dotnet run --project src/NightShade/NightShade.csproj
```

配布用の自己完結型 exe を生成する場合:

```bash
dotnet publish src/NightShade/NightShade.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o publish
```

## プロジェクト構成

```
NightShade.sln
└─ src/NightShade/
   ├─ App.xaml / App.xaml.cs      … 合成ルート（サービスと ViewModel の配線）
   ├─ app.manifest                … Per-Monitor V2 DPI 対応
   ├─ Interop/                    … Win32 API（P/Invoke はここだけ）
   │   ├─ NativeMethods.cs
   │   ├─ MonitorInfo.cs
   │   └─ ScreenHelper.cs         … 物理ピクセルでのディスプレイ列挙
   ├─ Models/AppSettings.cs       … settings.json の内容
   ├─ Services/
   │   ├─ ISettingsService.cs / JsonSettingsService.cs
   │   ├─ IOverlayController.cs / OverlayController.cs … 全モニタ分のオーバーレイ管理
   │   ├─ IWindowService.cs / WindowService.cs         … ViewModel から画面を開く抽象
   │   └─ TrayIconService.cs      … NotifyIcon（左/右/中クリックを Command に流すだけ）
   ├─ ViewModels/
   │   ├─ ObservableObject.cs / RelayCommand.cs
   │   └─ ShellViewModel.cs       … 状態と操作の中心
   ├─ Views/
   │   ├─ OverlayWindow.xaml(.cs) … 1 モニタ分の暗化ウィンドウ
   │   └─ QuickMenuWindow.xaml(.cs) … ON/OFF ボタン + 暗さスライダーのみの最小メニュー
   └─ Themes/Dark.xaml            … ダークテーマ
```
