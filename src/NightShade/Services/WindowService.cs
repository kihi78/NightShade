using System.Windows;
using NightShade.ViewModels;
using NightShade.Views;

namespace NightShade.Services;

/// <summary>
/// <see cref="IWindowService"/> の WPF 実装。View を知っているのはこのクラスだけ。
/// </summary>
public sealed class WindowService : IWindowService
{
    private ShellViewModel? _viewModel;
    private QuickMenuWindow? _quickMenu;

    /// <summary>ViewModel との相互参照を避けるため、生成後に注入する。</summary>
    public void Bind(ShellViewModel viewModel) => _viewModel = viewModel;

    public void ShowQuickMenu()
    {
        if (_viewModel is null)
        {
            return;
        }

        if (_quickMenu is not null)
        {
            CloseQuickMenu();
            return;
        }

        var window = new QuickMenuWindow { DataContext = _viewModel };
        window.Closed += (_, _) => _quickMenu = null;
        _quickMenu = window;
        window.ShowNearCursor();
    }

    public void CloseQuickMenu()
    {
        _quickMenu?.Close();
        _quickMenu = null;
    }

    public void Shutdown() => Application.Current?.Shutdown();
}
