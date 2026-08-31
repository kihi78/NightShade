using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using NightShade.Interop;

namespace NightShade.Views;

/// <summary>
/// タスクトレイ右クリックで出るクイックメニュー。
/// マウスカーソル位置に合わせて表示し、フォーカスが外れたら閉じる。
/// </summary>
public partial class QuickMenuWindow : Window
{
    private const int EdgeMargin = 8;

    public QuickMenuWindow()
    {
        InitializeComponent();

        Deactivated += (_, _) => Close();
        PreviewKeyDown += OnPreviewKeyDown;
    }

    /// <summary>マウスカーソルの近く（作業領域からはみ出さない位置）に表示する。</summary>
    public void ShowNearCursor()
    {
        if (!NativeMethods.GetCursorPos(out var cursor))
        {
            Show();
            Activate();
            return;
        }

        var work = ScreenHelper.GetWorkAreaFromPoint(cursor);

        // 表示前からカーソルと同じモニタ内に置く。
        // 遠く離れた座標（画面外）で先に測ると、モニタごとに DPI が異なる環境で
        // 実際の表示位置に移動した際にサイズが変わってしまい、
        // 高さの見積もりがずれてタスクバーの裏に隠れる原因になるため。
        Left = work.Left + EdgeMargin;
        Top = work.Top + EdgeMargin;
        Show();
        UpdateLayout();

        var handle = new WindowInteropHelper(this).Handle;
        if (handle == IntPtr.Zero)
        {
            Activate();
            return;
        }

        NativeMethods.GetWindowRect(handle, out var rect);
        var width = rect.Right - rect.Left;
        var height = rect.Bottom - rect.Top;

        // 横位置: カーソルの左右中央（作業領域内に収める）
        var maxX = Math.Max(work.Left + EdgeMargin, work.Right - width - EdgeMargin);
        var x = Math.Clamp(cursor.X - width / 2, work.Left + EdgeMargin, maxX);

        // 縦位置: 基本はカーソルの上。入りきらなければ下に出すが、
        // どちらにしても作業領域（タスクバーを除いた領域）の外には出さない。
        var maxY = Math.Max(work.Top + EdgeMargin, work.Bottom - height - EdgeMargin);
        var y = cursor.Y - height - EdgeMargin;
        if (y < work.Top + EdgeMargin)
        {
            y = cursor.Y + EdgeMargin;
        }

        y = Math.Clamp(y, work.Top + EdgeMargin, maxY);

        NativeMethods.SetWindowPos(
            handle,
            NativeMethods.HWND_TOPMOST,
            x,
            y,
            0,
            0,
            NativeMethods.SWP_NOSIZE | NativeMethods.SWP_SHOWWINDOW);

        Activate();
    }

    private void OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            Close();
        }
    }
}
