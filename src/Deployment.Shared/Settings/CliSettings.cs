using System.Text.Json;
using Deployment.CLI.Localization;

namespace Deployment.CLI.Settings;

public class CliSettings
{
    public string? Language { get; set; }
}

public static class CliSettingsStore
{
    private static string GetPath(string dataDir) => Path.Combine(dataDir, "cli-settings.json");

    public static CliSettings Load(string dataDir)
    {
        var path = GetPath(dataDir);
        if (!File.Exists(path)) return new CliSettings();

        try
        {
            var json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<CliSettings>(json) ?? new CliSettings();
        }
        catch
        {
            return new CliSettings();
        }
    }

    public static void Save(string dataDir, CliSettings settings)
    {
        Directory.CreateDirectory(dataDir);
        var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(GetPath(dataDir), json);
    }

    public static void SaveLanguage(string dataDir, Language language)
    {
        var settings = Load(dataDir);
        settings.Language = language.ToCode();
        Save(dataDir, settings);
    }
}
