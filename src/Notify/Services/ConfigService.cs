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
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    // Maps env var names to the AppConfig property they populate.
    private static readonly (string EnvKey, Action<AppConfig, string> Apply)[] EnvVarMap =
    [
        ("NOTIFY_TEAMS_TENANT_ID",      (c, v) => c.TenantId       = v),
        ("NOTIFY_TEAMS_CLIENT_ID",      (c, v) => c.ClientId       = v),
        ("NOTIFY_TEAMS_CLIENT_SECRET",  (c, v) => c.ClientSecret   = v),
        ("NOTIFY_TEAMS_DEFAULT_TEAM",   (c, v) => c.DefaultTeam    = v),
        ("NOTIFY_TEAMS_DEFAULT_CHANNEL",(c, v) => c.DefaultChannel = v),
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
                var saved = JsonSerializer.Deserialize<AppConfig>(json, JsonOptions);
                if (saved is not null)
                {
                    if (!string.IsNullOrWhiteSpace(saved.TenantId))      config.TenantId      = saved.TenantId;
                    if (!string.IsNullOrWhiteSpace(saved.ClientId))      config.ClientId      = saved.ClientId;
                    if (!string.IsNullOrWhiteSpace(saved.ClientSecret))  config.ClientSecret  = saved.ClientSecret;
                    if (!string.IsNullOrWhiteSpace(saved.DefaultTeam))   config.DefaultTeam   = saved.DefaultTeam;
                    if (!string.IsNullOrWhiteSpace(saved.DefaultChannel)) config.DefaultChannel = saved.DefaultChannel;
                }
            }
            catch (JsonException)
            {
                // Malformed config file — skip it and rely on other sources
            }
        }

        // Layer 2 — environment variables
        ApplySource(config, key => Environment.GetEnvironmentVariable(key));

        // Layer 3 — --env-file (highest priority)
        if (envFilePath is not null)
        {
            var envFileValues = await ParseEnvFileAsync(envFilePath);
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
        var json = JsonSerializer.Serialize(config, JsonOptions);
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
