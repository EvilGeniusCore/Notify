namespace TeamsNotify.Services;

/// <summary>
/// Wraps Microsoft.Graph API calls.
/// Responsible for sending messages, listing teams, and resolving team/channel names to IDs.
/// </summary>
public class GraphService
{
    // TODO: inject GraphServiceClient and ILogger

    // TODO: SendMessageAsync(string teamId, string channelId, string body, bool isHtml, string? subject)
    // TODO: ResolveTeamIdAsync(string nameOrId)
    // TODO: ResolveChannelIdAsync(string teamId, string nameOrId)
    // TODO: ListTeamsAsync()
    // TODO: ListChannelsAsync(string teamId)
}
