using System.CommandLine;

namespace TeamsNotify.Commands;

/// <summary>
/// Lists teams and channels available to the configured app registration.
/// No arguments: lists all teams.
/// --team <name|id>: lists channels within that team.
/// </summary>
public static class ListCommand
{
    public static Command Build()
    {
        // TODO: define options and handler
        var command = new Command("list", "List teams and channels available to the app");
        return command;
    }
}
