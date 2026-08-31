using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using NightShade.Interop;
using NightShade.ViewModels;

namespace NightShade.Services;

/// <summary>
/// タスクトレイ常駐部分。
/// 左クリック = ON/OFF 切り替え、右クリック = クイックメニュー表示。
/// 入力を ViewModel の Command に流すだけで、状態は一切持たない。
/// </summary>
public sealed class TrayIconService : IDisposable
{
    private readonly ShellViewModel _viewModel;
    private readonly NotifyIcon _notifyIcon;

    private Icon? _iconOn;
    private Icon? _iconOff;
    private bool _disposed;

    public TrayIconService(ShellViewModel viewModel)
    {
        _viewModel = viewModel;
        _notifyIcon = new NotifyIcon();
    }

    public void Initialize()
    {
        _iconOn = CreateMoonIcon(active: true);
        _iconOff = CreateMoonIcon(active: false);

        _notifyIcon.MouseUp += OnMouseUp;
        _notifyIcon.Visible = true;

        _viewModel.PropertyChanged += OnViewModelPropertyChanged;
        UpdateVisual();
    }

    private void OnMouseUp(object? sender, MouseEventArgs e)
    {
        switch (e.Button)
        {
            case MouseButtons.Left:
                _viewModel.ToggleOverlayCommand.Execute(null);
                break;

            case MouseButtons.Right:
                _viewModel.OpenQuickMenuCommand.Execute(null);
                break;

            case MouseButtons.Middle:
                // クイックメニューに「終了」ボタンを置かない代わりの終了経路。
                _viewModel.ExitCommand.Execute(null);
                break;
        }
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(ShellViewModel.IsOverlayEnabled))
        {
            UpdateVisual();
        }
    }

    private void UpdateVisual()
    {
        // アイコンの色（ON/OFF）で状態を示す。ツールチップは常にアプリ名を表示する。
        _notifyIcon.Icon = _viewModel.IsOverlayEnabled ? _iconOn : _iconOff;
        _notifyIcon.Text = "NightShade";
    }

    /// <summary>三日月アイコンを実行時に描画する（外部アイコンファイルを不要にするため）。</summary>
    private static Icon CreateMoonIcon(bool active)
    {
        const int size = 32;

        using var bitmap = new Bitmap(size, size);
        using (var graphics = Graphics.FromImage(bitmap))
        {
            graphics.SmoothingMode = SmoothingMode.AntiAlias;
            graphics.Clear(Color.Transparent);

            using var full = new GraphicsPath();
            full.AddEllipse(3f, 3f, 26f, 26f);

            using var cut = new GraphicsPath();
            cut.AddEllipse(11f, -3f, 26f, 26f);

            using var region = new Region(full);
            region.Exclude(cut);

            var color = active
                ? Color.FromArgb(255, 122, 167, 255)
                : Color.FromArgb(255, 150, 150, 165);

            using var brush = new SolidBrush(color);
            graphics.FillRegion(brush, region);
        }

        var handle = bitmap.GetHicon();
        try
        {
            using var temp = Icon.FromHandle(handle);
            return (Icon)temp.Clone();
        }
        finally
        {
            NativeMethods.DestroyIcon(handle);
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
        _notifyIcon.MouseUp -= OnMouseUp;
        _notifyIcon.Visible = false;
        _notifyIcon.Icon = null;
        _notifyIcon.Dispose();

        _iconOn?.Dispose();
        _iconOff?.Dispose();
    }
}
