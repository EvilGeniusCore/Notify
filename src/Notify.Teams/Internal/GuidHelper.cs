namespace Notify.Teams.Internal;

/// <summary>
/// Utility for detecting whether a string is a GUID, used when resolving
/// team and channel name-or-ID arguments.
/// </summary>
internal static class GuidHelper
{
    /// <summary>
    /// Returns <c>true</c> if <paramref name="value"/> is a valid GUID in any standard format.
    /// </summary>
    internal static bool IsGuid(string value) => Guid.TryParse(value, out _);
}
