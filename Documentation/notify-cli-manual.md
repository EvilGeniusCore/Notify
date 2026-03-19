# notify — CLI User Manual
> For admins, developers, and script writers who need to send Microsoft Teams messages from the command line, scripts, CI/CD pipelines, or cron jobs.

## Overview

`notify` is a cross-platform CLI tool for sending messages to Microsoft Teams channels. It runs on Windows, Linux, and macOS with no .NET runtime required and is designed to be dropped into any script or pipeline with minimal setup.

## Requirements

- An Entra ID App Registration with `Team.ReadBasic.All` and `Channel.ReadBasic.All` application permissions (admin consented) and a client secret
- The `notify` Teams app installed in each team you want to post to — this grants the RSC permission that allows app-only channel messaging
- One-time configuration of credentials (env vars, env file, or `notify configure`)

## Known Issue — `notify send` Currently Blocked (March 2026)

`notify list` works correctly. `notify send` fails with a Graph API permission error due to a confirmed Microsoft bug in the `ChannelMessage.Send.Group` RSC permission implementation. The permission is documented as supported but is not currently honoured by the Graph API send endpoint.

Bug report: [MicrosoftDocs/msteams-docs #14043](https://github.com/MicrosoftDocs/msteams-docs/issues/14043)
Microsoft acknowledged: 17 February 2026. Escalation in progress.

No changes to your configuration are needed — everything is set up correctly. The fix is on Microsoft's engineering backlog.

See [Setting Up an Entra ID App Registration](#setting-up-an-entra-id-app-registration) below.

## Installation

Download the zip for your platform from the [releases page](https://github.com/EvilGeniusCore/Notify/releases), extract it, and place the binary on your PATH.

| Platform | Binary in zip |
|---|---|
| Windows x64 | `notify.exe` |
| Linux x64 | `notify` |
| Linux arm64 | `notify` |
| macOS x64 | `notify` |
| macOS arm64 (Apple Silicon) | `notify` |

Each zip contains the binary, `README.md`, `LICENSE`, and this manual.

**Linux / macOS — make executable after extracting:**

```bash
chmod +x notify
sudo mv notify /usr/local/bin/
```

**Windows — add to PATH:**

Place `notify.exe` in a folder that is on your `PATH`, or add its folder to your `PATH` via System Properties.

## Setting Up an Entra ID App Registration

`ChannelMessage.Send` does not exist as an Application permission in Microsoft Graph — it is delegated only. Sending messages app-only requires a Teams app installed in each team via RSC (Resource-Specific Consent). The App Registration here covers reading teams and channels only; the RSC permission for sending is granted when the Teams app is installed. See [Teams App Setup](#teams-app-setup) below.

1. Go to [portal.azure.com](https://portal.azure.com) and sign in with your organisation account
2. Search for **App registrations** in the top search bar and click **New registration**
3. Give the app a name (e.g. `notify`), leave all other defaults, click **Register**
4. From the **Overview** page, copy:
   - **Application (client) ID** → your `NOTIFY_TEAMS_CLIENT_ID`
   - **Directory (tenant) ID** → your `NOTIFY_TEAMS_TENANT_ID`
5. Go to **Certificates & secrets** → **New client secret** → set an expiry → click **Add**. Copy the **Value** immediately — it is only shown once → your `NOTIFY_TEAMS_CLIENT_SECRET`
6. Go to **API permissions** → **Add a permission** → **Microsoft Graph** → **Application permissions** → add both:
   - `Team.ReadBasic.All` — allows `notify list` to enumerate teams and resolve team names to IDs
   - `Channel.ReadBasic.All` — allows `notify list` to enumerate channels and resolve channel names to IDs
7. Click **Grant admin consent for [your organisation]** — a Global Admin must complete this step

## Teams App Setup

Sending messages requires the `notify` Teams app to be installed in each target team. This grants the RSC permission `ChannelMessage.Send.Group` scoped to that team, which is the only supported path for app-only channel messaging.

See [TeamsApp/README.md](../TeamsApp/README.md) for the full packaging and installation steps. The short version:

1. Clone the repo and fill in `TeamsApp/manifest.json` with your App Registration client ID
2. Run `TeamsApp/Package-TeamsApp.ps1` to produce `notify-app.zip`
3. In Teams, go to the target team → **Manage team** → **Apps** → **Upload an app** → select `notify-app.zip`
4. Repeat for each team that needs to receive notifications

The `notify list` command works as soon as the App Registration is configured. The `notify send` command requires the Teams app to be installed in the target team.

## Configuration

Credentials are resolved in this order, stopping at the first source that provides a value:

1. `--env-file <path>` — explicit key=value file passed on the command line
2. Environment variables — set in the shell, container, or CI/CD platform
3. Config file — written by `notify configure`, stored in the platform config directory
4. Error — exits with code `5` if required credentials are missing

### Environment variables

```
NOTIFY_TEAMS_TENANT_ID       Entra ID tenant GUID
NOTIFY_TEAMS_CLIENT_ID       App Registration client ID
NOTIFY_TEAMS_CLIENT_SECRET   App Registration client secret value
NOTIFY_TEAMS_DEFAULT_TEAM    Default team name or GUID (optional)
NOTIFY_TEAMS_DEFAULT_CHANNEL Default channel name or GUID (optional)
```

### Using an env file

An env file is a plain text file with one `KEY=VALUE` per line. Lines starting with `#` are ignored.

```
# notify.env
NOTIFY_TEAMS_TENANT_ID=your-tenant-id
NOTIFY_TEAMS_CLIENT_ID=your-client-id
NOTIFY_TEAMS_CLIENT_SECRET=your-secret
NOTIFY_TEAMS_DEFAULT_TEAM=DevOps
NOTIFY_TEAMS_DEFAULT_CHANNEL=Alerts
```

```bash
notify send --env-file ./notify.env --message "Done"
```

If the file is named `notify.env` and is in the current directory, it is loaded automatically without `--env-file`.

Keep the env file out of source control — add `notify.env` to your `.gitignore`.

### Saving a default configuration

`notify configure` saves credentials to the platform config file so you don't need to set environment variables on your developer machine.

```bash
notify configure \
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
| Windows | `%APPDATA%\notify\config.json` |
| Linux / macOS | `~/.config/notify/config.json` |

## Commands

### `send` — Send a message

```
notify send [options]

  -m, --message <text>      Message body (or omit to read from --file or stdin)
  -f, --file <path>         Read message body from a file
  -t, --team <name|id>      Target team — overrides NOTIFY_TEAMS_DEFAULT_TEAM
  -c, --channel <name|id>   Target channel — overrides NOTIFY_TEAMS_DEFAULT_CHANNEL
      --subject <text>      Optional subject line shown above the message body
      --html                Treat the message body as HTML
      --dry-run             Print the resolved request without sending
  -q, --quiet               Suppress all output (exit code only)
      --env-file <path>     Load credentials from a key=value file
```

Exactly one of `--message`, `--file`, or piped stdin must supply the message body. If none are provided the command exits with code `1`.

### `list` — Discover teams and channels

```
notify list [options]

  (no --team)               List all teams the app has access to
  -t, --team <name|id>      List channels within the specified team
      --env-file <path>     Load credentials from a key=value file
```

Use `list` to find the exact names and GUIDs to use in your scripts.

### `configure` — Save default credentials and channel

```
notify configure [options]

  --tenant-id <guid>           Entra ID tenant GUID
  --client-id <guid>           App Registration client GUID
  --client-secret <secret>     App Registration client secret value
  --default-team <name|id>     Default team used when --team is not supplied
  --default-channel <name|id>  Default channel used when --channel is not supplied
```

### `version` — Show version info

```
notify version
```

Prints the binary version, .NET runtime, and OS.

## Examples

### Send a plain text message

```bash
notify send --team "DevOps" --channel "Alerts" -m "Deployment complete."
```

### Send a message with a subject line

```bash
notify send --team "DevOps" --channel "Alerts" \
  --subject "Build #42 Failed" \
  -m "Unit tests failed on main."
```

### Send an HTML message

```bash
notify send --team "DevOps" --channel "Alerts" --html \
  -m "<b>Deployment failed</b><br>Branch: <code>main</code>"
```

### Pipe output from another command

```bash
echo "Backup finished: $(date)" | notify send --channel "Alerts"
```

### Send the contents of a file

```bash
notify send --team "DevOps" --channel "Alerts" -f report.txt
```

### Send an HTML file

```bash
notify send --team "DevOps" --channel "Alerts" --html -f report.html
```

### Preview without sending

```bash
notify send --team "DevOps" --channel "Alerts" -m "Test" --dry-run
```

### Use an env file instead of environment variables

```bash
notify send --env-file ./notify.env -m "Done"
```

### Discover available teams and channels

```bash
notify list
notify list --team "DevOps"
```

## Using in CI/CD Pipelines

The general pattern for all platforms: store credentials as pipeline secrets, inject them as environment variables, call `notify send` as a step.

### GitHub Actions

```yaml
- name: Notify Teams
  env:
    NOTIFY_TEAMS_TENANT_ID:     ${{ secrets.NOTIFY_TEAMS_TENANT_ID }}
    NOTIFY_TEAMS_CLIENT_ID:     ${{ secrets.NOTIFY_TEAMS_CLIENT_ID }}
    NOTIFY_TEAMS_CLIENT_SECRET: ${{ secrets.NOTIFY_TEAMS_CLIENT_SECRET }}
  run: |
    notify send \
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
    - notify send
        --team "DevOps"
        --channel "Alerts"
        --subject "Pipeline $CI_PIPELINE_ID complete"
        --message "Branch: $CI_COMMIT_REF_NAME"
  variables:
    NOTIFY_TEAMS_TENANT_ID:     $NOTIFY_TEAMS_TENANT_ID
    NOTIFY_TEAMS_CLIENT_ID:     $NOTIFY_TEAMS_CLIENT_ID
    NOTIFY_TEAMS_CLIENT_SECRET: $NOTIFY_TEAMS_CLIENT_SECRET
```

Add the variables under **Settings → CI/CD → Variables** with the **Masked** flag set.

### Azure Pipelines

```yaml
- task: Bash@3
  displayName: Notify Teams
  env:
    NOTIFY_TEAMS_TENANT_ID:     $(NOTIFY_TEAMS_TENANT_ID)
    NOTIFY_TEAMS_CLIENT_ID:     $(NOTIFY_TEAMS_CLIENT_ID)
    NOTIFY_TEAMS_CLIENT_SECRET: $(NOTIFY_TEAMS_CLIENT_SECRET)
  inputs:
    targetType: inline
    script: |
      notify send \
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
            string(credentialsId: 'notify-tenant-id',     variable: 'NOTIFY_TEAMS_TENANT_ID'),
            string(credentialsId: 'notify-client-id',     variable: 'NOTIFY_TEAMS_CLIENT_ID'),
            string(credentialsId: 'notify-client-secret', variable: 'NOTIFY_TEAMS_CLIENT_SECRET')
        ]) {
            sh """
                notify send \\
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
chmod 600 /etc/notify/prod.env
```

```crontab
# Run nightly backup and notify on completion
0 2 * * * /usr/local/bin/backup.sh && \
  notify send \
    --env-file /etc/notify/prod.env \
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

notify send \
  --env-file /etc/notify/prod.env \
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
notify send --team "DevOps" --channel "Alerts" -m "Done"
if [ $? -ne 0 ]; then
  echo "Teams notification failed" >&2
fi
```

## Troubleshooting

**`error: authentication failed`** (exit code 2)
- Verify the tenant ID, client ID, and client secret are correct
- Check the client secret has not expired — regenerate it in the Azure portal if needed
- Confirm admin consent has been granted for `Team.ReadBasic.All` and `Channel.ReadBasic.All` in API permissions

**`error: No team found with the name '...'`** (exit code 3)
- Run `notify list` to see the exact team names visible to the app
- The app must be a member of the team — add it via **Manage team → Apps** in Teams
- Try passing the team GUID instead of the name to rule out name mismatch

**`error: No channel found with the name '...'`** (exit code 3)
- Run `notify list --team "..."` to see the exact channel names
- Private channels require the app to be explicitly added to that channel

**`error: graph api error`** (exit code 4)
- A 429 response means the Graph API is throttling — the SDK retries automatically but if retries are exhausted the command fails. Wait and retry.
- A 5xx response is a transient Graph API error — retry after a short delay

**`error: graph api error` when sending but `list` works** (exit code 4)
- **Known Microsoft bug (March 2026):** The `ChannelMessage.Send.Group` RSC permission is not currently honoured by the Graph API send endpoint. This is a confirmed Microsoft issue with escalation in progress — see [MicrosoftDocs/msteams-docs #14043](https://github.com/MicrosoftDocs/msteams-docs/issues/14043). Your configuration is correct; no changes are needed.
- If the bug has been resolved, verify the `notify` Teams app is installed in the target team via **Manage team → Apps**. Only a team owner can install apps.
