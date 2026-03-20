namespace Notify.Teams.Models;

/// <summary>
/// Describes a message to be sent to a Teams channel via webhook.
/// </summary>
public record SendMessageRequest
{
    /// <summary>The message body. Plain text by default; set <see cref="IsHtml"/> to send as HTML.</summary>
    public required string Body { get; init; }

    /// <summary>
    /// When <c>true</c>, <see cref="Body"/> should be treated as HTML by the provider.
    /// Defaults to <c>false</c> (plain text / markdown).
    ///
    /// NOTE: The Teams webhook provider (MessageCard via Power Automate) ignores this flag —
    /// MessageCard sections always render with markdown enabled and do not have a distinct HTML mode.
    /// This property is retained for future providers or service implementations that do
    /// differentiate between HTML and plain-text inputs.
    /// </summary>
    public bool IsHtml { get; init; }

    /// <summary>
    /// Optional subject line displayed as the card title in Teams.
    /// Pass <c>null</c> to send without a title.
    /// </summary>
    public string? Subject { get; init; }
}
