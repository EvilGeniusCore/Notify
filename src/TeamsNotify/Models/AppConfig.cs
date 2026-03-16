namespace TeamsNotify.Models;

/// <summary>
/// Holds resolved credentials and defaults for the current invocation.
/// Populated by ConfigService from --env-file, environment variables, or the platform config file.
/// </summary>
public class AppConfig
{
    public string? TenantId { get; set; }
    public string? ClientId { get; set; }
    public string? ClientSecret { get; set; }
    public string? DefaultTeam { get; set; }
    public string? DefaultChannel { get; set; }
}
