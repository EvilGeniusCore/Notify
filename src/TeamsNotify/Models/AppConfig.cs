using TeamsNotify.Core.Models;

namespace TeamsNotify.Models;

/// <summary>
/// Resolved configuration for the current CLI invocation.
/// Populated by ConfigService from --env-file, environment variables, or the platform config file.
/// Call Validate() before ToCredentials() to ensure required fields are present.
/// </summary>
public class AppConfig
{
    public string? TenantId { get; set; }
    public string? ClientId { get; set; }
    public string? ClientSecret { get; set; }
    public string? DefaultTeam { get; set; }
    public string? DefaultChannel { get; set; }

    /// <summary>
    /// Asserts that all required credential fields are present.
    /// Throws <see cref="InvalidOperationException"/> listing every missing field.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown when one or more of TenantId, ClientId, or ClientSecret are null or whitespace.
    /// The CLI maps this to exit code 5.
    /// </exception>
    public void Validate()
    {
        var missing = new List<string>();
        if (string.IsNullOrWhiteSpace(TenantId))     missing.Add("TEAMS_NOTIFY_TENANT_ID");
        if (string.IsNullOrWhiteSpace(ClientId))     missing.Add("TEAMS_NOTIFY_CLIENT_ID");
        if (string.IsNullOrWhiteSpace(ClientSecret)) missing.Add("TEAMS_NOTIFY_CLIENT_SECRET");

        if (missing.Count > 0)
            throw new InvalidOperationException(
                $"Missing required configuration: {string.Join(", ", missing)}. " +
                "Set via --env-file, environment variables, or 'teams-notify configure'.");
    }

    /// <summary>
    /// Extracts auth credentials for passing to AuthService.
    /// Only call after <see cref="Validate"/> has passed.
    /// </summary>
    public TeamsCredentials ToCredentials() => new()
    {
        TenantId     = TenantId!,
        ClientId     = ClientId!,
        ClientSecret = ClientSecret!,
    };
}
