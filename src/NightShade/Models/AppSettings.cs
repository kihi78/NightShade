namespace NightShade.Models;

/// <summary>
/// settings.json に永続化される設定。
/// ON/OFF 状態は意図的に含めない（起動時は常に OFF から始める仕様のため）。
/// </summary>
public sealed class AppSettings
{
    /// <summary>暗さ（オーバーレイの不透明度）。0.0 ～ 0.9。</summary>
    public double Opacity { get; set; } = 0.35;

    /// <summary>スライダーの 1 目盛りで変化させる量。</summary>
    public double OpacityStep { get; set; } = 0.05;

    public AppSettings Clone() => new()
    {
        Opacity = Opacity,
        OpacityStep = OpacityStep
    };
}
