# NightShade

モニターの最小輝度でも眩しいときに、画面全体へ半透明の黒いオーバーレイを重ねて暗くする
タスクトレイ常駐アプリ（Windows 10 / 11、WPF + .NET 9）。

## 使い方

| 操作 | 動作 |
| --- | --- |
| トレイアイコンを左クリック | 暗化オーバーレイの ON / OFF |
| トレイアイコンを右クリック | クイックメニュー（ON/OFF ボタン + 暗さスライダーのみ） |
| トレイアイコンを中クリック | アプリを終了 |
| Ctrl + Shift + ↑ | 1 段階暗くする（OFF なら自動で ON） |
| Ctrl + Shift + ↓ | 1 段階明るくする（0% まで下げると OFF） |
| Ctrl + Shift + N | ON / OFF の切り替え |

- クイックメニューは情報量を最小限にしており、タイトルや設定・終了ボタンは持たない。
  終了はトレイアイコンの中クリックから行う。
- オーバーレイはマウス操作を完全に透過するため、下のアプリはそのまま操作できる。
- 起動時は **常に OFF**。暗さの設定値だけを次回起動時に引き継ぐ。
- 設定は `%APPDATA%\NightShade\settings.json` に保存される（UI 上に設定画面はない）。

## インストール

### Windows 10/11 での実行

1. [Release ページ](https://github.com/sidoclo/NightShade/releases) から `NightShade.exe` をダウンロード
2. 任意のフォルダに配置
3. `NightShade.exe` をダブルクリックして起動
4. タスクトレイにアイコンが表示され、常駐開始

設定は `%APPDATA%\NightShade\settings.json` に自動保存されます。

### スタートアップに追加したい場合

```
Windows キー + R → shell:startup
```

スタートアップフォルダを開き、`NightShade.exe` へのショートカットを配置してください。

## ビルドと実行

ソースコードからビルドする場合:

```bash
dotnet build NightShade.sln -c Release
```

```bash
dotnet run --project src/NightShade/NightShade.csproj
```

ビルド成果物は `src/NightShade/bin/Release/net9.0-windows/` に出力されます。

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
   │   ├─ TrayIconService.cs      … NotifyIcon（左/右/中クリックを Command に流すだけ）
   │   └─ HotKeyService.cs        … グローバルショートカット
   ├─ ViewModels/
   │   ├─ ObservableObject.cs / RelayCommand.cs
   │   └─ ShellViewModel.cs       … 状態と操作の中心
   ├─ Views/
   │   ├─ OverlayWindow.xaml(.cs) … 1 モニタ分の暗化ウィンドウ
   │   └─ QuickMenuWindow.xaml(.cs) … ON/OFF ボタン + 暗さスライダーのみの最小メニュー
   └─ Themes/Dark.xaml            … ダークテーマ
```

## 設計メモ

- **ON/OFF と暗さの操作はすべて `ShellViewModel` の Command / メソッド**として定義している。
  トレイ・クイックメニュー・ショートカットはいずれもそれを呼ぶだけなので、
  入力手段を増やしても ViewModel 側の変更は不要。
- 暗さは背景ブラシのアルファ値で表現する。`AllowsTransparency="True"` の WPF ウィンドウは
  `WS_EX_LAYERED` 付きのレイヤードウィンドウとして生成される
  （`False` の場合 WPF が `WS_EX_LAYERED` を自前で外し続けるため、
  `SetLayeredWindowAttributes` による半透明化は機能しない）。
- WPF が面倒を見てくれない `WS_EX_TRANSPARENT`（クリック透過）/ `WS_EX_TOOLWINDOW`（Alt+Tab に出さない）
  / `WS_EX_NOACTIVATE`（フォーカスを奪わない）は `SetWindowLong` で自前で付与する。
- ウィンドウの配置は WPF の論理ピクセルではなく `SetWindowPos` による物理ピクセル指定。
  モニタごとに DPI が異なっていても隙間なく覆える。
- ディスプレイ構成の変更（解像度変更・モニタの抜き差し）は `SystemEvents.DisplaySettingsChanged`
  を検知してオーバーレイを作り直す。
- クイックメニューの表示位置は、表示前からカーソルと同じモニタの作業領域内に置いてから
  実測サイズで最終位置を決め、その作業領域（タスクバー除く）の外には出さないようクランプしている。
  一度画面外の座標で仮表示してから移動する実装だと、モニタごとに DPI が異なる環境で
  移動後にサイズが変わってしまい、タスクバーの裏に一部が隠れることがあったため。
- **集中モード（応答負荷モード）の自動 ON 対策**: Windows は「トップモスト・枠なしでモニタ全体を
  ぴったり覆うウィンドウ」を全画面表示のアプリとみなし、通知設定の
  「応答負荷を自動的にオンにする」→「全画面モードでアプリを使用するとき」ルールにより
  集中モードを自動的に有効化することがある。これを避けるため、オーバーレイの高さを実際の
  モニタ高さより 1px だけ短くし、「モニタにぴったり一致」しないようにしている
  （[OverlayWindow.xaml.cs](src/NightShade/Views/OverlayWindow.xaml.cs)）。見た目にはほぼ影響しない。
  ただし Windows 側のこの自動ルール自体をユーザーが「全画面モードでアプリを使用するとき」で
  オフにしておくのが最も確実な対策になる。
- **効率モード（EcoQoS）対策**: メインウィンドウを持たないトレイ常駐アプリは Windows から
  「バックグラウンドのみのアプリ」と判定され、実行速度が自動的に絞られる（タスクマネージャーの
  効率モードが勝手に有効になる）ことがある。起動時に加えて、暗化を ON にした瞬間と、ON の間は
  30 秒おきに `SetProcessInformation`（`PROCESS_POWER_THROTTLING_EXECUTION_SPEED` を無効化）を
  呼び直すことで、Windows が再度スロットリングを掛け直すのに対抗している
  （[OverlayController.cs](src/NightShade/Services/OverlayController.cs) / [NativeMethods.cs](src/NightShade/Interop/NativeMethods.cs)）。
- **他の Topmost ウィンドウにオーバーレイが埋もれる問題への対策**: トレイアイコンをクリックした
  直後はタスクバー自身が一瞬 Z オーダーの先頭に来るなど、オーバーレイの下に隠れることがある。
  上記と同じ 30 秒タイマーで `SetWindowPos(HWND_TOPMOST)` による最前面の再アサートも行っている。
  応答性より負荷の低さを優先しているため、少し遅れて復帰する形になる。IME の変換候補ウィンドウの
  ように一瞬だけ現れて消えるものまでは追従できないが、これは許容している（下記）。
- **意図的に対応していないもの / 対応できないもの**:
  - IME の変換候補（予測変換）ウィンドウは暗化されない。表示・消滅が非常に速い一時的な UI であり、
    上記の低頻度な再アサートでは実用上追従できないため対応を見送っている。
  - タスクバー・スタートメニュー・通知センター・クイック設定などの Explorer シェル面は、
    通常の Topmost よりもさらに上位の特別な Z オーダー帯域で描画されており、公開 Win32 API では
    追い越せない。f.lux など他の画面暗化ツールにも共通する既知の限界。
  - マウスポインターは OS のハードウェアカーソルプレーンとして、あらゆるウィンドウの最前面に
    常に別途合成される。これはどのアプリ（Windows 標準の Night Light 等も含む）からも
    暗くすることができない、OS の表示パイプライン自体の仕様。
