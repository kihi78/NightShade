using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Threading;
using Microsoft.Win32;
using NightShade.Interop;
using NightShade.Views;

namespace NightShade.Services;

/// <summary>
/// 全ディスプレイ分の <see cref="OverlayWindow"/> をまとめて管理する。
/// 解像度変更やモニタの抜き差しを検知して自動で作り直す。
/// </summary>
public sealed class OverlayController : IOverlayController, IDisposable
{
    private readonly List<OverlayWindow> _windows = new();
    private readonly DispatcherTimer _throttlingGuardTimer;
    private readonly DispatcherTimer _startupReassertTimer;

    // WinEventHook のコールバックは GC で回収されないようフィールドで保持し続ける必要がある。
    private readonly NativeMethods.WinEventProc _foregroundChangedCallback;
    private IntPtr _foregroundHook = IntPtr.Zero;

    private double _opacity = 0.35;
    private bool _isEnabled;
    private bool _disposed;

    public OverlayController()
    {
        SystemEvents.DisplaySettingsChanged += OnDisplaySettingsChanged;
        _foregroundChangedCallback = OnForegroundWindowChanged;

        // オーバーレイ ON 中、Windows が「フォアグラウンドの窓を持たないバックグラウンド
        // アプリ」と再判定して効率モード（EcoQoS）を掛け直してくることがあるため、
        // ON の間は定期的に無効化コマンドを送り直して対抗する。負荷を抑えるため間隔は長め。
        _throttlingGuardTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(30) };
        _throttlingGuardTimer.Tick += (_, _) => NativeMethods.DisableExecutionSpeedThrottling();

        // ON にした直後の一瞬だけ、Explorer がタスクバーを自身の Z バンドへ載せ直す処理と
        // 競合し、タスクバーだけがオーバーレイの上に残ることがある。生成直後の即時
        // 再アサートに加えて、少し時間を置いてもう一度だけ再アサートすることで、
        // Explorer 側の処理が落ち着いた後の状態でも確実に上に来るようにする。
        _startupReassertTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(300) };
        _startupReassertTimer.Tick += (_, _) =>
        {
            _startupReassertTimer.Stop();
            foreach (var window in _windows)
            {
                window.ReassertTopmost();
            }
        };
    }

    public bool IsEnabled => _isEnabled;

    public void SetEnabled(bool enabled)
    {
        if (_isEnabled == enabled)
        {
            return;
        }

        _isEnabled = enabled;

        if (enabled)
        {
            // ON にした直後に再度無効化し、以降も定期的に再適用し続ける。
            NativeMethods.DisableExecutionSpeedThrottling();
            _throttlingGuardTimer.Start();
            StartForegroundWatch();
            CreateWindows();
            _startupReassertTimer.Start();
        }
        else
        {
            _throttlingGuardTimer.Stop();
            _startupReassertTimer.Stop();
            StopForegroundWatch();
            CloseWindows();
        }
    }

    public void SetOpacity(double opacity)
    {
        _opacity = Math.Clamp(opacity, 0.0, 1.0);

        foreach (var window in _windows)
        {
            window.SetOverlayOpacity(_opacity);
        }
    }

    private void CreateWindows()
    {
        foreach (var monitor in ScreenHelper.GetMonitors())
        {
            var window = new OverlayWindow(monitor, _opacity);
            _windows.Add(window);
            window.Show();

            // Show() 直後は、Explorer がタスクバーを自身の Z バンドへ載せ直す処理と
            // 競合し、生成直後の 1 回だけタスクバーがオーバーレイの上に残ることがある
            // （その後、画面やタスクバーを右クリックするなどしてフォアグラウンド変化が
            // 起きると WinEventHook 経由の ReassertTopmost で正しく直る）。
            // 生成直後にもう一度アサートし直しておくことで、ON にした瞬間から
            // タスクバーもきちんと暗化された状態にする。
            window.ReassertTopmost();
        }
    }

    private void CloseWindows()
    {
        foreach (var window in _windows)
        {
            window.Close();
        }

        _windows.Clear();
    }

    /// <summary>
    /// フォアグラウンドウィンドウの変化を監視する。
    ///
    /// タスクバーのアプリアイコンをクリックするなどしてフォアグラウンドが切り替わると、
    /// タスクバー自身が一瞬 Z オーダーの先頭に来てオーバーレイの下に隠れ、それきり戻らない
    /// ことがある。ポーリング間隔を詰めると常時 CPU を消費してしまうため、代わりに
    /// フォアグラウンド変化イベント（WinEvent）を直接受け取り、変化があった瞬間にだけ
    /// 最前面を再アサートする。アイドル中はイベントが来ない限りコストが掛からない。
    /// </summary>
    private void StartForegroundWatch()
    {
        if (_foregroundHook != IntPtr.Zero)
        {
            return;
        }

        _foregroundHook = NativeMethods.SetWinEventHook(
            NativeMethods.EVENT_SYSTEM_FOREGROUND,
            NativeMethods.EVENT_SYSTEM_FOREGROUND,
            IntPtr.Zero,
            _foregroundChangedCallback,
            0,
            0,
            NativeMethods.WINEVENT_OUTOFCONTEXT | NativeMethods.WINEVENT_SKIPOWNPROCESS);
    }

    private void StopForegroundWatch()
    {
        if (_foregroundHook == IntPtr.Zero)
        {
            return;
        }

        NativeMethods.UnhookWinEvent(_foregroundHook);
        _foregroundHook = IntPtr.Zero;
    }

    private void OnForegroundWindowChanged(
        IntPtr hWinEventHook,
        uint eventType,
        IntPtr hwnd,
        int idObject,
        int idChild,
        uint dwEventThread,
        uint dwmsEventTime)
    {
        foreach (var window in _windows)
        {
            window.ReassertTopmost();
        }
    }

    private void OnDisplaySettingsChanged(object? sender, EventArgs e)
    {
        // SystemEvents は専用スレッドから発火するため UI スレッドに戻す
        var dispatcher = Application.Current?.Dispatcher;
        if (dispatcher is null)
        {
            return;
        }

        dispatcher.InvokeAsync(() =>
        {
            if (!_isEnabled || _disposed)
            {
                return;
            }

            CloseWindows();
            CreateWindows();
            _startupReassertTimer.Start();
        });
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        SystemEvents.DisplaySettingsChanged -= OnDisplaySettingsChanged;
        _throttlingGuardTimer.Stop();
        _startupReassertTimer.Stop();
        StopForegroundWatch();
        CloseWindows();
    }
}
