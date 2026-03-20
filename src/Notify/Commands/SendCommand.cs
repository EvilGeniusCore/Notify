using System.CommandLine;
using System.CommandLine.Invocation;
using System.Text.Json.Nodes;
using Notify.Teams.Models;
using Notify.Services;

namespace Notify.Commands;

/// <summary>
/// Sends a message to a Teams channel via a Power Automate webhook URL.
/// Supports --message, --file, and stdin as message sources.
/// </summary>
public static class SendCommand
{
    public static Command Build(Option<string?> envFileOption)
    {
        var command = new Command("send", """
            Send a message to a Teams channel via a Power Automate webhook URL.

            Message sources (one required): --message, --file, --template, or piped stdin.
            Webhook URL (one required):     --webhook, NOTIFY_TEAMS_WEBHOOK_URL env var,
                                            notify.env file in current directory,
                                            or saved default from 'notify configure'.
            """);

        var messageOption = new Option<string?>(["-m", "--message"], "Message body (plain text or HTML with --html)")
            { ArgumentHelpName = "text" };
        var fileOption    = new Option<FileInfo?>(["-f", "--file"],   "Read message body from a file")
            { ArgumentHelpName = "path" };
        var webhookOption = new Option<string?>("--webhook",          "Webhook URL — overrides NOTIFY_TEAMS_WEBHOOK_URL")
            { ArgumentHelpName = "url" };
        var subjectOption = new Option<string?>("--subject",          "Optional subject line shown above the message body")
            { ArgumentHelpName = "text" };
        var templateOption = new Option<FileInfo?>("--template",       "Path to a provider-specific MessageCard JSON template file")
            { ArgumentHelpName = "path" };
        var htmlFlag      = new Option<bool>("--html",                "Treat message body as HTML (Teams HTML subset supported)");
        var dryRunFlag    = new Option<bool>("--dry-run",             "Print the payload without sending");
        var quietFlag     = new Option<bool>(["-q", "--quiet"],       "Suppress output; rely on exit code only");

        command.AddOption(messageOption);
        command.AddOption(fileOption);
        command.AddOption(webhookOption);
        command.AddOption(subjectOption);
        command.AddOption(templateOption);
        command.AddOption(htmlFlag);
        command.AddOption(dryRunFlag);
        command.AddOption(quietFlag);

        command.SetHandler(async (InvocationContext context) =>
        {
            var envFile  = context.ParseResult.GetValueForOption(envFileOption);
            var message  = context.ParseResult.GetValueForOption(messageOption);
            var file     = context.ParseResult.GetValueForOption(fileOption);
            var webhook  = context.ParseResult.GetValueForOption(webhookOption);
            var subject  = context.ParseResult.GetValueForOption(subjectOption);
            var template = context.ParseResult.GetValueForOption(templateOption);
            var isHtml   = context.ParseResult.GetValueForOption(htmlFlag);
            var dryRun   = context.ParseResult.GetValueForOption(dryRunFlag);
            var quiet    = context.ParseResult.GetValueForOption(quietFlag);

            try
            {
                // Resolve optional message body
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

                // Body is required unless a template is supplying the card content
                if (body is null && template is null)
                {
                    Console.Error.WriteLine("error: no message provided — use --message, --file, --template, or pipe via stdin.");
                    context.ExitCode = ExitCodes.GeneralError;
                    return;
                }

                // Load config and apply --webhook override
                var configService = new ConfigService();
                var config = await configService.LoadAsync(envFile);

                if (webhook is not null)
                    config.WebhookUrl = webhook;

                // Dry-run: print resolved options and exit without sending
                if (dryRun)
                {
                    Console.WriteLine($"[dry-run] webhook:  {config.WebhookUrl ?? "(not set)"}");
                    if (template is not null)
                        Console.WriteLine($"[dry-run] template: {template.FullName}");
                    if (subject is not null)
                        Console.WriteLine($"[dry-run] subject:  {subject}");
                    if (body is not null)
                    {
                        Console.WriteLine($"[dry-run] html:     {isHtml.ToString().ToLowerInvariant()}");
                        Console.WriteLine($"[dry-run] body:");
                        Console.WriteLine(body);
                    }
                    return;
                }

                config.Validate();
                var webhookService = ServiceFactory.BuildWebhookService(config);
                var credentials    = config.ToCredentials();

                if (template is not null)
                {
                    var json = await File.ReadAllTextAsync(template.FullName);
                    var card = JsonNode.Parse(json)?.AsObject()
                        ?? throw new InvalidOperationException($"Template is not a valid JSON object: {template.FullName}");

                    await webhookService.SendFromTemplateAsync(card, subject, body, credentials, context.GetCancellationToken());
                }
                else
                {
                    var request = new SendMessageRequest
                    {
                        Body    = body!,
                        IsHtml  = isHtml,
                        Subject = subject,
                    };

                    await webhookService.SendMessageAsync(request, credentials, context.GetCancellationToken());
                }

                if (!quiet)
                    Console.WriteLine("Message sent.");
            }
            catch (InvalidOperationException ex)
            {
                Console.Error.WriteLine($"error: {ex.Message}");
                context.ExitCode = ExitCodes.ConfigurationMissing;
            }
            catch (HttpRequestException ex)
            {
                Console.Error.WriteLine($"error: webhook request failed — {ex.Message}");
                context.ExitCode = ExitCodes.WebhookError;
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
