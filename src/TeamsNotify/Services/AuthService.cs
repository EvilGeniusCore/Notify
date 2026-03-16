namespace TeamsNotify.Services;

/// <summary>
/// Resolves credentials and builds the authentication provider for Microsoft.Graph.
/// Uses Client Credentials (app-only) via Azure.Identity.
/// Credential resolution order: --env-file -> environment variables -> config file.
/// </summary>
public class AuthService
{
    // TODO: inject AppConfig and ILogger

    // TODO: BuildGraphClientAsync() -> GraphServiceClient
}
