using System.CommandLine;

namespace TeamsNotify.Commands;

/// <summary>
/// Saves default credentials and channel settings to the platform config file.
/// </summary>
public static class ConfigureCommand
{
    public static Command Build()
    {
        // TODO: define options and handler
        var command = new Command("configure", "Save default credentials and channel settings");
        return command;
    }
}
