namespace TeamsNotify.Core.Models;

/// <summary>
/// Authentication credentials for an Entra ID App Registration.
/// Passed to AuthService to build a GraphServiceClient.
/// </summary>
public record TeamsCredentials
{
    public required string TenantId { get; init; }
    public required string ClientId { get; init; }
    public required string ClientSecret { get; init; }
}
