namespace Notify.Teams.Models;

/// <summary>
/// Webhook URL credential for posting to a Teams channel via Power Automate.
/// Obtain the URL from Teams → channel → ... → Workflows → "Send webhook alerts to channel".
/// Treat the URL as a secret — it grants anyone the ability to post to that channel.
/// </summary>
public record WebhookCredentials
{
    /// <summary>The Power Automate webhook URL for the target channel.</summary>
    public required string WebhookUrl { get; init; }
}
