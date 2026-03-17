using Microsoft.Extensions.Logging;
using Microsoft.Graph;
using Microsoft.Graph.Models;
using TeamsNotify.Core.Exceptions;
using TeamsNotify.Core.Internal;
using TeamsNotify.Core.Models;
using ChannelInfo = TeamsNotify.Core.Models.ChannelInfo;
using TeamInfo = TeamsNotify.Core.Models.TeamInfo;

namespace TeamsNotify.Core.Services;

/// <summary>
/// Wraps Microsoft Graph API calls for Teams messaging.
/// Responsible for sending messages, listing teams and channels, and resolving names to IDs.
/// </summary>
/// <remarks>
/// Obtain an instance by passing a <see cref="GraphServiceClient"/> built by <see cref="AuthService"/>.
/// All methods are async and accept an optional <see cref="CancellationToken"/>.
/// Graph API throttling (429) is handled automatically by the SDK via the <c>Retry-After</c> header.
/// </remarks>
public class GraphService
{
    private readonly GraphServiceClient _graphClient;
    private readonly ILogger<GraphService> _logger;

    /// <param name="graphClient">An authenticated Graph client, built by <see cref="AuthService"/>.</param>
    /// <param name="logger">Logger instance.</param>
    public GraphService(GraphServiceClient graphClient, ILogger<GraphService> logger)
    {
        _graphClient = graphClient;
        _logger = logger;
    }

    /// <summary>
    /// Sends a message to a Teams channel.
    /// </summary>
    /// <param name="request">
    /// The message to send. <see cref="SendMessageRequest.TeamId"/> and
    /// <see cref="SendMessageRequest.ChannelId"/> must be resolved GUIDs.
    /// Use <see cref="ResolveTeamIdAsync"/> and <see cref="ResolveChannelIdAsync"/> if you have names.
    /// </param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="request"/> is null.</exception>
    public async Task SendMessageAsync(SendMessageRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        _logger.LogDebug(
            "Sending message to team {TeamId}, channel {ChannelId}. Subject: {Subject}, IsHtml: {IsHtml}.",
            request.TeamId, request.ChannelId, request.Subject ?? "(none)", request.IsHtml);

        var message = new ChatMessage
        {
            Subject = request.Subject,
            Body = new ItemBody
            {
                Content = request.Body,
                ContentType = request.IsHtml ? BodyType.Html : BodyType.Text
            }
        };

        await _graphClient
            .Teams[request.TeamId]
            .Channels[request.ChannelId]
            .Messages
            .PostAsync(message, cancellationToken: cancellationToken);

        _logger.LogDebug("Message sent successfully.");
    }

    /// <summary>
    /// Resolves a team name or GUID to a team GUID.
    /// If <paramref name="nameOrId"/> is already a GUID it is returned unchanged.
    /// Otherwise all teams visible to the app are searched by display name (case-insensitive).
    /// </summary>
    /// <param name="nameOrId">A team display name or GUID.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>The team GUID.</returns>
    /// <exception cref="TeamsNotFoundException">Thrown when no team matches the supplied name.</exception>
    public async Task<string> ResolveTeamIdAsync(string nameOrId, CancellationToken cancellationToken = default)
    {
        if (GuidHelper.IsGuid(nameOrId))
        {
            _logger.LogDebug("Team value {Value} is a GUID — skipping lookup.", nameOrId);
            return nameOrId;
        }

        _logger.LogDebug("Resolving team name '{Name}' to ID.", nameOrId);

        var teams = await FetchAllTeamsAsync(cancellationToken);
        var match = teams.FirstOrDefault(t =>
            string.Equals(t.DisplayName, nameOrId, StringComparison.OrdinalIgnoreCase));

        if (match is null)
            throw new TeamsNotFoundException($"No team found with the name '{nameOrId}'.");

        _logger.LogDebug("Resolved team '{Name}' to ID {Id}.", nameOrId, match.Id);
        return match.Id!;
    }

