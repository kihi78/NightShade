using System;
using System.ComponentModel;
using System.Threading;
using System.Windows;
using System.Windows.Input;
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
    private HotKeyService? _hotKeyService;
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
        _shell.PropertyChanged += OnShellPropertyChanged;

        _trayIconService = new TrayIconService(_shell);
        _trayIconService.Initialize();

        _hotKeyService = new HotKeyService();
        ApplyHotKeys();
    }

    private void OnShellPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ShellViewModel.EnableGlobalHotKeys))
        {
            ApplyHotKeys();
        }
    }

    /// <summary>
    /// ショートカットの割り当て。追加したい場合はここに 1 行足すだけでよい。
    /// </summary>
    private void ApplyHotKeys()
    {
        if (_hotKeyService is null || _shell is null)
        {
            return;
        }

        _hotKeyService.UnregisterAll();

        if (!_shell.EnableGlobalHotKeys)
        {
            return;
        }

        const uint ctrlShift = NativeMethods.MOD_CONTROL | NativeMethods.MOD_SHIFT;

        _hotKeyService.TryRegister(ctrlShift, Key.Up, () => _shell.IncreaseDarknessCommand.Execute(null));
        _hotKeyService.TryRegister(ctrlShift, Key.Down, () => _shell.DecreaseDarknessCommand.Execute(null));
        _hotKeyService.TryRegister(ctrlShift, Key.N, () => _shell.ToggleOverlayCommand.Execute(null));
    }

    protected override void OnExit(ExitEventArgs e)
    {
        if (_shell is not null)
        {
            _shell.PropertyChanged -= OnShellPropertyChanged;
        }

        _hotKeyService?.Dispose();
        _trayIconService?.Dispose();
        _shell?.Dispose();
        _overlayController?.Dispose();

        _singleInstanceMutex?.Dispose();

        base.OnExit(e);
    }
}
