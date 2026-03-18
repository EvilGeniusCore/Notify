namespace Notify.Teams.Exceptions;

/// <summary>
/// Thrown when a team or channel cannot be found by the supplied name or ID.
/// </summary>
/// <remarks>
/// This exception is raised by <see cref="Services.GraphService.ResolveTeamIdAsync"/> and
/// <see cref="Services.GraphService.ResolveChannelIdAsync"/> when the lookup returns no match.
/// The CLI maps this exception to exit code <c>3</c>.
/// </remarks>
public class TeamsNotFoundException : Exception
{
    /// <summary>Initialises a new instance with a descriptive message.</summary>
    public TeamsNotFoundException(string message) : base(message) { }

    /// <summary>Initialises a new instance with a descriptive message and inner exception.</summary>
    public TeamsNotFoundException(string message, Exception inner) : base(message, inner) { }
}
