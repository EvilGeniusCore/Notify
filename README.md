# notify

A .NET 10 CLI tool for sending Microsoft Teams messages via the Graph API — designed for CI/CD pipelines, cron jobs, and scripts. Runs on Linux, Windows, and macOS with no .NET runtime required.

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
# Set credentials once
export NOTIFY_TEAMS_TENANT_ID=your-tenant-id
export NOTIFY_TEAMS_CLIENT_ID=your-client-id
export NOTIFY_TEAMS_CLIENT_SECRET=your-client-secret

# Send a message
notify send --team DevOps --channel Alerts --message "Deployment complete"
```

## Usage

```bash
# Send a plain text message
notify send --team DevOps --channel Alerts --message "Deployment complete"

# Send with a subject line
notify send --team DevOps --channel Alerts --subject "Build #42 Failed" --message "Unit tests failed on main"

# Send an HTML formatted message
notify send --team DevOps --channel Alerts --html --message "<b>Failed</b> — branch <code>main</code>"

# Send message body from a file
notify send --team DevOps --channel Alerts --file ./message.html --html

# Pipe stdin
echo "Build failed" | notify send --team DevOps --channel Alerts

# Use a credentials file (useful for cron jobs)
notify send --env-file ./teams.env --team DevOps --channel Alerts --message "Done"

# Dry run — prints the resolved request without sending
notify send --team DevOps --channel Alerts --message "test" --dry-run

# Discover available teams and channels
notify list
notify list --team DevOps
```

## Configuration

Credentials are resolved in this order (highest priority first):

1. `--env-file <path>` — key=value file, overrides everything
2. Environment variables — set in the shell, crontab, or container
3. Config file — written by `notify configure`, stored in the platform-appropriate location

### Environment variables

```
NOTIFY_TEAMS_TENANT_ID=<your-tenant-id>
NOTIFY_TEAMS_CLIENT_ID=<your-client-id>
NOTIFY_TEAMS_CLIENT_SECRET=<your-client-secret>
NOTIFY_TEAMS_DEFAULT_TEAM=DevOps
NOTIFY_TEAMS_DEFAULT_CHANNEL=Alerts
```

### Typical patterns

| Scenario | Approach |
|---|---|
| CI/CD pipeline or container | Set environment variables in the platform |
| Cron job or script folder | `--env-file ./teams.env` alongside your script |
| Developer laptop | Run `notify configure` once |

### Entra ID setup

1. In the [Azure portal](https://portal.azure.com), open **Entra ID > App registrations > New registration**
2. Note the **Directory (tenant) ID** and **Application (client) ID** from the Overview page
3. Under **Certificates & secrets**, create a new client secret and copy the **value** (not the ID)
4. Under **API permissions**, add `Microsoft Graph > Application permissions > ChannelMessage.Send`
5. Click **Grant admin consent**
6. Add the app to each team it needs to post to: open the team in Teams, go to **Manage team > Apps > Add an app**, search for your app registration by name

## Commands

| Command | Description |
|---|---|
| `send` | Send a message to a Teams channel |
| `configure` | Save default credentials and channel settings to the config file |
| `list` | List all teams (no args) or channels within a team (`--team <name\|id>`) |
| `version` | Show version, runtime, and OS info |

## Send options

```
-m, --message <text>      Message body — required unless --file or stdin is used
-f, --file <path>         Read message body from a file
-t, --team <name|id>      Target team (overrides NOTIFY_TEAMS_DEFAULT_TEAM)
-c, --channel <name|id>   Target channel (overrides NOTIFY_TEAMS_DEFAULT_CHANNEL)
    --subject <text>      Optional subject line shown above the message body
    --html                Treat message body as HTML (Teams HTML subset)
    --dry-run             Print the resolved request without sending
-q, --quiet               Suppress output; rely on exit code only

Global:
    --env-file <path>     Load credentials from a key=value file
```

`--team` and `--channel` accept either a name or a GUID. Names require a Graph API lookup — use `notify list` to find IDs for production scripts where stability matters.

## HTML support

Pass `--html` to send formatted content. Teams renders a subset of HTML:

| Tag | Renders as |
|---|---|
| `<b>`, `<strong>` | Bold |
| `<i>`, `<em>` | Italic |
| `<s>` | Strikethrough |
| `<u>` | Underline |
| `<code>` | Inline code |
| `<pre>` | Code block |
| `<ul>` / `<ol>` / `<li>` | Lists |
| `<blockquote>` | Quote block |
| `<a href="...">` | Hyperlink |
| `<br>`, `<p>` | Line breaks / paragraphs |

Headings, images, and tables are not supported and will be stripped by Teams.

## Using Notify.Teams in your .NET app

If you want to send Teams messages directly from a .NET application without the CLI:

```csharp
using Notify.Teams.Models;
using Notify.Teams.Services;

var credentials = new TeamsCredentials
{
    TenantId     = Environment.GetEnvironmentVariable("NOTIFY_TEAMS_TENANT_ID")!,
    ClientId     = Environment.GetEnvironmentVariable("NOTIFY_TEAMS_CLIENT_ID")!,
    ClientSecret = Environment.GetEnvironmentVariable("NOTIFY_TEAMS_CLIENT_SECRET")!,
};

var auth    = new AuthService(credentials);
var graph   = new GraphService(auth.BuildGraphClient());

var teamId    = await graph.ResolveTeamIdAsync("DevOps");
var channelId = await graph.ResolveChannelIdAsync(teamId, "Alerts");

await graph.SendMessageAsync(new SendMessageRequest
{
    TeamId    = teamId,
    ChannelId = channelId,
    Subject   = "Deployment complete",
    Body      = "All tests passed. Version 2.1.0 deployed to production.",
});
```

See the [Notify.Teams library guide](Documentation/Notify.Teams-Library-Guide.md) for full API documentation.

## Exit codes

| Code | Meaning |
|---|---|
| `0` | Success |
| `1` | General error |
| `2` | Auth failure |
| `3` | Team or channel not found |
| `4` | Graph API error (throttled 429, server error 5xx) |
| `5` | Configuration missing |

## Licence

LGPL-3.0. Commercial use permitted. Modifications to this library must be shared back under LGPL. Applications that consume this library are not affected by the copyleft.
