namespace Notify;

/// <summary>Exit codes returned by the CLI. Documented in the planning doc.</summary>
internal static class ExitCodes
{
    internal const int Success              = 0;
    internal const int GeneralError         = 1;
    internal const int AuthFailure          = 2;
    internal const int NotFound             = 3;
    internal const int GraphApiError        = 4;
    internal const int ConfigurationMissing = 5;
}
