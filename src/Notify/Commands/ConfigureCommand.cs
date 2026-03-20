using System.CommandLine;
using System.CommandLine.Invocation;
using Notify.Models;
using Notify.Services;

namespace Notify.Commands;

/// <summary>
/// Saves the default webhook URL to the platform config file.
/// </summary>
public static class ConfigureCommand
{
    public static Command Build()
    {
        var command = new Command("configure", """
            Save the default webhook URL to the platform config file.

            Run once on a developer machine so you don't need --webhook or
            NOTIFY_TEAMS_WEBHOOK_URL on every invocation. For CI/CD and scripts,
            use NOTIFY_TEAMS_WEBHOOK_URL or --env-file instead.

            Config file location:
              Windows:      %APPDATA%\notify\config.json
              Linux/macOS:  ~/.config/notify/config.json
            """);

        var webhookOption = new Option<string?>("--webhook-url", "Power Automate webhook URL for the target channel")
            { ArgumentHelpName = "url" };

        command.AddOption(webhookOption);

        command.SetHandler(async (InvocationContext context) =>
        {
            var webhookUrl = context.ParseResult.GetValueForOption(webhookOption);

            try
            {
                var configService = new ConfigService();
                var config = await configService.LoadAsync(envFilePath: null);

                if (webhookUrl is not null) config.WebhookUrl = webhookUrl;

                await configService.SaveAsync(config);

                var path = configService.GetConfigFilePath();
                Console.WriteLine($"Configuration saved to {path}");
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"error: {ex.Message}");
                context.ExitCode = ExitCodes.GeneralError;
            }
        });

        return command;
    }
}
