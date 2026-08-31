using NightShade.Models;

namespace NightShade.Services;

/// <summary>設定の読み書きを抽象化する。保存先を変えたい場合はこの実装を差し替える。</summary>
public interface ISettingsService
{
    /// <summary>設定ファイルのフルパス。</summary>
    string FilePath { get; }

    /// <summary>設定を読み込む。失敗した場合は既定値を返す。</summary>
    AppSettings Load();

    /// <summary>設定を保存する。失敗しても例外は投げない。</summary>
    void Save(AppSettings settings);
}
