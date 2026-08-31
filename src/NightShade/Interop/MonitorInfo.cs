namespace NightShade.Interop;

/// <summary>物理ピクセル単位の矩形。</summary>
public readonly record struct PixelRect(int Left, int Top, int Right, int Bottom)
{
    public int Width => Right - Left;

    public int Height => Bottom - Top;
}

/// <summary>1 台のディスプレイの情報（すべて物理ピクセル）。</summary>
/// <param name="Bounds">画面全体の矩形。</param>
/// <param name="WorkArea">タスクバー等を除いた作業領域。</param>
/// <param name="IsPrimary">プライマリディスプレイなら true。</param>
public sealed record MonitorInfo(PixelRect Bounds, PixelRect WorkArea, bool IsPrimary);
