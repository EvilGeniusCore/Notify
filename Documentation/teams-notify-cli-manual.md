# teams-notify — CLI User Manual
> For admins, developers, and script writers who need to send Microsoft Teams messages from the command line, scripts, CI/CD pipelines, or cron jobs.

---

> **STUB — Complete this document once the application is finalised and all rough edges are resolved.**
> Sections marked `[TODO]` require working code and confirmed behaviour before they can be written accurately.

---

## Overview

`teams-notify` is a cross-platform CLI tool for sending messages to Microsoft Teams channels. It runs on Windows, Linux, and macOS with no .NET runtime required (self-contained binary) and is designed to be dropped into any script or pipeline with minimal setup.

---

## Requirements

- An Entra ID App Registration with:
  - `ChannelMessage.Send` application permission (admin consented)
  - A client secret
- One-time configuration of credentials (env vars, env file, or `teams-notify configure`)

See [Setting Up an Entra ID App Registration](#setting-up-an-entra-id-app-registration) below.

---

## Installation

### Option 1 — Self-contained binary (no .NET runtime required)

[TODO] Download the binary for your platform from the releases page and place it on your PATH.

| Platform | Binary |
|---|---|
| Windows x64 | `teams-notify.exe` |
| Linux x64 | `teams-notify` |
| Linux arm64 | `teams-notify` |
| macOS x64 | `teams-notify` |
| macOS arm64 | `teams-notify` |

### Option 2 — dotnet global tool (requires .NET runtime)

```bash
dotnet tool install -g teams-notify
```

---

## Setting Up an Entra ID App Registration

[TODO] Step-by-step walkthrough:
1. Create the App Registration in the Azure / Entra portal
2. Add the `ChannelMessage.Send` application permission
3. Grant admin consent
4. Create a client secret
5. Note down Tenant ID, Client ID, Client Secret

---

## Configuration

Credentials are resolved in this order, stopping at the first source that provides a value:

1. `--env-file <path>` — explicit key=value file passed on the command line
2. Environment variables — set in the shell, container, or CI/CD platform
3. Config file — written by `teams-notify configure`, stored in the platform config directory
4. Error — exits with code `5` if required credentials are missing

### Environment variables

```
TEAMS_NOTIFY_TENANT_ID
TEAMS_NOTIFY_CLIENT_ID
TEAMS_NOTIFY_CLIENT_SECRET
TEAMS_NOTIFY_DEFAULT_TEAM
TEAMS_NOTIFY_DEFAULT_CHANNEL
```

### Using an env file

```bash
teams-notify send --env-file ./teams.env --team "DevOps" --channel "Alerts" -m "Done"
```

```
# teams.env
TEAMS_NOTIFY_TENANT_ID=your-tenant-id
TEAMS_NOTIFY_CLIENT_ID=your-client-id
TEAMS_NOTIFY_CLIENT_SECRET=your-secret
TEAMS_NOTIFY_DEFAULT_TEAM=DevOps
TEAMS_NOTIFY_DEFAULT_CHANNEL=Alerts
```

### Saving a default configuration

```bash
teams-notify configure
```

[TODO] Document interactive prompts and saved file location per platform.

---

## Commands

### `send` — Send a message

```
teams-notify send [options]

Options:
  -m, --message <text>      Message body (or omit to read from --file or stdin)
  -f, --file <path>         Read message body from a file
  -t, --team <name|id>      Target team (name or GUID)
  -c, --channel <name|id>   Target channel (name or GUID)
      --subject <text>      Optional subject line shown above the message
      --html                Treat the message body as HTML
      --dry-run             Print the request without sending
  -q, --quiet               Suppress all output (exit code only)
      --env-file <path>     Load credentials from a key=value file
```

### `list` — List teams and channels

```
teams-notify list [options]

  (no arguments)            List all teams the app has access to
  -t, --team <name|id>      List channels within the specified team
```

### `configure` — Save default credentials and channel

```
teams-notify configure [options]
```

[TODO] Document options and behaviour.

### `version` — Show version info

```
teams-notify version
```

---

## Examples

### Send a plain text message

```bash
teams-notify send --team "DevOps" --channel "Alerts" -m "Deployment complete."
```

### Send a message with a subject line

```bash
teams-notify send --team "DevOps" --channel "Alerts" --subject "Build #42 Failed" -m "Unit tests failed on main."
```

### Send an HTML message

```bash
teams-notify send --team "DevOps" --channel "Alerts" --html \
  -m "<b>Deployment failed</b><br>Branch: <code>main</code>"
```

### Pipe output from another command

```bash
echo "Backup finished: $(date)" | teams-notify send --channel "Alerts"
```

### Send the contents of a file

```bash
teams-notify send --team "DevOps" --channel "Alerts" -f report.txt
```

### Send an HTML file

```bash
teams-notify send --team "DevOps" --channel "Alerts" --html -f report.html
```

### Preview without sending

```bash
teams-notify send --team "DevOps" --channel "Alerts" -m "Test" --dry-run
```

### Use an env file instead of environment variables

```bash
teams-notify send --env-file ./teams.env --channel "Alerts" -m "Done"
```

### List all accessible teams

```bash
teams-notify list
```

### List channels in a team

```bash
teams-notify list --team "DevOps"
```

---

## Using in CI/CD Pipelines

[TODO] Examples for:
- GitHub Actions
- GitLab CI
- Azure Pipelines
- Jenkins

General pattern — inject credentials as pipeline secrets, call `teams-notify send` as a step.

```yaml
# [TODO — GitHub Actions example]
```

---

## Using in Cron Jobs

[TODO] Example crontab entry using `--env-file` to keep credentials out of the crontab itself.

```bash
# [TODO]
```

---

## Exit Codes

| Code | Meaning |
|---|---|
| `0` | Success |
| `1` | General error |
| `2` | Auth failure |
| `3` | Team or channel not found |
| `4` | Graph API error (throttled or server error) |
| `5` | Configuration missing or destination cannot be resolved |

---

## Troubleshooting

[TODO] Common issues and fixes:
- Auth failure — wrong credentials, missing admin consent
- Team/channel not found — name vs ID, app not a member of the team
- Throttling — Graph API rate limits, retry behaviour
- Permissions — `ChannelMessage.Send` not granted
