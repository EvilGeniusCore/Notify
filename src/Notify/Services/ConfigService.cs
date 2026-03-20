using System.Text.Json;
using System.Text.Json.Serialization;
using Notify.Models;

namespace Notify.Services;

/// <summary>
/// Loads and saves application configuration for the CLI.
/// Resolution order (highest to lowest priority): --env-file → environment variables → platform config file.
/// </summary>
public class ConfigService
{
    // Maps env var names to the AppConfig property they populate.
    private static readonly (string EnvKey, Action<AppConfig, string> Apply)[] EnvVarMap =
    [
        ("NOTIFY_TEAMS_WEBHOOK_URL", (c, v) => c.WebhookUrl = v),
    ];

    /// <summary>
    /// Loads configuration from all available sources in priority order.
    /// Each source overlays the previous — higher-priority values win.
    /// </summary>
    /// <param name="envFilePath">
    /// Path to a KEY=VALUE file passed via --env-file.
    /// When provided, values in this file override environment variables.
    /// </param>
    public async Task<AppConfig> LoadAsync(string? envFilePath)
    {
        var config = new AppConfig();

        // Layer 1 — platform config file (lowest priority)
        var configFilePath = GetConfigFilePath();
        if (File.Exists(configFilePath))
        {
            try
            {
                var json = await File.ReadAllTextAsync(configFilePath);
                var saved = JsonSerializer.Deserialize(json, AppConfigJsonContext.Default.AppConfig);
                if (saved is not null)
                {
                    if (!string.IsNullOrWhiteSpace(saved.WebhookUrl)) config.WebhookUrl = saved.WebhookUrl;
                }
            }
            catch (JsonException)
            {
                // Malformed config file — skip it and rely on other sources
            }
        }

        // Layer 2 — environment variables
        ApplySource(config, key => Environment.GetEnvironmentVariable(key));

        // Layer 3 — env file (highest priority)
        // Explicit --env-file takes precedence; fall back to notify.env in the current directory.
        var resolvedEnvFile = envFilePath
            ?? (File.Exists("notify.env") ? "notify.env" : null);

        if (resolvedEnvFile is not null)
        {
            var envFileValues = await ParseEnvFileAsync(resolvedEnvFile);
            ApplySource(config, key => envFileValues.GetValueOrDefault(key));
        }

        return config;
    }

    /// <summary>
    /// Saves <paramref name="config"/> to the platform config file.
    /// Creates the directory if it does not exist.
    /// </summary>
    public async Task SaveAsync(AppConfig config)
    {
        var path = GetConfigFilePath();
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var json = JsonSerializer.Serialize(config, AppConfigJsonContext.Default.AppConfig);
        await File.WriteAllTextAsync(path, json);
    }

    /// <summary>
    /// Returns the platform-appropriate path for the config file.
    /// Windows: %APPDATA%\notify\config.json
    /// Linux / macOS: ~/.config/notify/config.json
    /// </summary>
    public virtual string GetConfigFilePath()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        return Path.Combine(appData, "notify", "config.json");
    }

    // -------------------------------------------------------------------------
    // Private helpers
    // -------------------------------------------------------------------------

    private static void ApplySource(AppConfig config, Func<string, string?> getValue)
    {
        foreach (var (envKey, apply) in EnvVarMap)
        {
            var value = getValue(envKey);
            if (!string.IsNullOrWhiteSpace(value))
                apply(config, value);
        }
    }

    private static async Task<Dictionary<string, string>> ParseEnvFileAsync(string path)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var line in await File.ReadAllLinesAsync(path))
        {
            var trimmed = line.Trim();
            if (trimmed.StartsWith('#') || !trimmed.Contains('=')) continue;

            var idx   = trimmed.IndexOf('=');
            var key   = trimmed[..idx].Trim();
            var value = trimmed[(idx + 1)..].Trim();

            if (!string.IsNullOrEmpty(key) && !string.IsNullOrEmpty(value))
                result[key] = value;
        }

        return result;
    }
}

[JsonSerializable(typeof(AppConfig))]
[JsonSourceGenerationOptions(WriteIndented = true, DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
internal partial class AppConfigJsonContext : JsonSerializerContext { }
