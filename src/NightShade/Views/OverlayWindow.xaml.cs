using System;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using NightShade.Interop;

namespace NightShade.Views;

/// <summary>
/// 1 台のディスプレイを覆う暗化ウィンドウ。
///
/// 暗さは背景ブラシのアルファ値で表現する。
/// AllowsTransparency="True" の WPF ウィンドウは WS_EX_LAYERED を持つレイヤードウィンドウとして
/// 作られる（逆に False の場合、WPF は WS_EX_LAYERED を自前で外してしまうため
/// SetLayeredWindowAttributes による半透明化は使えない）。
///
/// 一方で以下の拡張スタイルは WPF が面倒を見てくれないので SetWindowLong で自分で付ける。
///   WS_EX_TRANSPARENT … マウス操作を完全に透過させ、下の画面をそのまま操作できるようにする
///   WS_EX_TOOLWINDOW  … Alt+Tab に出さない
///   WS_EX_NOACTIVATE  … フォーカスを奪わない
/// </summary>
public partial class OverlayWindow : Window
{
    private readonly MonitorInfo _monitor;

    public OverlayWindow(MonitorInfo monitor, double overlayOpacity)
    {
        InitializeComponent();

        _monitor = monitor;

        // 実際の位置・サイズは OnSourceInitialized で物理ピクセル指定に上書きする。
        // ここでは初期表示時のちらつきを抑えるための概算値を入れておく。
        Left = monitor.Bounds.Left;
        Top = monitor.Bounds.Top;
        Width = monitor.Bounds.Width;
        Height = monitor.Bounds.Height;

        SetOverlayOpacity(overlayOpacity);
    }

    /// <summary>このウィンドウが担当するディスプレイ。</summary>
    public MonitorInfo Monitor => _monitor;

    /// <summary>暗さを更新する。</summary>
    public void SetOverlayOpacity(double overlayOpacity)
    {
        var alpha = (byte)Math.Round(Math.Clamp(overlayOpacity, 0.0, 1.0) * 255);

        var brush = new SolidColorBrush(Color.FromArgb(alpha, 0, 0, 0));
        brush.Freeze();
        Background = brush;
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);

        var handle = new WindowInteropHelper(this).Handle;

        var exStyle = NativeMethods.GetWindowLong(handle, NativeMethods.GWL_EXSTYLE);
        exStyle |= NativeMethods.WS_EX_TRANSPARENT
                   | NativeMethods.WS_EX_TOOLWINDOW
                   | NativeMethods.WS_EX_NOACTIVATE;
        NativeMethods.SetWindowLong(handle, NativeMethods.GWL_EXSTYLE, exStyle);

        // WPF の論理ピクセルではなく物理ピクセルで配置する
        // （モニタごとに DPI が違っても正確に画面全体を覆うため）
        var bounds = _monitor.Bounds;
        NativeMethods.SetWindowPos(
            handle,
            NativeMethods.HWND_TOPMOST,
            bounds.Left,
            bounds.Top,
            bounds.Width,
            bounds.Height,
            NativeMethods.SWP_NOACTIVATE | NativeMethods.SWP_SHOWWINDOW);
    }

    /// <summary>
    /// 最前面（Topmost）を再アサートする。
    /// 他のアプリがトップモストウィンドウを新たに作ると Z オーダーの先頭を奪われることがあるため、
    /// ON の間は定期的に呼び出して先頭に戻す。
    /// ただし Explorer が管理するシェル面（タスクバー・通知センター等）は、通常の
    /// トップモストより上位の Z バンドで描画されており、この再アサートでは追い抜けない。
    /// </summary>
    public void ReassertTopmost()
    {
        var handle = new WindowInteropHelper(this).Handle;
        if (handle == IntPtr.Zero)
        {
            return;
        }

        NativeMethods.SetWindowPos(
            handle,
            NativeMethods.HWND_TOPMOST,
            0,
            0,
            0,
            0,
            NativeMethods.SWP_NOMOVE | NativeMethods.SWP_NOSIZE | NativeMethods.SWP_NOACTIVATE);
    }
}
