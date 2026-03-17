namespace TeamsNotify.Core.Models;

/// <summary>
/// Describes a message to be sent to a Teams channel.
/// TeamId and ChannelId must be resolved GUIDs before calling GraphService.
/// </summary>
public record SendMessageRequest
{
    public required string TeamId { get; init; }
    public required string ChannelId { get; init; }
    public required string Body { get; init; }
    public bool IsHtml { get; init; }
    public string? Subject { get; init; }
}
