using System;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using NightShade.Models;

namespace NightShade.Services;

/// <summary>
/// %APPDATA%\NightShade\settings.json に JSON で保存する実装。
/// </summary>
public sealed class JsonSettingsService : ISettingsService
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true
    };

    public JsonSettingsService()
    {
        var directory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "NightShade");

        FilePath = Path.Combine(directory, "settings.json");
    }

    public string FilePath { get; }

    public AppSettings Load()
    {
        try
        {
            if (!File.Exists(FilePath))
            {
                return new AppSettings();
            }

            var json = File.ReadAllText(FilePath);
            var settings = JsonSerializer.Deserialize<AppSettings>(json);
            return settings is null ? new AppSettings() : Normalize(settings);
        }
        catch (Exception ex)
        {
            // 設定が壊れていてもアプリは起動させる
            Debug.WriteLine($"[NightShade] 設定の読み込みに失敗しました: {ex.Message}");
            return new AppSettings();
        }
    }

    public void Save(AppSettings settings)
    {
        try
        {
            var directory = Path.GetDirectoryName(FilePath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            // 書き込み中の電源断などで設定ファイルが壊れないよう、一時ファイル経由で置き換える
            var tempPath = FilePath + ".tmp";
            File.WriteAllText(tempPath, JsonSerializer.Serialize(Normalize(settings), SerializerOptions));
            File.Move(tempPath, FilePath, overwrite: true);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[NightShade] 設定の保存に失敗しました: {ex.Message}");
        }
    }

    private static AppSettings Normalize(AppSettings settings)
    {
        settings.Opacity = Math.Clamp(settings.Opacity, 0.0, 0.9);
        settings.OpacityStep = Math.Clamp(settings.OpacityStep, 0.01, 0.25);
        return settings;
    }
}
