using System;
using System.Threading;
using System.Windows;
using NightShade.Interop;
using NightShade.Services;
using NightShade.ViewModels;

namespace NightShade;

/// <summary>
/// 合成ルート。ここで各サービスと ViewModel を組み立てて配線する。
/// StartupUri は持たず、メインウィンドウを表示せずにトレイ常駐で起動する。
/// </summary>
public partial class App : Application
{
    private const string SingleInstanceMutexName = "NightShade.SingleInstance";

    private Mutex? _singleInstanceMutex;
    private JsonSettingsService? _settingsService;
    private OverlayController? _overlayController;
    private WindowService? _windowService;
    private TrayIconService? _trayIconService;
    private ShellViewModel? _shell;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // メインウィンドウを持たないため Windows に「バックグラウンドのみのアプリ」と
        // 判定され、効率モード（EcoQoS）で自動的にスロットリングされることがある。
        // 応答性を保つため起動直後に無効化しておく（失敗しても致命的ではないので無視する）。
        NativeMethods.DisableExecutionSpeedThrottling();

        // 二重起動を防ぐ（トレイアイコンが増えてしまうため）
        _singleInstanceMutex = new Mutex(true, SingleInstanceMutexName, out var isFirstInstance);
        if (!isFirstInstance)
        {
            Shutdown();
            return;
        }

        _settingsService = new JsonSettingsService();
        var settings = _settingsService.Load();

        _overlayController = new OverlayController();
        _windowService = new WindowService();

        _shell = new ShellViewModel(settings, _settingsService, _overlayController, _windowService);
        _windowService.Bind(_shell);

        _trayIconService = new TrayIconService(_shell);
        _trayIconService.Initialize();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _trayIconService?.Dispose();
        _shell?.Dispose();
        _overlayController?.Dispose();

        _singleInstanceMutex?.Dispose();

        base.OnExit(e);
    }
}
