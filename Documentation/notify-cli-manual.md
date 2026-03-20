# notify — CLI User Manual
> For admins, developers, and script writers who need to send Microsoft Teams messages from the command line, scripts, CI/CD pipelines, or cron jobs.

## Overview

`notify` is a cross-platform CLI tool for sending messages to Microsoft Teams channels. It runs on Windows, Linux, and macOS with no .NET runtime required and is designed to be dropped into any script or pipeline with minimal setup.

## Requirements

- A Power Automate webhook URL for each Teams channel you want to post to
- One-time configuration of the webhook URL (env var, env file, or `notify configure`)

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

## Setting Up a Webhook URL in Teams

Each channel you want to post to requires its own webhook URL. Any team member can create one — team owner is not required.

1. Open the target channel in Teams
2. Click **...** → **Workflows**
3. Search for **"Send webhook alerts to a channel"** and select it
4. Give the workflow a name (e.g. `notify`) and click **Next**
5. Confirm the team and channel, click **Add workflow**
6. Copy the generated URL — this is your `NOTIFY_TEAMS_WEBHOOK_URL`

The URL is a credential. Treat it like a password — store it in an env file or CI/CD secret, never in source control.

## Configuration

Credentials are resolved in this order, stopping at the first source that provides a value:

1. `--env-file <path>` — explicit key=value file passed on the command line
2. Environment variables — set in the shell, container, or CI/CD platform
3. Config file — written by `notify configure`, stored in the platform config directory
4. Error — exits with code `5` if required credentials are missing

### Environment variables

```
NOTIFY_TEAMS_WEBHOOK_URL     Power Automate webhook URL for the target channel
```

### Using an env file

An env file is a plain text file with one `KEY=VALUE` per line. Lines starting with `#` are ignored.

```
# notify.env
NOTIFY_TEAMS_WEBHOOK_URL=https://prod2-xx.region.logic.azure.com/...
```

```bash
notify send --env-file ./notify.env --message "Done"
```

If the file is named `notify.env` and is in the current directory, it is loaded automatically without `--env-file`.

Keep the env file out of source control — add `notify.env` to your `.gitignore`.

### Saving a default configuration

`notify configure` saves the webhook URL to the platform config file so you don't need to set environment variables on your developer machine.

```bash
notify configure --webhook-url https://prod2-xx.region.logic.azure.com/...
```

Run `configure` again at any time to update the saved URL.

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
      --webhook <url>       Webhook URL — overrides NOTIFY_TEAMS_WEBHOOK_URL
      --subject <text>      Optional subject line shown above the message body
      --html                Treat the message body as HTML
      --dry-run             Print the payload without sending
  -q, --quiet               Suppress all output (exit code only)
      --env-file <path>     Load credentials from a key=value file
```

Exactly one of `--message`, `--file`, or piped stdin must supply the message body. If none are provided the command exits with code `1`.

### `configure` — Save default webhook URL

```
notify configure [options]

  --webhook-url <url>    Power Automate webhook URL to save as the default
```

### `version` — Show version info

```
notify version
```

Prints the binary version, .NET runtime, and OS.

## Examples

### Send a plain text message

```bash
notify send -m "Deployment complete."
```

### Send a message with a subject line

```bash
notify send --subject "Build #42 Failed" -m "Unit tests failed on main."
```

### Send an HTML message

```bash
notify send --html -m "<b>Deployment failed</b><br>Branch: <code>main</code>"
```

### Pipe output from another command

```bash
echo "Backup finished: $(date)" | notify send
```

### Send the contents of a file

```bash
notify send -f report.txt
```

### Send an HTML file

```bash
notify send --html -f report.html
```

### Preview without sending

```bash
notify send -m "Test" --dry-run
```

### Use an env file instead of environment variables

```bash
notify send --env-file ./notify.env -m "Done"
```

### Override the webhook URL per invocation

```bash
notify send --webhook https://prod2-xx.region.logic.azure.com/... -m "Done"
```

## Using in CI/CD Pipelines

The general pattern for all platforms: store credentials as pipeline secrets, inject them as environment variables, call `notify send` as a step.

### GitHub Actions

```yaml
- name: Notify Teams
  env:
    NOTIFY_TEAMS_WEBHOOK_URL: ${{ secrets.NOTIFY_TEAMS_WEBHOOK_URL }}
  run: |
    notify send \
      --subject "Build ${{ github.run_number }} complete" \
      --message "Branch: ${{ github.ref_name }}"
```

Add the secret under **Repository → Settings → Secrets and variables → Actions**.

### GitLab CI

```yaml
notify-teams:
  stage: notify
  script:
    - notify send
        --subject "Pipeline $CI_PIPELINE_ID complete"
        --message "Branch: $CI_COMMIT_REF_NAME"
  variables:
    NOTIFY_TEAMS_WEBHOOK_URL: $NOTIFY_TEAMS_WEBHOOK_URL
```

Add the variable under **Settings → CI/CD → Variables** with the **Masked** flag set.

### Azure Pipelines

```yaml
- task: Bash@3
  displayName: Notify Teams
  env:
    NOTIFY_TEAMS_WEBHOOK_URL: $(NOTIFY_TEAMS_WEBHOOK_URL)
  inputs:
    targetType: inline
    script: |
      notify send \
        --subject "Build $(Build.BuildNumber) complete" \
        --message "Branch: $(Build.SourceBranchName)"
```

Add the variable under **Pipelines → Library → Variable groups** and link the group to the pipeline.

### Jenkins

```groovy
stage('Notify Teams') {
    steps {
        withCredentials([
            string(credentialsId: 'notify-webhook-url', variable: 'NOTIFY_TEAMS_WEBHOOK_URL')
        ]) {
            sh """
                notify send \\
                  --subject "Build ${env.BUILD_NUMBER} complete" \\
                  --message "Branch: ${env.BRANCH_NAME}"
            """
        }
    }
}
```

## Using in Cron Jobs

Use `--env-file` to keep the webhook URL out of the crontab entirely. Store the env file in a location only readable by the user running the cron job.

```bash
chmod 600 /etc/notify/prod.env
```

```crontab
# Run nightly backup and notify on completion
0 2 * * * /usr/local/bin/backup.sh && \
  notify send \
    --env-file /etc/notify/prod.env \
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
  --message "$MSG" \
  --quiet

exit $EXIT
```

## Exit Codes

| Code | Meaning |
|---|---|
| `0` | Success |
| `1` | General error (including no message provided) |
| `2` | HTTP error from webhook — 4xx or 5xx response from Power Automate |
| `5` | Configuration missing — webhook URL not set |

Scripts should check `$?` (Linux/macOS) or `%ERRORLEVEL%` (Windows) after each call.

```bash
notify send -m "Done"
if [ $? -ne 0 ]; then
  echo "Teams notification failed" >&2
fi
```

## Troubleshooting

**`error: webhook URL not configured`** (exit code 5)
- Set `NOTIFY_TEAMS_WEBHOOK_URL` in your env file, environment, or run `notify configure --webhook-url <url>`
- If using `--env-file`, confirm the file path is correct and the variable name is spelled correctly

**`error: webhook request failed` with a 4xx response** (exit code 2)
- The URL may have expired or been deleted — regenerate it in Teams via **Workflows**
- Confirm the URL was copied in full — Power Automate URLs are long and easy to truncate

**`error: webhook request failed` with a 5xx response** (exit code 2)
- Transient Power Automate error — retry after a short delay

**Message not appearing in the channel**
- Check the Power Automate workflow run history — the flow may have received the request but failed internally
- Confirm the workflow is still active and the team/channel still exists
