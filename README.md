# teams-notify

A .NET 10 CLI tool for sending Microsoft Teams messages via the Graph API — designed for CI/CD pipelines, cron jobs, and scripts. Runs on Linux, Windows, and Mac with no .NET runtime required.

## Install

Download the self-contained binary for your platform from the releases page and place it on your PATH.

For users who already have the .NET runtime:

```bash
dotnet tool install -g teams-notify
```

## Usage

```bash
# Send a message to a channel
teams-notify send --team DevOps --channel Alerts --message "Deployment complete"

# Send with a subject
teams-notify send --team DevOps --channel Alerts --subject "Build #42 Failed" --message "Unit tests failed on main"

# Send HTML formatted message
teams-notify send --team DevOps --channel Alerts --html --message "<b>Failed</b> — branch <code>main</code>"

# Send message from a file
teams-notify send --team DevOps --channel Alerts --file ./message.html --html

# Pipe stdin
echo "Build failed" | teams-notify send --team DevOps --channel Alerts

# Use a credentials file
teams-notify send --env-file ./teams.env --team DevOps --channel Alerts --message "Done"

# Dry run (prints request, does not send)
teams-notify send --team DevOps --channel Alerts --message "test" --dry-run

# Discover teams and channels
teams-notify list
teams-notify list --team DevOps
```

## Configuration

Credentials are resolved in this order:

1. `--env-file <path>` — key=value file, overrides everything
2. Environment variables — set in the shell, crontab, or container
3. Config file — written by `teams-notify configure`, stored in the platform-appropriate location

### Environment variables

```
TEAMS_NOTIFY_TENANT_ID=<your-tenant-id>
TEAMS_NOTIFY_CLIENT_ID=<your-client-id>
TEAMS_NOTIFY_CLIENT_SECRET=<your-client-secret>
TEAMS_NOTIFY_DEFAULT_TEAM=DevOps
TEAMS_NOTIFY_DEFAULT_CHANNEL=Alerts
```

### Typical patterns

| Scenario | Approach |
|---|---|
| CI/CD pipeline or container | Set environment variables in the platform |
| Cron job or script folder | `--env-file ./teams.env` alongside your script |
| Developer laptop | Run `teams-notify configure` once |

### Entra ID setup

- Create an App Registration in Entra ID
- Generate a client secret
- Grant admin consent for the `ChannelMessage.Send` permission
- Use the tenant ID, client ID, and client secret as your credentials

## Commands

| Command | Description |
|---|---|
| `send` | Send a message to a channel or user |
| `configure` | Save default credentials and channel settings to the config file |
| `list` | List all teams (no args) or channels within a team (`--team <name\|id>`) |
| `version` | Show version info |

## Send options

```
-m, --message <text>      Message body — required unless --file or stdin is used
-f, --file <path>         Read message body from a file
-t, --team <name|id>      Target team
-c, --channel <name|id>   Target channel
    --to <email>          Send to a user instead
    --subject <text>      Optional subject/title
    --html                Treat message body as HTML
    --dry-run             Print request, don't send
-q, --quiet               Suppress output (exit code only)

Global:
    --env-file <path>     Load credentials from a key=value file
```

`--team` and `--channel` accept either a name or a GUID. Names require a Graph API lookup — use `teams-notify list` to find IDs for production scripts where stability matters.

## HTML support

Pass `--html` to send formatted content. Teams renders a subset of HTML:

| Tag | Renders as |
|---|---|
| `<b>`, `<strong>` | Bold |
| `<i>`, `<em>` | Italic |
| `<code>` | Inline code |
| `<pre>` | Code block |
| `<ul>` / `<ol>` / `<li>` | Lists |
| `<blockquote>` | Quote block |
| `<a href="...">` | Hyperlink |
| `<br>`, `<p>` | Line breaks / paragraphs |

Headings, images, and tables are not supported and will be stripped by Teams.

## Exit Codes

| Code | Meaning |
|---|---|
| `0` | Success |
| `1` | General error |
| `2` | Auth failure |
| `3` | Team or channel not found |
| `4` | Graph API error (throttled 429, server error 5xx) |
| `5` | Configuration missing or destination cannot be resolved |