    /// <summary>
    /// Resolves a channel name or GUID to a channel GUID within the specified team.
    /// If <paramref name="nameOrId"/> is already a GUID it is returned unchanged.
    /// Otherwise all channels in the team are searched by display name (case-insensitive).
    /// </summary>
    /// <param name="teamId">The GUID of the team containing the channel.</param>
    /// <param name="nameOrId">A channel display name or GUID.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>The channel GUID.</returns>
    /// <exception cref="TeamsNotFoundException">Thrown when no channel matches the supplied name.</exception>
    public async Task<string> ResolveChannelIdAsync(string teamId, string nameOrId, CancellationToken cancellationToken = default)
    {
        if (GuidHelper.IsGuid(nameOrId))
        {
            _logger.LogDebug("Channel value {Value} is a GUID — skipping lookup.", nameOrId);
            return nameOrId;
        }

        _logger.LogDebug("Resolving channel name '{Name}' in team {TeamId}.", nameOrId, teamId);

        var channels = await FetchAllChannelsAsync(teamId, cancellationToken);
        var match = channels.FirstOrDefault(c =>
            string.Equals(c.DisplayName, nameOrId, StringComparison.OrdinalIgnoreCase));

        if (match is null)
            throw new TeamsNotFoundException($"No channel found with the name '{nameOrId}' in team '{teamId}'.");

        _logger.LogDebug("Resolved channel '{Name}' to ID {Id}.", nameOrId, match.Id);
        return match.Id!;
    }

    /// <summary>
    /// Returns a summary of all Teams teams visible to the app registration.
    /// </summary>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>A read-only list of <see cref="TeamInfo"/> records.</returns>
    public async Task<IReadOnlyList<TeamInfo>> ListTeamsAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Listing all teams.");
        var teams = await FetchAllTeamsAsync(cancellationToken);

        return teams
            .Where(t => t.Id is not null && t.DisplayName is not null)
            .Select(t => new TeamInfo(t.Id!, t.DisplayName!))
            .OrderBy(t => t.DisplayName, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    /// <summary>
    /// Returns a summary of all channels within the specified team.
    /// </summary>
    /// <param name="teamId">The GUID of the team.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>A read-only list of <see cref="ChannelInfo"/> records.</returns>
    public async Task<IReadOnlyList<ChannelInfo>> ListChannelsAsync(string teamId, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Listing channels in team {TeamId}.", teamId);
        var channels = await FetchAllChannelsAsync(teamId, cancellationToken);

        return channels
            .Where(c => c.Id is not null && c.DisplayName is not null)
            .Select(c => new ChannelInfo(c.Id!, c.DisplayName!, c.MembershipType?.ToString() ?? "standard"))
            .OrderBy(c => c.DisplayName, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    // -------------------------------------------------------------------------
    // Private helpers
    // -------------------------------------------------------------------------

    private async Task<List<Team>> FetchAllTeamsAsync(CancellationToken cancellationToken)
    {
        var teams = new List<Team>();

        var page = await _graphClient.Teams.GetAsync(config =>
        {
            config.QueryParameters.Select = ["id", "displayName"];
        }, cancellationToken);

        if (page is null) return teams;

        var iterator = PageIterator<Team, TeamCollectionResponse>
            .CreatePageIterator(_graphClient, page, team =>
            {
                teams.Add(team);
                return true;
            });

        await iterator.IterateAsync(cancellationToken);
        return teams;
    }

    private async Task<List<Channel>> FetchAllChannelsAsync(string teamId, CancellationToken cancellationToken)
    {
        var channels = new List<Channel>();

        var page = await _graphClient.Teams[teamId].Channels.GetAsync(config =>
        {
            config.QueryParameters.Select = ["id", "displayName", "membershipType"];
        }, cancellationToken);

        if (page is null) return channels;

        var iterator = PageIterator<Channel, ChannelCollectionResponse>
            .CreatePageIterator(_graphClient, page, channel =>
            {
                channels.Add(channel);
                return true;
            });

        await iterator.IterateAsync(cancellationToken);
        return channels;
    }
}
