# notify

A .NET 10 CLI tool for sending Microsoft Teams messages via Power Automate webhooks — designed for CI/CD pipelines, cron jobs, and scripts. Runs on Linux, Windows, and macOS with no .NET runtime required.

## Install

**Self-contained binary** — download for your platform from the [releases page](https://github.com/EvilGeniusCore/Notify/releases) and place it on your PATH. No .NET runtime needed.

**dotnet global tool** — for machines that already have .NET installed:

```bash
dotnet tool install -g Notify
```

**NuGet library** — for .NET applications that want to send Teams messages without shelling out to the CLI:

```bash
dotnet add package Notify.Teams
```

## Quick start

```bash
# Set your webhook URL
export NOTIFY_TEAMS_WEBHOOK_URL=https://prod2-xx.region.logic.azure.com/...

# Send a message
notify send --message "Deployment complete"
```

The webhook URL comes from Teams → channel → **...** → **Workflows** → "Send webhook alerts to a channel". See [Getting a webhook URL](#getting-a-webhook-url) below.

## Usage

```bash
# Send a plain text message
notify send --message "Deployment complete"

# Send with a subject line
notify send --subject "Build #42 Failed" --message "Unit tests failed on main"

# Send an HTML formatted message
notify send --html --message "<b>Failed</b> — branch <code>main</code>"

# Send message body from a file
notify send --file ./message.txt

# Pipe stdin
echo "Build failed" | notify send

# Use a credentials file (useful for cron jobs)
notify send --env-file ./notify.env --message "Done"

# Override the webhook URL per invocation
notify send --webhook https://prod2-xx.region.logic.azure.com/... --message "Done"

# Send a MessageCard JSON template (with optional body appended)
notify send --template ./alert.json --subject "Deployment" --message "Extra context"

# Dry run — prints the resolved options without sending
notify send --message "test" --dry-run
```

## Configuration

Credentials are resolved in this order (highest priority first):

1. `--env-file <path>` — key=value file, overrides everything
2. Environment variables — set in the shell, crontab, or container
3. Config file — written by `notify configure`, stored in the platform-appropriate location

### Environment variables

```
NOTIFY_TEAMS_WEBHOOK_URL=<your-webhook-url>
```

### Typical patterns

| Scenario | Approach |
|---|---|
| CI/CD pipeline or container | Set `NOTIFY_TEAMS_WEBHOOK_URL` as a pipeline secret/env var |
| Cron job or script folder | `--env-file ./notify.env` alongside your script |
| Developer laptop | Run `notify configure --webhook-url <url>` once |

### Getting a webhook URL

Each channel you want to post to needs its own webhook URL. Any team member can create one.

1. Open the target channel in Teams
2. Click **...** → **Workflows**
3. Search for **"Send webhook alerts to a channel"** and select it
4. Give the workflow a name (e.g. `notify`) and click **Next**
5. Confirm the team and channel, then click **Add workflow**
6. Copy the generated URL — this is your `NOTIFY_TEAMS_WEBHOOK_URL`

The URL is a credential. Treat it like a password — store it in an env file or CI/CD secret, never in source control.

## Commands

| Command | Description |
|---|---|
| `send` | Send a message to a Teams channel |
| `configure` | Save the default webhook URL to the config file |
| `version` | Show version, runtime, and OS info |

## Send options

```
-m, --message <text>      Message body — required unless --file, --template, or stdin is used
-f, --file <path>         Read message body from a file
    --webhook <url>       Webhook URL — overrides NOTIFY_TEAMS_WEBHOOK_URL
    --subject <text>      Optional subject line shown as the card title
    --template <path>     Path to a MessageCard JSON template file
    --html                Treat message body as HTML
    --dry-run             Print the resolved options without sending
-q, --quiet               Suppress output; rely on exit code only

Global:
    --env-file <path>     Load credentials from a key=value file
```

## Using Notify.Teams in your .NET app

If you want to send Teams messages directly from a .NET application without the CLI:

```csharp
using Notify.Teams.Models;
using Notify.Teams.Services;
using Microsoft.Extensions.Logging.Abstractions;

var credentials = new WebhookCredentials
{
    WebhookUrl = Environment.GetEnvironmentVariable("NOTIFY_TEAMS_WEBHOOK_URL")!,
};

var service = new WebhookService(new HttpClient(), NullLogger<WebhookService>.Instance);

await service.SendMessageAsync(new SendMessageRequest
{
    Body    = "All tests passed. Version 2.1.0 deployed to production.",
    Subject = "Deployment complete",
}, credentials);
```

To send a pre-built MessageCard JSON template with an optional subject or body override:

```csharp
using System.Text.Json.Nodes;

var templateJson = await File.ReadAllTextAsync("alert-card.json");
var template     = JsonNode.Parse(templateJson)!.AsObject();

await service.SendFromTemplateAsync(template, subjectOverride: "Build Failed", bodyOverride: null, credentials);
```

## Exit codes

| Code | Meaning |
|---|---|
| `0` | Success |
| `1` | General error (including no message provided) |
| `2` | HTTP error from webhook — 4xx or 5xx response from Power Automate |
| `5` | Configuration missing — webhook URL not set |

## Licence

LGPL-3.0. Commercial use permitted. Modifications to this library must be shared back under LGPL. Applications that consume this library are not affected by the copyleft.
