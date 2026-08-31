namespace NightShade.Models;

/// <summary>
/// settings.json に永続化される設定。
/// ON/OFF 状態は意図的に含めない（起動時は常に OFF から始める仕様のため）。
/// </summary>
public sealed class AppSettings
{
    /// <summary>暗さ（オーバーレイの不透明度）。0.0 ～ 0.9。</summary>
    public double Opacity { get; set; } = 0.35;

    /// <summary>ショートカット等で 1 段階変化させる量。</summary>
    public double OpacityStep { get; set; } = 0.05;

    /// <summary>グローバルショートカットキーを有効にするか。</summary>
    public bool EnableGlobalHotKeys { get; set; } = true;

    public AppSettings Clone() => new()
    {
        Opacity = Opacity,
        OpacityStep = OpacityStep,
        EnableGlobalHotKeys = EnableGlobalHotKeys
    };
}
