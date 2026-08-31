using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace NightShade.Interop;

/// <summary>
/// ディスプレイ構成を物理ピクセルで取得するヘルパー。
/// WPF の SystemParameters.VirtualScreen* は DPI スケールの影響を受けるため、
/// 混在 DPI のマルチモニタ環境では Win32 の値を直接使う。
/// </summary>
internal static class ScreenHelper
{
    /// <summary>接続されている全ディスプレイを取得する。</summary>
    public static IReadOnlyList<MonitorInfo> GetMonitors()
    {
        var result = new List<MonitorInfo>();

        // コールバックはローカル変数に保持して GC による回収を防ぐ
        NativeMethods.MonitorEnumProc callback = (IntPtr hMonitor, IntPtr hdc, ref NativeMethods.RECT rect, IntPtr data) =>
        {
            if (TryGetMonitorInfo(hMonitor, out var info))
            {
                result.Add(info);
            }

            return true;
        };

        NativeMethods.EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, callback, IntPtr.Zero);
        GC.KeepAlive(callback);

        if (result.Count == 0)
        {
            // 列挙に失敗した場合のフォールバック
            var fallback = new PixelRect(0, 0, 1920, 1080);
            result.Add(new MonitorInfo(fallback, fallback, true));
        }

        return result;
    }

    /// <summary>指定した座標を含むディスプレイの作業領域を取得する。</summary>
    public static PixelRect GetWorkAreaFromPoint(NativeMethods.POINT point)
    {
        var handle = NativeMethods.MonitorFromPoint(point, NativeMethods.MONITOR_DEFAULTTONEAREST);
        if (handle != IntPtr.Zero && TryGetMonitorInfo(handle, out var info))
        {
            return info.WorkArea;
        }

        return new PixelRect(0, 0, 1920, 1080);
    }

    private static bool TryGetMonitorInfo(IntPtr hMonitor, out MonitorInfo info)
    {
        var native = new NativeMethods.MONITORINFO
        {
            cbSize = Marshal.SizeOf<NativeMethods.MONITORINFO>()
        };

        if (!NativeMethods.GetMonitorInfo(hMonitor, ref native))
        {
            info = null!;
            return false;
        }

        info = new MonitorInfo(
            ToRect(native.rcMonitor),
            ToRect(native.rcWork),
            (native.dwFlags & NativeMethods.MONITORINFOF_PRIMARY) != 0);
        return true;
    }

    private static PixelRect ToRect(NativeMethods.RECT rect)
        => new(rect.Left, rect.Top, rect.Right, rect.Bottom);
}
