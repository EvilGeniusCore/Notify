using Azure.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Graph;
using Notify.Teams.Models;

namespace Notify.Teams.Services;

/// <summary>
/// Builds a <see cref="GraphServiceClient"/> from the supplied <see cref="TeamsCredentials"/>.
/// Uses Client Credentials (app-only) via <c>Azure.Identity</c>.
/// </summary>
/// <remarks>
/// The <see cref="GraphServiceClient"/> is constructed synchronously. Token acquisition is lazy —
/// the first Graph API call triggers the actual authentication request to Entra ID.
/// Ensure the App Registration has <c>ChannelMessage.Send</c> with admin consent before calling any Graph method.
/// </remarks>
public class AuthService
{
    private readonly TeamsCredentials _credentials;
    private readonly ILogger<AuthService> _logger;

    /// <param name="credentials">Entra ID App Registration credentials.</param>
    /// <param name="logger">Logger instance.</param>
    public AuthService(TeamsCredentials credentials, ILogger<AuthService> logger)
    {
        _credentials = credentials;
        _logger = logger;
    }

    /// <summary>
    /// Builds and returns a <see cref="GraphServiceClient"/> configured for app-only access
    /// using the credentials supplied at construction.
    /// </summary>
    /// <returns>A configured <see cref="GraphServiceClient"/> ready for Graph API calls.</returns>
    /// <exception cref="ArgumentException">
    /// Thrown if any credential field is null or whitespace.
    /// </exception>
    public GraphServiceClient BuildGraphClient()
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(_credentials.TenantId, nameof(TeamsCredentials.TenantId));
        ArgumentException.ThrowIfNullOrWhiteSpace(_credentials.ClientId, nameof(TeamsCredentials.ClientId));
        ArgumentException.ThrowIfNullOrWhiteSpace(_credentials.ClientSecret, nameof(TeamsCredentials.ClientSecret));

        _logger.LogDebug("Building GraphServiceClient for tenant {TenantId}.", _credentials.TenantId);

        var credential = new ClientSecretCredential(
            _credentials.TenantId,
            _credentials.ClientId,
            _credentials.ClientSecret);

        return new GraphServiceClient(credential, ["https://graph.microsoft.com/.default"]);
    }
}
