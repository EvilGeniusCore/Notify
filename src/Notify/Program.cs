using System.CommandLine;
using System.Reflection;
using System.Runtime.InteropServices;
using Notify.Commands;

var envFileOption = new Option<string?>(
    "--env-file",
    "Load credentials from a KEY=VALUE file, overriding environment variables")
    { ArgumentHelpName = "path" };

var root = new RootCommand("""
    Send Microsoft Teams messages from scripts, CI/CD pipelines, and cron jobs.

    Quick start:
      1. Get a webhook URL:
           Teams > channel > ... > Workflows
           > "Send webhook alerts to a channel"
      2. Send a message:
           notify send --webhook <url> --message "Hello"
      3. To avoid passing --webhook every time, save it as the default:
           notify configure --webhook-url <url>
           or set NOTIFY_TEAMS_WEBHOOK_URL in your environment or a notify.env file.

    Source and docs: https://github.com/EvilGeniusCore/Notify
    """);

root.AddGlobalOption(envFileOption);

root.AddCommand(SendCommand.Build(envFileOption));
root.AddCommand(ConfigureCommand.Build());

var versionCommand = new Command("version", "Show version, runtime, and OS info");
versionCommand.SetHandler(() =>
{
    var version = Assembly.GetExecutingAssembly()
        .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
        ?.InformationalVersion ?? "unknown";

    Console.WriteLine($"notify {version}");
    Console.WriteLine($"runtime:     {RuntimeInformation.FrameworkDescription}");
    Console.WriteLine($"os:          {RuntimeInformation.OSDescription}");
});
root.AddCommand(versionCommand);

return await root.InvokeAsync(args);
