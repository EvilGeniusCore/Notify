using System.CommandLine;
using System.CommandLine.Invocation;
using Notify.Models;
using Notify.Services;

namespace Notify.Commands;

/// <summary>
/// Saves default credentials and channel settings to the platform config file.
/// Loads any existing config first so that only the supplied fields are updated.
/// </summary>
public static class ConfigureCommand
{
    public static Command Build()
    {
        var command = new Command("configure", "Save default credentials and channel settings");

        var tenantOption  = new Option<string?>("--tenant-id",        "Entra ID tenant GUID (Directory > Overview in Azure portal)")
            { ArgumentHelpName = "guid" };
        var clientOption  = new Option<string?>("--client-id",        "App Registration client GUID (App registrations > your app > Overview)")
            { ArgumentHelpName = "guid" };
        var secretOption  = new Option<string?>("--client-secret",    "App Registration client secret value (not the secret ID)")
            { ArgumentHelpName = "secret" };
        var teamOption    = new Option<string?>("--default-team",     "Default team name or GUID used when --team is not supplied")
            { ArgumentHelpName = "name|id" };
        var channelOption = new Option<string?>("--default-channel",  "Default channel name or GUID used when --channel is not supplied")
            { ArgumentHelpName = "name|id" };

        command.AddOption(tenantOption);
        command.AddOption(clientOption);
        command.AddOption(secretOption);
        command.AddOption(teamOption);
        command.AddOption(channelOption);

        command.SetHandler(async (InvocationContext context) =>
        {
            var tenantId       = context.ParseResult.GetValueForOption(tenantOption);
            var clientId       = context.ParseResult.GetValueForOption(clientOption);
            var clientSecret   = context.ParseResult.GetValueForOption(secretOption);
            var defaultTeam    = context.ParseResult.GetValueForOption(teamOption);
            var defaultChannel = context.ParseResult.GetValueForOption(channelOption);

            try
            {
                var configService = new ConfigService();

                // Load existing config so we only update what was supplied
                var config = await configService.LoadAsync(envFilePath: null);

                if (tenantId       is not null) config.TenantId       = tenantId;
                if (clientId       is not null) config.ClientId       = clientId;
                if (clientSecret   is not null) config.ClientSecret   = clientSecret;
                if (defaultTeam    is not null) config.DefaultTeam    = defaultTeam;
                if (defaultChannel is not null) config.DefaultChannel = defaultChannel;

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
