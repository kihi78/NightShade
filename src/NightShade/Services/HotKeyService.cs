using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Input;
using System.Windows.Interop;
using NightShade.Interop;

namespace NightShade.Services;

/// <summary>
/// グローバルショートカットキーの登録。
/// メッセージ専用ウィンドウを 1 つ作り、WM_HOTKEY を受けて登録済みの処理を呼ぶ。
/// ViewModel の Command をそのまま渡せるので、キー割り当ての追加は 1 行で済む。
/// </summary>
public sealed class HotKeyService : IDisposable
{
    private readonly HwndSource _source;
    private readonly Dictionary<int, Action> _handlers = new();

    private int _nextId = 0x4E53; // 'NS'
    private bool _disposed;

    public HotKeyService()
    {
        var parameters = new HwndSourceParameters("NightShade.HotKeyWindow")
        {
            ParentWindow = NativeMethods.HWND_MESSAGE,
            WindowStyle = 0
        };

        _source = new HwndSource(parameters);
        _source.AddHook(WndProc);
    }

    /// <summary>ショートカットを登録する。他アプリと競合した場合は false を返す。</summary>
    public bool TryRegister(uint modifiers, Key key, Action handler)
    {
        var id = _nextId++;
        var virtualKey = (uint)KeyInterop.VirtualKeyFromKey(key);

        if (!NativeMethods.RegisterHotKey(_source.Handle, id, modifiers | NativeMethods.MOD_NOREPEAT, virtualKey))
        {
            Debug.WriteLine($"[NightShade] ショートカットの登録に失敗しました: {modifiers} + {key}");
            return false;
        }

        _handlers[id] = handler;
        return true;
    }

    /// <summary>登録済みのショートカットをすべて解除する。</summary>
    public void UnregisterAll()
    {
        foreach (var id in _handlers.Keys)
        {
            NativeMethods.UnregisterHotKey(_source.Handle, id);
        }

        _handlers.Clear();
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == NativeMethods.WM_HOTKEY && _handlers.TryGetValue(wParam.ToInt32(), out var action))
        {
            handled = true;
            action();
        }

        return IntPtr.Zero;
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        UnregisterAll();
        _source.RemoveHook(WndProc);
        _source.Dispose();
    }
}
