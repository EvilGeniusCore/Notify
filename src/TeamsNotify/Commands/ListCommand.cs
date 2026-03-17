using System.CommandLine;
using System.CommandLine.Invocation;
using Azure.Identity;
using Microsoft.Graph.Models.ODataErrors;
using TeamsNotify.Core.Exceptions;
using TeamsNotify.Services;

namespace TeamsNotify.Commands;

/// <summary>
/// Lists teams and channels available to the configured app registration.
/// No --team argument: lists all teams.
/// --team provided: lists channels within that team.
/// </summary>
public static class ListCommand
{
    public static Command Build(Option<string?> envFileOption)
    {
        var command = new Command("list", "List teams and channels available to the app");

        var teamOption = new Option<string?>(["-t", "--team"], "List channels within this team; omit to list all teams")
            { ArgumentHelpName = "name|id" };
        command.AddOption(teamOption);

        command.SetHandler(async (InvocationContext context) =>
        {
            var envFile = context.ParseResult.GetValueForOption(envFileOption);
            var team    = context.ParseResult.GetValueForOption(teamOption);

            try
            {
                var configService = new ConfigService();
                var config = await configService.LoadAsync(envFile);
                config.Validate();

                var graphService = ServiceFactory.BuildGraphService(config);

                if (team is null)
                {
                    var teams = await graphService.ListTeamsAsync(context.GetCancellationToken());

                    if (teams.Count == 0)
                    {
                        Console.WriteLine("No teams found. Ensure the app has been added to at least one team.");
                        return;
                    }

                    Console.WriteLine($"{"ID",-40} {"Display Name"}");
                    Console.WriteLine(new string('-', 72));
                    foreach (var t in teams)
                        Console.WriteLine($"{t.Id,-40} {t.DisplayName}");
                }
                else
                {
                    var teamId   = await graphService.ResolveTeamIdAsync(team, context.GetCancellationToken());
                    var channels = await graphService.ListChannelsAsync(teamId, context.GetCancellationToken());

                    if (channels.Count == 0)
                    {
                        Console.WriteLine($"No channels found in team '{team}'.");
                        return;
                    }

                    Console.WriteLine($"{"ID",-40} {"Type",-12} {"Display Name"}");
                    Console.WriteLine(new string('-', 80));
                    foreach (var c in channels)
                        Console.WriteLine($"{c.Id,-40} {c.MembershipType,-12} {c.DisplayName}");
                }
            }
            catch (InvalidOperationException ex)
            {
                Console.Error.WriteLine($"error: {ex.Message}");
                context.ExitCode = ExitCodes.ConfigurationMissing;
            }
            catch (TeamsNotFoundException ex)
            {
                Console.Error.WriteLine($"error: {ex.Message}");
                context.ExitCode = ExitCodes.NotFound;
            }
            catch (AuthenticationFailedException ex)
            {
                Console.Error.WriteLine($"error: authentication failed — {ex.Message}");
                context.ExitCode = ExitCodes.AuthFailure;
            }
            catch (ODataError ex)
            {
                Console.Error.WriteLine($"error: graph api error — {ex.Error?.Message ?? ex.Message}");
                context.ExitCode = ExitCodes.GraphApiError;
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
