# Notify.Teams

Send Microsoft Teams channel messages from any .NET application via the Graph API. Handles authentication, team/channel name resolution, and message formatting (plain text and HTML).

```bash
dotnet add package Notify.Teams
```

## Quick start

```csharp
using Notify.Teams.Models;
using Notify.Teams.Services;

var credentials = new TeamsCredentials
{
    TenantId     = "your-tenant-id",
    ClientId     = "your-client-id",
    ClientSecret = "your-client-secret"
};

var auth    = new AuthService(credentials);
var graph   = new GraphService(auth.BuildGraphClient());

// Resolve names to IDs (or pass GUIDs directly)
var teamId    = await graph.ResolveTeamIdAsync("DevOps");
var channelId = await graph.ResolveChannelIdAsync(teamId, "Alerts");

await graph.SendMessageAsync(new SendMessageRequest
{
    TeamId    = teamId,
    ChannelId = channelId,
    Body      = "Deployment complete",
});
```

## Authentication

Requires an Entra ID App Registration with the `ChannelMessage.Send` application permission (admin consent required). Supply the tenant ID, client ID, and client secret from the registration.

## HTML messages

Set `IsHtml = true` on the request to send formatted content. Teams renders a subset of HTML: bold, italic, lists, links, code blocks, and blockquotes.

```csharp
await graph.SendMessageAsync(new SendMessageRequest
{
    TeamId    = teamId,
    ChannelId = channelId,
    Subject   = "Build #42 Failed",
    Body      = "<b>Failed</b> — branch <code>main</code>",
    IsHtml    = true,
});
```

## Listing teams and channels

```csharp
var teams    = await graph.ListTeamsAsync();
var channels = await graph.ListChannelsAsync(teamId);
```

## Source and implementation example

Source code and full documentation are on GitHub at [EvilGeniusCore/Notify](https://github.com/EvilGeniusCore/Notify).

If you want to see a complete working implementation using this library, the `notify` CLI tool in that repository is built entirely on `Notify.Teams` and covers credential loading, command parsing, exit code handling, and dry-run support — a useful reference for integrating the library into your own application.

## Licence

LGPL-3.0. Commercial use permitted. Modifications to this library must be shared back under LGPL. Applications that consume this library are not affected by the copyleft.
