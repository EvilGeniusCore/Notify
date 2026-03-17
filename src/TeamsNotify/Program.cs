using System.CommandLine;
using System.Reflection;
using System.Runtime.InteropServices;
using TeamsNotify.Commands;

var envFileOption = new Option<string?>(
    "--env-file",
    "Load credentials and defaults from a KEY=VALUE file, overriding environment variables")
    { ArgumentHelpName = "path" };

var root = new RootCommand("teams-notify — send Microsoft Teams messages from the command line");
root.AddGlobalOption(envFileOption);

root.AddCommand(SendCommand.Build(envFileOption));
root.AddCommand(ConfigureCommand.Build());
root.AddCommand(ListCommand.Build(envFileOption));

var versionCommand = new Command("version", "Show version info");
versionCommand.SetHandler(() =>
{
    var version = Assembly.GetExecutingAssembly()
        .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
        ?.InformationalVersion ?? "unknown";

    Console.WriteLine($"teams-notify {version}");
    Console.WriteLine($"runtime:     {RuntimeInformation.FrameworkDescription}");
    Console.WriteLine($"os:          {RuntimeInformation.OSDescription}");
});
root.AddCommand(versionCommand);

return await root.InvokeAsync(args);
