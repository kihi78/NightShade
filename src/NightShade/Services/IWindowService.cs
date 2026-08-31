namespace NightShade.Services;

/// <summary>
/// ViewModel から画面を開くための抽象。ViewModel が View を直接 new しないようにする。
/// </summary>
public interface IWindowService
{
    /// <summary>タスクトレイ右クリック時のクイックメニューをカーソル位置に表示する。</summary>
    void ShowQuickMenu();

    /// <summary>クイックメニューが開いていれば閉じる。</summary>
    void CloseQuickMenu();

    /// <summary>アプリケーションを終了する。</summary>
    void Shutdown();
}
