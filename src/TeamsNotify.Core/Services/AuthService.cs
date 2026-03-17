using Microsoft.Extensions.Logging;
using Microsoft.Graph;
using TeamsNotify.Core.Models;

namespace TeamsNotify.Core.Services;

/// <summary>
/// Builds a GraphServiceClient from the supplied credentials.
/// Uses Client Credentials (app-only) via Azure.Identity.
/// </summary>
public class AuthService
{
    private readonly TeamsCredentials _credentials;
    private readonly ILogger<AuthService> _logger;

    public AuthService(TeamsCredentials credentials, ILogger<AuthService> logger)
    {
        _credentials = credentials;
        _logger = logger;
    }

    // TODO: BuildGraphClientAsync() -> GraphServiceClient
}
