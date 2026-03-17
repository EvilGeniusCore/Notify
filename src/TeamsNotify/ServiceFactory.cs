using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using TeamsNotify.Core.Services;
using TeamsNotify.Models;

namespace TeamsNotify;

/// <summary>
/// Builds TeamsNotify.Core services from a resolved AppConfig.
/// Keeps command handlers free of wiring boilerplate.
/// </summary>
internal static class ServiceFactory
{
    /// <summary>
    /// Validates <paramref name="config"/>, builds an authenticated
    /// <see cref="GraphService"/>, and returns it ready for use.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Re-thrown from <see cref="AppConfig.Validate"/> when credentials are missing.
    /// </exception>
    internal static GraphService BuildGraphService(AppConfig config)
    {
        config.Validate();

        var auth  = new AuthService(config.ToCredentials(), NullLogger<AuthService>.Instance);
        var client = auth.BuildGraphClient();

        return new GraphService(client, NullLogger<GraphService>.Instance);
    }
}
