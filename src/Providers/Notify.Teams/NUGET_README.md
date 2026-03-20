# Notify.Teams

Send Microsoft Teams channel messages from any .NET application via Power Automate webhooks. No app registration or admin consent required — just a webhook URL from the Teams Workflows feature.

```bash
dotnet add package Notify.Teams
```

## Quick start

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
    Body    = "Deployment complete",
    Subject = "Build #42",
}, credentials);
```

## Getting a webhook URL

Each channel needs its own URL. Any team member can create one — no admin required.

1. Open the target channel in Teams
2. Click **...** → **Workflows**
3. Search for **"Send webhook alerts to a channel"** and select it
4. Name it (e.g. `notify`), confirm the channel, click **Add workflow**
5. Copy the generated URL

Treat the URL as a secret — it grants anyone the ability to post to that channel.

## Sending with a subject line

```csharp
await service.SendMessageAsync(new SendMessageRequest
{
    Subject = "Build #42 Failed",
    Body    = "Unit tests failed on main.",
}, credentials);
```

## Sending from a MessageCard template

Use `SendFromTemplateAsync` to post a pre-built MessageCard JSON payload with optional overrides:

```csharp
using System.Text.Json.Nodes;

var json     = await File.ReadAllTextAsync("alert-card.json");
var template = JsonNode.Parse(json)!.AsObject();

await service.SendFromTemplateAsync(
    template,
    subjectOverride: "Deployment failed",
    bodyOverride:    "Branch: main — see pipeline for details",
    credentials);
```

If `subjectOverride` is provided it replaces the `title` and `summary` fields in the template. If `bodyOverride` is provided it is appended as a new markdown section.

## Source

Source code and full documentation: [EvilGeniusCore/Notify](https://github.com/EvilGeniusCore/Notify).

The `notify` CLI tool in that repository is built entirely on `Notify.Teams` and covers credential loading, command parsing, exit code handling, and dry-run support — a useful reference for integrating the library into your own application.

## Licence

LGPL-3.0. Commercial use permitted. Modifications to this library must be shared back under LGPL. Applications that consume this library are not affected by the copyleft.
