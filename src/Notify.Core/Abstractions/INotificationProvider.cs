namespace Notify.Core.Abstractions;

/// <summary>
/// Marker interface for notification providers.
/// Each platform (Teams, Matrix, Discord, etc.) implements this interface.
///
/// NOTE: This abstraction is intentionally minimal. The project/package separation
/// (Notify.Core → Notify.Teams, Notify.Matrix, etc.) is scaffolded for future
/// multi-provider expansion. The send contract will be formalised when a second
/// provider is introduced — for now this serves as the architectural seam.
/// </summary>
public interface INotificationProvider
{
    /// <summary>
    /// A short identifier for this provider, e.g. "teams", "matrix", "discord".
    /// </summary>
    string ProviderId { get; }
}
