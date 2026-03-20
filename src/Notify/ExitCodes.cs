namespace Notify;

/// <summary>Exit codes returned by the CLI. Documented in the planning doc.</summary>
internal static class ExitCodes
{
    internal const int Success              = 0;
    internal const int GeneralError         = 1;
    internal const int WebhookError         = 2;
    internal const int ConfigurationMissing = 5;
}
