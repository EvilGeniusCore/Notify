using System.CommandLine;
using TeamsNotify.Commands;

var root = new RootCommand("teams-notify — send Microsoft Teams messages from the command line");

// TODO: add --env-file as a global option and wire into config resolution

root.AddCommand(SendCommand.Build());
root.AddCommand(ConfigureCommand.Build());
root.AddCommand(ListCommand.Build());
root.AddCommand(new Command("version", "Show version info"));

return await root.InvokeAsync(args);
