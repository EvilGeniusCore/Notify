namespace Notify.Teams.Models;

/// <summary>
/// Describes a message to be sent to a Teams channel.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="TeamId"/> and <see cref="ChannelId"/> must be resolved GUIDs before passing this record
/// to <see cref="Services.GraphService.SendMessageAsync"/>. Use
/// <see cref="Services.GraphService.ResolveTeamIdAsync"/> and
/// <see cref="Services.GraphService.ResolveChannelIdAsync"/> to resolve names to IDs first,
/// or pass GUIDs directly if already known.
/// </para>
/// <para>
/// When <see cref="IsHtml"/> is <c>true</c>, <see cref="Body"/> is sent with <c>contentType: html</c>.
/// Teams renders a specific HTML subset — see the HTML Specification in the planning documentation.
/// </para>
/// </remarks>
public record SendMessageRequest
{
    /// <summary>The GUID of the target team.</summary>
    public required string TeamId { get; init; }

    /// <summary>The GUID of the target channel within the team.</summary>
    public required string ChannelId { get; init; }

    /// <summary>The message body. Plain text by default; set <see cref="IsHtml"/> to send as HTML.</summary>
    public required string Body { get; init; }

    /// <summary>
    /// When <c>true</c>, <see cref="Body"/> is treated as HTML and sent with <c>contentType: html</c>.
    /// Defaults to <c>false</c> (plain text).
    /// </summary>
    public bool IsHtml { get; init; }

    /// <summary>
    /// Optional subject line displayed above the message body in Teams.
    /// Pass <c>null</c> to send without a subject.
    /// </summary>
    public string? Subject { get; init; }
}
