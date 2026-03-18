namespace Notify.Core.Abstractions;

/// <summary>
/// Marker interface for notification providers.
/// Each platform (Teams, Matrix, Discord, etc.) implements this interface.
/// The send interface will be formalised when a second provider is introduced.
/// </summary>
public interface INotificationProvider
{
    /// <summary>
    /// A short identifier for this provider, e.g. "teams", "matrix", "discord".
    /// </summary>
    string ProviderId { get; }
}
