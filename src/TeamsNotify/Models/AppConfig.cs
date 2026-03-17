using TeamsNotify.Core.Models;

namespace TeamsNotify.Models;

/// <summary>
/// Resolved configuration for the current CLI invocation.
/// Populated by ConfigService from --env-file, environment variables, or the platform config file.
/// Call ToCredentials() to extract the auth subset for TeamsNotify.Core services.
/// </summary>
public class AppConfig
{
    public string? TenantId { get; set; }
    public string? ClientId { get; set; }
    public string? ClientSecret { get; set; }
    public string? DefaultTeam { get; set; }
    public string? DefaultChannel { get; set; }

    /// <summary>
    /// Extracts auth credentials for passing to AuthService.
    /// Only call after validating that all three fields are non-null.
    /// </summary>
    public TeamsCredentials ToCredentials() => new()
    {
        TenantId = TenantId!,
        ClientId = ClientId!,
        ClientSecret = ClientSecret!,
    };
}
