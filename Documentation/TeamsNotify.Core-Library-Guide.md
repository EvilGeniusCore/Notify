# TeamsNotify.Core — Developer Guide
> For .NET developers who want to send Teams messages directly from their application without shelling out to the CLI.

## Overview

`TeamsNotify.Core` is a .NET library that wraps the Microsoft Graph API for sending messages to Microsoft Teams channels. It handles authentication via Client Credentials (app-only), team and channel resolution by name or GUID, and message delivery.

Use this library if:
- You are building a .NET application that needs to send Teams notifications programmatically
- You want typed, awaitable API calls rather than shelling out to the `teams-notify` CLI
- You need to integrate Teams messaging into a larger service, background worker, or pipeline

## Requirements

- .NET 10 or later
- An Entra ID App Registration with:
  - `ChannelMessage.Send` application permission (admin consented)
  - A client secret

See [Setting Up an Entra ID App Registration](#setting-up-an-entra-id-app-registration) below.

## Installation

```bash
dotnet add package TeamsNotify.Core
```

## Setting Up an Entra ID App Registration

1. Go to [portal.azure.com](https://portal.azure.com) and sign in with your organisation account
2. Search for **App registrations** in the top search bar and click **New registration**
3. Give the app a name (e.g. `teams-notify`), leave all other defaults, click **Register**
4. From the **Overview** page, copy:
   - **Application (client) ID** → your `ClientId`
   - **Directory (tenant) ID** → your `TenantId`
5. Go to **Certificates & secrets** → **New client secret** → set an expiry → click **Add**. Copy the **Value** immediately — it is only shown once
6. Go to **API permissions** → **Add a permission** → **Microsoft Graph** → **Application permissions** → search for `ChannelMessage.Send` → add it
7. Click **Grant admin consent for [your organisation]** — a Global Admin must do this step

> **Note:** `ChannelMessage.Send` is a protected API on Microsoft Graph. On some Microsoft 365 tenants (particularly lower-tier plans) it may not appear in the Application permissions list without first requesting access at `aka.ms/teamsgraph/requestaccess`. E3/E5 tenants typically have access once admin consent is granted.

## Quick Start

```csharp
using Microsoft.Extensions.Logging.Abstractions;
using TeamsNotify.Core.Models;
using TeamsNotify.Core.Services;

var credentials = new TeamsCredentials
{
    TenantId     = "your-tenant-id",
    ClientId     = "your-client-id",
    ClientSecret = "your-client-secret",
};

var auth        = new AuthService(credentials, NullLogger<AuthService>.Instance);
var graphClient = auth.BuildGraphClient();
var graph       = new GraphService(graphClient, NullLogger<GraphService>.Instance);

// Resolve names to IDs (pass GUIDs directly to skip the lookup)
var teamId    = await graph.ResolveTeamIdAsync("DevOps");
var channelId = await graph.ResolveChannelIdAsync(teamId, "Alerts");

await graph.SendMessageAsync(new SendMessageRequest
{
    TeamId    = teamId,
    ChannelId = channelId,
    Body      = "Deployment complete.",
});
```

## Authentication

`AuthService` accepts a `TeamsCredentials` record and builds a configured `GraphServiceClient`. Token acquisition is lazy — the actual authentication request to Entra ID happens on the first Graph API call, not at construction time.

```csharp
var credentials = new TeamsCredentials
{
    TenantId     = Environment.GetEnvironmentVariable("TEAMS_NOTIFY_TENANT_ID")!,
    ClientId     = Environment.GetEnvironmentVariable("TEAMS_NOTIFY_CLIENT_ID")!,
    ClientSecret = Environment.GetEnvironmentVariable("TEAMS_NOTIFY_CLIENT_SECRET")!,
};

var auth        = new AuthService(credentials, NullLogger<AuthService>.Instance);
var graphClient = auth.BuildGraphClient();
```

`BuildGraphClient()` throws `ArgumentException` if any credential field is null or whitespace — this is a fast-fail before any network call is made.

### `TeamsCredentials`

| Property | Type | Description |
|---|---|---|
| `TenantId` | `string` | Your Entra ID tenant GUID — Directory > Overview in the Azure portal |
| `ClientId` | `string` | App Registration client ID — App registrations > your app > Overview |
| `ClientSecret` | `string` | App Registration client secret value — not the secret ID |

## Sending a Message

`GraphService.SendMessageAsync()` accepts a `SendMessageRequest` record. `TeamId` and `ChannelId` must be GUIDs — use the resolve methods below if you have names.

### `SendMessageRequest`

| Property | Type | Required | Description |
|---|---|---|---|
| `TeamId` | `string` | Yes | GUID of the target team |
| `ChannelId` | `string` | Yes | GUID of the target channel |
| `Body` | `string` | Yes | Message text or HTML content |
| `IsHtml` | `bool` | No | Set `true` to send `Body` as HTML. Defaults to `false` |
| `Subject` | `string?` | No | Optional subject line shown above the message body |

### Sending plain text

```csharp
await graph.SendMessageAsync(new SendMessageRequest
{
    TeamId    = teamId,
    ChannelId = channelId,
    Body      = "Build #42 passed. All tests green.",
});
```

### Sending HTML

```csharp
await graph.SendMessageAsync(new SendMessageRequest
{
    TeamId    = teamId,
    ChannelId = channelId,
    Body      = "<b>Build failed</b> — branch <code>main</code><br><ul><li>Step: Unit Tests</li><li>Exit code: 1</li></ul>",
    IsHtml    = true,
});
```

Teams renders a subset of HTML: `<b>`, `<i>`, `<s>`, `<u>`, `<code>`, `<pre>`, `<ul>`, `<ol>`, `<li>`, `<blockquote>`, `<a>`, `<br>`, `<p>`. Headings, images, and tables are stripped.

### Sending with a subject line

```csharp
await graph.SendMessageAsync(new SendMessageRequest
{
    TeamId    = teamId,
    ChannelId = channelId,
    Subject   = "Build #42 Failed",
    Body      = "Unit tests failed on main. See the build log for details.",
});
```

## Resolving Team and Channel Names to IDs

Pass a display name to resolve it to a GUID via a Graph API lookup. Pass a GUID directly to skip the lookup entirely — useful in production scripts where the IDs are stable.

```csharp
// Resolve by name (case-insensitive)
var teamId    = await graph.ResolveTeamIdAsync("DevOps");
var channelId = await graph.ResolveChannelIdAsync(teamId, "Alerts");

// Pass a GUID directly — no lookup performed
var teamId    = await graph.ResolveTeamIdAsync("3fa85f64-5717-4562-b3fc-2c963f66afa6");
var channelId = await graph.ResolveChannelIdAsync(teamId, "19:abc123@thread.tacv2");
```

`TeamsNotFoundException` is thrown if a name does not match any team or channel visible to the app.

## Listing Teams and Channels

```csharp
// List all teams the app has access to
IReadOnlyList<TeamInfo> teams = await graph.ListTeamsAsync();
foreach (var team in teams)
    Console.WriteLine($"{team.Id}  {team.DisplayName}");

// List all channels within a team
IReadOnlyList<ChannelInfo> channels = await graph.ListChannelsAsync(teamId);
foreach (var channel in channels)
    Console.WriteLine($"{channel.Id}  {channel.MembershipType,-12}  {channel.DisplayName}");
```

### `TeamInfo`

| Property | Type | Description |
|---|---|---|
| `Id` | `string` | Team GUID |
| `DisplayName` | `string` | Team display name |

### `ChannelInfo`

| Property | Type | Description |
|---|---|---|
| `Id` | `string` | Channel GUID |
| `DisplayName` | `string` | Channel display name |
| `MembershipType` | `string` | `standard`, `private`, or `shared` |

## Error Handling

| Exception | When thrown |
|---|---|
| `ArgumentException` | A credential field passed to `AuthService` is null or whitespace |
| `TeamsNotFoundException` | A team or channel name passed to a resolve method has no match |
| `AuthenticationFailedException` | Entra ID rejected the credentials — wrong secret, expired secret, or missing admin consent |
| `ODataError` | The Graph API returned an error — inspect `.Error?.Message` for detail. Common causes: throttling (429), insufficient permissions, or the app is not a member of the team |

Graph API throttling (429) is handled automatically by the `Microsoft.Graph` SDK via the `Retry-After` header. If the SDK exhausts its retry attempts an `ODataError` is thrown.

```csharp
using Azure.Identity;
using Microsoft.Graph.Models.ODataErrors;
using TeamsNotify.Core.Exceptions;

try
{
    await graph.SendMessageAsync(request);
}
catch (TeamsNotFoundException ex)
{
    Console.Error.WriteLine($"Not found: {ex.Message}");
}
catch (AuthenticationFailedException ex)
{
    Console.Error.WriteLine($"Auth failed: {ex.Message}");
}
catch (ODataError ex)
{
    Console.Error.WriteLine($"Graph API error: {ex.Error?.Message ?? ex.Message}");
}
```

## Dependency Injection

Wire `AuthService` and `GraphService` into `IServiceCollection` for use in ASP.NET Core, Worker Services, or the Generic Host.

```csharp
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TeamsNotify.Core.Models;
using TeamsNotify.Core.Services;

var services = new ServiceCollection();
services.AddLogging(b => b.AddConsole());

services.AddSingleton(new TeamsCredentials
{
    TenantId     = Environment.GetEnvironmentVariable("TEAMS_NOTIFY_TENANT_ID")!,
    ClientId     = Environment.GetEnvironmentVariable("TEAMS_NOTIFY_CLIENT_ID")!,
    ClientSecret = Environment.GetEnvironmentVariable("TEAMS_NOTIFY_CLIENT_SECRET")!,
});

services.AddSingleton<AuthService>();
services.AddSingleton(sp =>
{
    var auth = sp.GetRequiredService<AuthService>();
    var logger = sp.GetRequiredService<ILogger<GraphService>>();
    return new GraphService(auth.BuildGraphClient(), logger);
});

var provider = services.BuildServiceProvider();
var graph = provider.GetRequiredService<GraphService>();
```

In an ASP.NET Core `Program.cs` or `Startup.cs` simply replace `new ServiceCollection()` with `builder.Services`.

## Full Example

End-to-end: load credentials from environment, build the client, resolve a channel by name, send a formatted HTML notification.

```csharp
using Azure.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Graph.Models.ODataErrors;
using TeamsNotify.Core.Exceptions;
using TeamsNotify.Core.Models;
using TeamsNotify.Core.Services;

// Load credentials from environment
var credentials = new TeamsCredentials
{
    TenantId     = Environment.GetEnvironmentVariable("TEAMS_NOTIFY_TENANT_ID")
                   ?? throw new InvalidOperationException("TEAMS_NOTIFY_TENANT_ID not set"),
    ClientId     = Environment.GetEnvironmentVariable("TEAMS_NOTIFY_CLIENT_ID")
                   ?? throw new InvalidOperationException("TEAMS_NOTIFY_CLIENT_ID not set"),
    ClientSecret = Environment.GetEnvironmentVariable("TEAMS_NOTIFY_CLIENT_SECRET")
                   ?? throw new InvalidOperationException("TEAMS_NOTIFY_CLIENT_SECRET not set"),
};

// Build services
var auth  = new AuthService(credentials, NullLogger<AuthService>.Instance);
var graph = new GraphService(auth.BuildGraphClient(), NullLogger<GraphService>.Instance);

try
{
    // Resolve names to IDs
    var teamId    = await graph.ResolveTeamIdAsync("DevOps");
    var channelId = await graph.ResolveChannelIdAsync(teamId, "Alerts");

    // Send an HTML notification
    await graph.SendMessageAsync(new SendMessageRequest
    {
        TeamId    = teamId,
        ChannelId = channelId,
        Subject   = "Deployment complete — v2.1.0",
        Body      = """
                    <b>Version 2.1.0</b> deployed to <code>production</code>.<br>
                    <ul>
                      <li>All tests passed</li>
                      <li>Migration ran successfully</li>
                    </ul>
                    """,
        IsHtml    = true,
    });

    Console.WriteLine("Notification sent.");
}
catch (TeamsNotFoundException ex)
{
    Console.Error.WriteLine($"error: {ex.Message}");
    Environment.Exit(3);
}
catch (AuthenticationFailedException ex)
{
    Console.Error.WriteLine($"auth failed: {ex.Message}");
    Environment.Exit(2);
}
catch (ODataError ex)
{
    Console.Error.WriteLine($"graph api error: {ex.Error?.Message ?? ex.Message}");
    Environment.Exit(4);
}
```
