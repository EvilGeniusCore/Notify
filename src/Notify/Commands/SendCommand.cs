using System.CommandLine;
using System.CommandLine.Invocation;
using Azure.Identity;
using Microsoft.Graph.Models.ODataErrors;
using Notify.Teams.Exceptions;
using Notify.Teams.Models;
using Notify.Services;

namespace Notify.Commands;

/// <summary>
/// Sends a message to a Teams channel.
/// Supports --message, --file, and stdin as message sources.
/// </summary>
public static class SendCommand
{
    public static Command Build(Option<string?> envFileOption)
    {
        var command = new Command("send", "Send a message to a Teams channel");

        var messageOption = new Option<string?>(["-m", "--message"], "Message body (plain text or HTML with --html)")
            { ArgumentHelpName = "text" };
        var fileOption    = new Option<FileInfo?>(["-f", "--file"],   "Read message body from a file")
            { ArgumentHelpName = "path" };
        var teamOption    = new Option<string?>(["-t", "--team"],     "Target team name or GUID (overrides NOTIFY_TEAMS_DEFAULT_TEAM)")
            { ArgumentHelpName = "name|id" };
        var channelOption = new Option<string?>(["-c", "--channel"],  "Target channel name or GUID (overrides NOTIFY_TEAMS_DEFAULT_CHANNEL)")
            { ArgumentHelpName = "name|id" };
        var subjectOption = new Option<string?>("--subject",          "Optional subject line shown above the message body")
            { ArgumentHelpName = "text" };
        var htmlFlag      = new Option<bool>("--html",                "Treat message body as HTML (Teams HTML subset supported)");
        var dryRunFlag    = new Option<bool>("--dry-run",             "Print the resolved request without sending");
        var quietFlag     = new Option<bool>(["-q", "--quiet"],       "Suppress output; rely on exit code only");

        command.AddOption(messageOption);
        command.AddOption(fileOption);
        command.AddOption(teamOption);
        command.AddOption(channelOption);
        command.AddOption(subjectOption);
        command.AddOption(htmlFlag);
        command.AddOption(dryRunFlag);
        command.AddOption(quietFlag);

        command.SetHandler(async (InvocationContext context) =>
        {
            var envFile = context.ParseResult.GetValueForOption(envFileOption);
            var message = context.ParseResult.GetValueForOption(messageOption);
            var file    = context.ParseResult.GetValueForOption(fileOption);
            var team    = context.ParseResult.GetValueForOption(teamOption);
            var channel = context.ParseResult.GetValueForOption(channelOption);
            var subject = context.ParseResult.GetValueForOption(subjectOption);
            var isHtml  = context.ParseResult.GetValueForOption(htmlFlag);
            var dryRun  = context.ParseResult.GetValueForOption(dryRunFlag);
            var quiet   = context.ParseResult.GetValueForOption(quietFlag);

            try
            {
                // Resolve message body
                string? body;
                if (file is not null)
                    body = await File.ReadAllTextAsync(file.FullName);
                else if (message is not null)
                    body = message;
                else if (Console.IsInputRedirected)
                {
                    var stdin = await Console.In.ReadToEndAsync();
                    body = string.IsNullOrWhiteSpace(stdin) ? null : stdin;
                }
                else
                    body = null;

                if (body is null)
                {
                    Console.Error.WriteLine("error: no message provided — use --message, --file, or pipe via stdin.");
                    context.ExitCode = ExitCodes.GeneralError;
                    return;
                }

                // Load config
                var configService = new ConfigService();
                var config = await configService.LoadAsync(envFile);

                // Apply command-line team/channel over configured defaults
                var resolvedTeam    = team    ?? config.DefaultTeam;
                var resolvedChannel = channel ?? config.DefaultChannel;

                if (string.IsNullOrWhiteSpace(resolvedTeam))
                {
                    Console.Error.WriteLine("error: no team specified — use --team or set NOTIFY_TEAMS_DEFAULT_TEAM.");
                    context.ExitCode = ExitCodes.ConfigurationMissing;
                    return;
                }

                if (string.IsNullOrWhiteSpace(resolvedChannel))
                {
                    Console.Error.WriteLine("error: no channel specified — use --channel or set NOTIFY_TEAMS_DEFAULT_CHANNEL.");
                    context.ExitCode = ExitCodes.ConfigurationMissing;
                    return;
                }

                // Dry-run: print and exit without sending
                if (dryRun)
                {
                    Console.WriteLine($"[dry-run] team:    {resolvedTeam}");
                    Console.WriteLine($"[dry-run] channel: {resolvedChannel}");
                    if (subject is not null)
                        Console.WriteLine($"[dry-run] subject: {subject}");
                    Console.WriteLine($"[dry-run] html:    {isHtml.ToString().ToLowerInvariant()}");
                    Console.WriteLine($"[dry-run] body:");
                    Console.WriteLine(body);
                    return;
                }

                // Build services and resolve IDs
                config.Validate();
                var graphService = ServiceFactory.BuildGraphService(config);

                var teamId    = await graphService.ResolveTeamIdAsync(resolvedTeam, context.GetCancellationToken());
                var channelId = await graphService.ResolveChannelIdAsync(teamId, resolvedChannel, context.GetCancellationToken());

                var request = new SendMessageRequest
                {
                    TeamId    = teamId,
                    ChannelId = channelId,
                    Body      = body,
                    IsHtml    = isHtml,
                    Subject   = subject
                };

                await graphService.SendMessageAsync(request, context.GetCancellationToken());

                if (!quiet)
                    Console.WriteLine("Message sent.");
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
