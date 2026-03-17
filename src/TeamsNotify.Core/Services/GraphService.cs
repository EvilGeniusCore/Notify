using Microsoft.Extensions.Logging;
using Microsoft.Graph;
using TeamsNotify.Core.Models;

namespace TeamsNotify.Core.Services;

/// <summary>
/// Wraps Microsoft.Graph API calls for Teams messaging.
/// Responsible for sending messages, listing teams/channels, and resolving names to IDs.
/// </summary>
public class GraphService
{
    private readonly GraphServiceClient _graphClient;
    private readonly ILogger<GraphService> _logger;

    public GraphService(GraphServiceClient graphClient, ILogger<GraphService> logger)
    {
        _graphClient = graphClient;
        _logger = logger;
    }

    // TODO: SendMessageAsync(SendMessageRequest request)
    // TODO: ResolveTeamIdAsync(string nameOrId) -> string
    // TODO: ResolveChannelIdAsync(string teamId, string nameOrId) -> string
    // TODO: ListTeamsAsync() -> IReadOnlyList<TeamSummary>
    // TODO: ListChannelsAsync(string teamId) -> IReadOnlyList<ChannelSummary>
}
