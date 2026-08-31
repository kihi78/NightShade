using System;
using System.Windows.Input;
using System.Windows.Threading;
using NightShade.Models;
using NightShade.Services;

namespace NightShade.ViewModels;

/// <summary>
/// アプリ全体の状態を持つ ViewModel。
/// トレイアイコン / クイックメニュー / グローバルショートカットの
/// すべてがこの ViewModel の Command・プロパティを操作する（UI 層とは疎結合）。
/// </summary>
public sealed class ShellViewModel : ObservableObject, IDisposable
{
    /// <summary>設定できる暗さの下限。</summary>
    public const double MinOpacity = 0.0;

    /// <summary>設定できる暗さの上限（完全な真っ黒は操作不能になるため 0.9 まで）。</summary>
    public const double MaxOpacity = 0.9;

    private readonly AppSettings _settings;
    private readonly ISettingsService _settingsService;
    private readonly IOverlayController _overlay;
    private readonly IWindowService _windows;
    private readonly DispatcherTimer _saveTimer;

    private bool _isOverlayEnabled;
    private bool _disposed;

    public ShellViewModel(
        AppSettings settings,
        ISettingsService settingsService,
        IOverlayController overlay,
        IWindowService windows)
    {
        _settings = settings;
        _settingsService = settingsService;
        _overlay = overlay;
        _windows = windows;

        // スライダー操作のたびにファイル書き込みが走らないよう保存を遅延させる
        _saveTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(600) };
        _saveTimer.Tick += (_, _) =>
        {
            _saveTimer.Stop();
            SaveNow();
        };

        _overlay.SetOpacity(_settings.Opacity);

        // 仕様: 起動時は常に OFF から始める（ON/OFF 状態は保存しない）
        _isOverlayEnabled = false;

        ToggleOverlayCommand = new RelayCommand(ToggleOverlay);
        EnableOverlayCommand = new RelayCommand(() => IsOverlayEnabled = true);
        DisableOverlayCommand = new RelayCommand(() => IsOverlayEnabled = false);
        IncreaseDarknessCommand = new RelayCommand(IncreaseDarkness);
        DecreaseDarknessCommand = new RelayCommand(DecreaseDarkness);
        OpenQuickMenuCommand = new RelayCommand(_windows.ShowQuickMenu);
        ExitCommand = new RelayCommand(Exit);
    }

    /// <summary>暗化オーバーレイの ON/OFF。</summary>
    public bool IsOverlayEnabled
    {
        get => _isOverlayEnabled;
        set
        {
            if (!SetProperty(ref _isOverlayEnabled, value))
            {
                return;
            }

            _overlay.SetEnabled(value);
            OnPropertyChanged(nameof(ToggleCaption));
            OnPropertyChanged(nameof(StatusText));
        }
    }

    /// <summary>暗さ（オーバーレイの不透明度）。0.0 ～ 0.9。</summary>
    public double Opacity
    {
        get => _settings.Opacity;
        set
        {
            var clamped = Math.Clamp(Math.Round(value, 3), MinOpacity, MaxOpacity);
            if (Math.Abs(clamped - _settings.Opacity) < 0.0005)
            {
                return;
            }

            _settings.Opacity = clamped;
            _overlay.SetOpacity(clamped);

            OnPropertyChanged();
            OnPropertyChanged(nameof(DarknessPercent));
            OnPropertyChanged(nameof(StatusText));
            ScheduleSave();
        }
    }

    /// <summary>1 段階で変化する暗さの量（グローバルショートカット等で使用）。</summary>
    public double OpacityStep
    {
        get => _settings.OpacityStep;
        set
        {
            var clamped = Math.Clamp(Math.Round(value, 3), 0.01, 0.25);
            if (Math.Abs(clamped - _settings.OpacityStep) < 0.0005)
            {
                return;
            }

            _settings.OpacityStep = clamped;
            OnPropertyChanged();
            ScheduleSave();
        }
    }

    /// <summary>グローバルショートカットを使うかどうか。</summary>
    public bool EnableGlobalHotKeys
    {
        get => _settings.EnableGlobalHotKeys;
        set
        {
            if (_settings.EnableGlobalHotKeys == value)
            {
                return;
            }

            _settings.EnableGlobalHotKeys = value;
            OnPropertyChanged();
            ScheduleSave();
        }
    }

    /// <summary>表示用の暗さ（％）。</summary>
    public int DarknessPercent => (int)Math.Round(Opacity * 100);

    /// <summary>トグルボタンに表示する文字列。</summary>
    public string ToggleCaption => IsOverlayEnabled ? "ON" : "OFF";

    /// <summary>トレイのツールチップ等に使う状態表示。</summary>
    public string StatusText => IsOverlayEnabled
        ? $"ON ({DarknessPercent}%)"
        : "OFF";

    public ICommand ToggleOverlayCommand { get; }

    public ICommand EnableOverlayCommand { get; }

    public ICommand DisableOverlayCommand { get; }

    public ICommand IncreaseDarknessCommand { get; }

    public ICommand DecreaseDarknessCommand { get; }

    public ICommand OpenQuickMenuCommand { get; }

    public ICommand ExitCommand { get; }

    /// <summary>ON/OFF を切り替える。</summary>
    public void ToggleOverlay() => IsOverlayEnabled = !IsOverlayEnabled;

    /// <summary>1 段階暗くする。OFF のときは自動的に ON にする。</summary>
    public void IncreaseDarkness()
    {
        Opacity += OpacityStep;
        IsOverlayEnabled = true;
    }

    /// <summary>1 段階明るくする。0 まで下げたら OFF にする。</summary>
    public void DecreaseDarkness()
    {
        Opacity -= OpacityStep;
        if (Opacity <= MinOpacity + 0.0005)
        {
            IsOverlayEnabled = false;
        }
    }

    /// <summary>設定を即座に保存する。</summary>
    public void SaveNow()
    {
        _saveTimer.Stop();
        _settingsService.Save(_settings);
    }

    private void ScheduleSave()
    {
        _saveTimer.Stop();
        _saveTimer.Start();
    }

    private void Exit()
    {
        SaveNow();
        _windows.Shutdown();
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        SaveNow();
    }
}
