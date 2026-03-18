namespace Notify.Teams.Models;

/// <summary>
/// A lightweight summary of a Teams channel, returned by
/// <see cref="Services.GraphService.ListChannelsAsync"/>.
/// </summary>
public record ChannelInfo(string Id, string DisplayName, string MembershipType);
