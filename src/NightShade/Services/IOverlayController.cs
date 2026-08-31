namespace NightShade.Services;

/// <summary>
/// 暗化オーバーレイの表示制御。ViewModel はこのインターフェイスにのみ依存する。
/// </summary>
public interface IOverlayController
{
    /// <summary>オーバーレイが表示中かどうか。</summary>
    bool IsEnabled { get; }

    /// <summary>オーバーレイの表示 / 非表示を切り替える。</summary>
    void SetEnabled(bool enabled);

    /// <summary>暗さ（0.0 = 透明 ～ 1.0 = 真っ黒）を設定する。</summary>
    void SetOpacity(double opacity);
}
