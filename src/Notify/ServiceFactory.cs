using Microsoft.Extensions.Logging.Abstractions;
using Notify.Teams.Services;
using Notify.Models;

namespace Notify;

/// <summary>
/// Builds Notify.Teams services from a resolved AppConfig.
/// Keeps command handlers free of wiring boilerplate.
/// </summary>
internal static class ServiceFactory
{
    /// <summary>
    /// Validates <paramref name="config"/> and returns a <see cref="WebhookService"/> ready for use.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Re-thrown from <see cref="AppConfig.Validate"/> when webhook URL is missing.
    /// </exception>
    internal static WebhookService BuildWebhookService(AppConfig config)
    {
        config.Validate();
        return new WebhookService(new HttpClient(), NullLogger<WebhookService>.Instance);
    }
}
