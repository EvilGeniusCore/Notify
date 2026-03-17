namespace TeamsNotify.Core.Models;

/// <summary>
/// Authentication credentials for an Entra ID App Registration.
/// Passed to <see cref="Services.AuthService"/> to build a <c>GraphServiceClient</c>.
/// </summary>
/// <remarks>
/// All three properties are required. Obtain these values from your Entra ID App Registration:
/// Azure Portal → Entra ID → App Registrations → your app → Overview (IDs) and Certificates &amp; Secrets.
/// The app registration must have the <c>ChannelMessage.Send</c> application permission with admin consent granted.
/// </remarks>
public record TeamsCredentials
{
    /// <summary>The Entra ID tenant GUID (Directory ID).</summary>
    public required string TenantId { get; init; }

    /// <summary>The App Registration client GUID (Application ID).</summary>
    public required string ClientId { get; init; }

    /// <summary>A client secret generated for the App Registration. Treat as a password.</summary>
    public required string ClientSecret { get; init; }
}
