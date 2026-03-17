using TeamsNotify.Core.Models;

namespace TeamsNotify.Services;

/// <summary>
/// Loads and saves application configuration for the CLI.
/// Resolves in order: --env-file -> environment variables -> platform config file.
/// Config file location resolved via Environment.GetFolderPath(SpecialFolder.ApplicationData).
/// Produces an AppConfig that can be passed directly to TeamsNotify.Core services.
/// </summary>
public class ConfigService
{
    // TODO: LoadAsync(string? envFilePath) -> AppConfig
    // TODO: SaveAsync(AppConfig config)
    // TODO: GetConfigFilePath() -> string
}
