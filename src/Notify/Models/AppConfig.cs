using Notify.Teams.Models;

namespace Notify.Models;

/// <summary>
/// Resolved configuration for the current CLI invocation.
/// Populated by ConfigService from --env-file, environment variables, or the platform config file.
/// Call Validate() before ToCredentials() to ensure required fields are present.
/// </summary>
public class AppConfig
{
    public string? WebhookUrl { get; set; }

    /// <summary>
    /// Asserts that the webhook URL is present.
    /// Throws <see cref="InvalidOperationException"/> if missing.
    /// The CLI maps this to exit code 5.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when WebhookUrl is null or whitespace.</exception>
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(WebhookUrl))
            throw new InvalidOperationException(
                "Missing required configuration: NOTIFY_TEAMS_WEBHOOK_URL. " +
                "Set via --webhook, --env-file, environment variable, or 'notify configure'.");
    }

    /// <summary>
    /// Extracts webhook credentials. Only call after <see cref="Validate"/> has passed.
    /// </summary>
    public WebhookCredentials ToCredentials() => new() { WebhookUrl = WebhookUrl! };
}
