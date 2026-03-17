namespace TeamsNotify.Core.Models;

/// <summary>
/// A lightweight summary of a Microsoft Teams team, returned by
/// <see cref="Services.GraphService.ListTeamsAsync"/>.
/// </summary>
public record TeamInfo(string Id, string DisplayName);
