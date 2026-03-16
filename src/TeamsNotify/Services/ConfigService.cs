namespace TeamsNotify.Services;

/// <summary>
/// Loads and saves application configuration.
/// Resolves in order: --env-file -> environment variables -> platform config file.
/// Config file location resolved via Environment.GetFolderPath(SpecialFolder.ApplicationData).
/// </summary>
public class ConfigService
{
    // TODO: LoadAsync(string? envFilePath) -> AppConfig
    // TODO: SaveAsync(AppConfig config)
    // TODO: GetConfigFilePath() -> string
}
