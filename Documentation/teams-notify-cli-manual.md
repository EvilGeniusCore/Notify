# teams-notify — CLI User Manual
> For admins, developers, and script writers who need to send Microsoft Teams messages from the command line, scripts, CI/CD pipelines, or cron jobs.

## Overview

`teams-notify` is a cross-platform CLI tool for sending messages to Microsoft Teams channels. It runs on Windows, Linux, and macOS with no .NET runtime required and is designed to be dropped into any script or pipeline with minimal setup.

## Requirements

- An Entra ID App Registration with:
  - `ChannelMessage.Send` application permission (admin consented)
  - A client secret
- One-time configuration of credentials (env vars, env file, or `teams-notify configure`)

See [Setting Up an Entra ID App Registration](#setting-up-an-entra-id-app-registration) below.

## Installation

Download the zip for your platform from the [releases page](https://github.com/Bonejob/teams-notify/releases), extract it, and place the binary on your PATH.

| Platform | Binary in zip |
|---|---|
| Windows x64 | `teams-notify.exe` |
| Linux x64 | `teams-notify` |
| Linux arm64 | `teams-notify` |
| macOS x64 | `teams-notify` |
| macOS arm64 (Apple Silicon) | `teams-notify` |

Each zip contains the binary, `README.md`, `LICENSE`, and this manual.

**Linux / macOS — make executable after extracting:**

```bash
chmod +x teams-notify
sudo mv teams-notify /usr/local/bin/
```

**Windows — add to PATH:**

Place `teams-notify.exe` in a folder that is on your `PATH`, or add its folder to your `PATH` via System Properties.

## Setting Up an Entra ID App Registration

1. Go to [portal.azure.com](https://portal.azure.com) and sign in with your organisation account
2. Search for **App registrations** in the top search bar and click **New registration**
3. Give the app a name (e.g. `teams-notify`), leave all other defaults, click **Register**
4. From the **Overview** page, copy:
   - **Application (client) ID** → your `TEAMS_NOTIFY_CLIENT_ID`
   - **Directory (tenant) ID** → your `TEAMS_NOTIFY_TENANT_ID`
5. Go to **Certificates & secrets** → **New client secret** → set an expiry → click **Add**. Copy the **Value** immediately — it is only shown once → your `TEAMS_NOTIFY_CLIENT_SECRET`
6. Go to **API permissions** → **Add a permission** → **Microsoft Graph** → **Application permissions** → search for `ChannelMessage.Send` → add it
7. Click **Grant admin consent for [your organisation]** — a Global Admin must complete this step

> **Note:** `ChannelMessage.Send` is a protected API. On some Microsoft 365 tenants it may require requesting access at `aka.ms/teamsgraph/requestaccess` before it appears in the Application permissions list. E3/E5 tenants typically have access once a Global Admin has granted consent.

## Configuration

Credentials are resolved in this order, stopping at the first source that provides a value:

1. `--env-file <path>` — explicit key=value file passed on the command line
2. Environment variables — set in the shell, container, or CI/CD platform
3. Config file — written by `teams-notify configure`, stored in the platform config directory
4. Error — exits with code `5` if required credentials are missing

### Environment variables

```
TEAMS_NOTIFY_TENANT_ID       Entra ID tenant GUID
TEAMS_NOTIFY_CLIENT_ID       App Registration client ID
TEAMS_NOTIFY_CLIENT_SECRET   App Registration client secret value
TEAMS_NOTIFY_DEFAULT_TEAM    Default team name or GUID (optional)
TEAMS_NOTIFY_DEFAULT_CHANNEL Default channel name or GUID (optional)
```

### Using an env file

An env file is a plain text file with one `KEY=VALUE` per line. Lines starting with `#` are ignored.

```
# teams.env
TEAMS_NOTIFY_TENANT_ID=your-tenant-id
TEAMS_NOTIFY_CLIENT_ID=your-client-id
TEAMS_NOTIFY_CLIENT_SECRET=your-secret
TEAMS_NOTIFY_DEFAULT_TEAM=DevOps
TEAMS_NOTIFY_DEFAULT_CHANNEL=Alerts
```

```bash
teams-notify send --env-file ./teams.env --message "Done"
```

Keep the env file out of source control — add `*.env` to your `.gitignore`.

### Saving a default configuration

`teams-notify configure` saves credentials to the platform config file so you don't need to set environment variables on your developer machine.

```bash
teams-notify configure \
  --tenant-id your-tenant-id \
  --client-id your-client-id \
  --client-secret your-secret \
  --default-team DevOps \
  --default-channel Alerts
```

You can supply any subset of options — unspecified fields are preserved from the existing config file. Run `configure` again at any time to update a single value.

**Config file locations:**

| Platform | Path |
|---|---|
| Windows | `%APPDATA%\teams-notify\config.json` |
| Linux / macOS | `~/.config/teams-notify/config.json` |

## Commands

### `send` — Send a message

```
teams-notify send [options]

  -m, --message <text>      Message body (or omit to read from --file or stdin)
  -f, --file <path>         Read message body from a file
  -t, --team <name|id>      Target team — overrides TEAMS_NOTIFY_DEFAULT_TEAM
  -c, --channel <name|id>   Target channel — overrides TEAMS_NOTIFY_DEFAULT_CHANNEL
      --subject <text>      Optional subject line shown above the message
      --html                Treat the message body as HTML
      --dry-run             Print the resolved request without sending
  -q, --quiet               Suppress all output (exit code only)
      --env-file <path>     Load credentials from a key=value file
```

Exactly one of `--message`, `--file`, or piped stdin must supply the message body. If none are provided the command exits with code `1`.

### `list` — Discover teams and channels

```
teams-notify list [options]

  (no --team)               List all teams the app has access to
  -t, --team <name|id>      List channels within the specified team
      --env-file <path>     Load credentials from a key=value file
```

Use `list` to find the exact names and GUIDs to use in your scripts.

### `configure` — Save default credentials and channel

```
teams-notify configure [options]

  --tenant-id <guid>           Entra ID tenant GUID
  --client-id <guid>           App Registration client GUID
  --client-secret <secret>     App Registration client secret value
  --default-team <name|id>     Default team used when --team is not supplied
  --default-channel <name|id>  Default channel used when --channel is not supplied
```

### `version` — Show version info

```
teams-notify version
```

Prints the binary version, .NET runtime, and OS.

## Examples

### Send a plain text message

```bash
teams-notify send --team "DevOps" --channel "Alerts" -m "Deployment complete."
```

### Send a message with a subject line

```bash
teams-notify send --team "DevOps" --channel "Alerts" \
  --subject "Build #42 Failed" \
  -m "Unit tests failed on main."
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
teams-notify send --env-file ./teams.env -m "Done"
```

### Discover available teams and channels

```bash
teams-notify list
teams-notify list --team "DevOps"
```

## Using in CI/CD Pipelines

The general pattern for all platforms: store credentials as pipeline secrets, inject them as environment variables, call `teams-notify send` as a step.

### GitHub Actions

```yaml
- name: Notify Teams
  env:
    TEAMS_NOTIFY_TENANT_ID:     ${{ secrets.TEAMS_NOTIFY_TENANT_ID }}
    TEAMS_NOTIFY_CLIENT_ID:     ${{ secrets.TEAMS_NOTIFY_CLIENT_ID }}
    TEAMS_NOTIFY_CLIENT_SECRET: ${{ secrets.TEAMS_NOTIFY_CLIENT_SECRET }}
  run: |
    teams-notify send \
      --team "DevOps" \
      --channel "Alerts" \
      --subject "Build ${{ github.run_number }} complete" \
      --message "Branch: ${{ github.ref_name }}"
```

Add the secrets under **Repository → Settings → Secrets and variables → Actions**.

### GitLab CI

```yaml
notify-teams:
  stage: notify
  script:
    - teams-notify send
        --team "DevOps"
        --channel "Alerts"
        --subject "Pipeline $CI_PIPELINE_ID complete"
        --message "Branch: $CI_COMMIT_REF_NAME"
  variables:
    TEAMS_NOTIFY_TENANT_ID:     $TEAMS_NOTIFY_TENANT_ID
    TEAMS_NOTIFY_CLIENT_ID:     $TEAMS_NOTIFY_CLIENT_ID
    TEAMS_NOTIFY_CLIENT_SECRET: $TEAMS_NOTIFY_CLIENT_SECRET
```

Add the variables under **Settings → CI/CD → Variables** with the **Masked** flag set.

### Azure Pipelines

```yaml
- task: Bash@3
  displayName: Notify Teams
  env:
    TEAMS_NOTIFY_TENANT_ID:     $(TEAMS_NOTIFY_TENANT_ID)
    TEAMS_NOTIFY_CLIENT_ID:     $(TEAMS_NOTIFY_CLIENT_ID)
    TEAMS_NOTIFY_CLIENT_SECRET: $(TEAMS_NOTIFY_CLIENT_SECRET)
  inputs:
    targetType: inline
    script: |
      teams-notify send \
        --team "DevOps" \
        --channel "Alerts" \
        --subject "Build $(Build.BuildNumber) complete" \
        --message "Branch: $(Build.SourceBranchName)"
```

Add the variables under **Pipelines → Library → Variable groups** and link the group to the pipeline.

### Jenkins

```groovy
stage('Notify Teams') {
    steps {
        withCredentials([
            string(credentialsId: 'teams-tenant-id',     variable: 'TEAMS_NOTIFY_TENANT_ID'),
            string(credentialsId: 'teams-client-id',     variable: 'TEAMS_NOTIFY_CLIENT_ID'),
            string(credentialsId: 'teams-client-secret', variable: 'TEAMS_NOTIFY_CLIENT_SECRET')
        ]) {
            sh """
                teams-notify send \\
                  --team "DevOps" \\
                  --channel "Alerts" \\
                  --subject "Build ${env.BUILD_NUMBER} complete" \\
                  --message "Branch: ${env.BRANCH_NAME}"
            """
        }
    }
}
```

## Using in Cron Jobs

Use `--env-file` to keep credentials out of the crontab entirely. Store the env file in a location only readable by the user running the cron job.

```bash
chmod 600 /etc/teams-notify/prod.env
```

```crontab
# Run nightly backup and notify on completion
0 2 * * * /usr/local/bin/backup.sh && \
  teams-notify send \
    --env-file /etc/teams-notify/prod.env \
    --team "Ops" \
    --channel "Alerts" \
    --message "Nightly backup completed: $(date)"
```

To notify on failure as well:

```bash
#!/bin/bash
# backup.sh
/usr/local/bin/do-backup.sh
EXIT=$?

MSG="Backup $([ $EXIT -eq 0 ] && echo 'succeeded' || echo 'FAILED (exit $EXIT)'): $(date)"

teams-notify send \
  --env-file /etc/teams-notify/prod.env \
  --team "Ops" \
  --channel "Alerts" \
  --message "$MSG" \
  --quiet

exit $EXIT
```

## Exit Codes

| Code | Meaning |
|---|---|
| `0` | Success |
| `1` | General error (including no message provided) |
| `2` | Auth failure — wrong credentials or missing admin consent |
| `3` | Team or channel not found |
| `4` | Graph API error (throttled 429 or server error 5xx) |
| `5` | Configuration missing — required credential or destination not set |

Scripts should check `$?` (Linux/macOS) or `%ERRORLEVEL%` (Windows) after each call.

```bash
teams-notify send --team "DevOps" --channel "Alerts" -m "Done"
if [ $? -ne 0 ]; then
  echo "Teams notification failed" >&2
fi
```

## Troubleshooting

**`error: authentication failed`** (exit code 2)
- Verify the tenant ID, client ID, and client secret are correct
- Check the client secret has not expired — regenerate it in the Azure portal if needed
- Confirm admin consent has been granted for `ChannelMessage.Send` in API permissions

**`error: No team found with the name '...'`** (exit code 3)
- Run `teams-notify list` to see the exact team names visible to the app
- The app must be a member of the team — add it via **Manage team → Apps** in Teams
- Try passing the team GUID instead of the name to rule out name mismatch

**`error: No channel found with the name '...'`** (exit code 3)
- Run `teams-notify list --team "..."` to see the exact channel names
- Private channels require the app to be explicitly added to that channel

**`error: graph api error`** (exit code 4)
- A 429 response means the Graph API is throttling — the SDK retries automatically but if retries are exhausted the command fails. Wait and retry.
- A 5xx response is a transient Graph API error — retry after a short delay

**`ChannelMessage.Send` not visible in API permissions**
- Your tenant may require requesting access to this protected API at `aka.ms/teamsgraph/requestaccess`
- On E3/E5 tenants, a Global Admin granting consent is usually sufficient
- Check that you selected **Application permissions** (not Delegated) when adding the permission
