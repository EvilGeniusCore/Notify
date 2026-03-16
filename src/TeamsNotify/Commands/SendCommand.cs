using System.CommandLine;

namespace TeamsNotify.Commands;

/// <summary>
/// Sends a message to a Teams channel or user.
/// Supports --message, --file, and stdin as message sources.
/// </summary>
public static class SendCommand
{
    public static Command Build()
    {
        // TODO: define options and handler
        var command = new Command("send", "Send a message to a Teams channel or user");
        return command;
    }
}
