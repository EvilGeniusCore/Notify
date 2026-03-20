# Notify — Teams Provider

Sends messages to Microsoft Teams channels via Power Automate webhook URLs. No Entra ID App Registration required for sending.

## How it works

A team member creates a Power Automate workflow with an HTTP trigger in the target Teams channel. The workflow generates a URL. `notify` POSTs a JSON payload to that URL. The workflow delivers the message to the channel.

Each webhook URL is scoped to one channel. The URL is a credential — treat it like a password.

## Setup — creating a webhook URL in Teams

1. Open the target channel in Teams
2. Click **...** → **Workflows**
3. Search for **"Send webhook alerts to channel"** and select it
4. Give the workflow a name (e.g. `notify`), select the team and channel, click **Save**
5. Copy the generated URL — this is your `NOTIFY_TEAMS_WEBHOOK_URL`

The workflow is created under the account of the person who set it up. Any team member can do this — team owner is not required.

## Configuration

Set the webhook URL via environment variable, `--env-file`, or the `notify configure` command.

**Environment variable:**
```
NOTIFY_TEAMS_WEBHOOK_URL=https://prod2-xx.region.logic.azure.com/...
```

**notify.env file:**
```
NOTIFY_TEAMS_WEBHOOK_URL=https://prod2-xx.region.logic.azure.com/...
```

**CLI option (per-invocation override):**
```
notify send --message "text" --webhook https://...
```

## Sending a message

```bash
# Using configured webhook URL
notify send --message "Backup completed successfully."

# Specifying the webhook URL directly
notify send --message "Backup completed." --webhook https://prod2-xx.region.logic.azure.com/...

# With a subject line
notify send --subject "Nightly Backup" --message "All volumes backed up. Duration: 4m 12s."

# From stdin
echo "Deploy complete." | notify send

# From a file
notify send --file /var/log/backup-summary.txt

# Dry run — prints the payload without sending
notify send --message "test" --dry-run
```

## Payload format

`notify` POSTs a MessageCard payload to the webhook URL:

```json
{
  "@type": "MessageCard",
  "@context": "http://schema.org/extensions",
  "summary": "subject or first line of body",
  "title": "optional subject line",
  "text": "message body"
}
```

`title` is omitted when no `--subject` is provided. `summary` is always included — it is used by Teams for notification previews.

## Exit codes

| Code | Meaning |
|---|---|
| `0` | Message delivered |
| `1` | General error |
| `2` | Webhook HTTP error (4xx/5xx from Power Automate) |
| `5` | Webhook URL not configured |

## Message presentation — future: template support

The MessageCard format supports richer presentation beyond title and body text:

- **`themeColor`** — hex colour for the left accent bar on the card. Useful for status at a glance (e.g. `FF0000` for failure, `00CC00` for success)
- **`facts`** — a list of `{ name, value }` pairs rendered as a two-column table inside the card. Good for structured data like build number, branch, duration
- **`activityImage`** — a publicly accessible image URL displayed in the card

These properties are not yet exposed by the CLI. The intended design is a `--template <path>` option accepting a provider-specific JSON file that defines the card shape — colour, facts, image, etc. The CLI handles content (`--message`, `--subject`); the template handles presentation. This keeps the CLI surface clean and provider-agnostic: other providers (Matrix, Discord) would have their own template schemas but the same `--template` flag.

Variable substitution in templates (injecting dynamic values into fact fields etc.) is intentionally out of scope — the expectation is that the caller generates the template file with values already filled in using standard shell tooling before invoking `notify`.

This feature is not yet scheduled. Implement when a concrete use case drives it.

## Exit codes

| Code | Meaning |
|---|---|
| `0` | Message delivered |
| `1` | General error |
| `5` | Webhook URL not configured |

## Limitations

- One channel per invocation. To post to multiple channels, invoke `notify send` once per URL.
- The webhook URL is scoped to a single channel. There is no channel discovery or name resolution — the target is encoded in the URL.
- Power Automate may require a licence depending on the organisation's M365 plan. Standard M365 Business plans typically include the HTTP trigger connector.
- The workflow is owned by the user who created it. If that user leaves the organisation or their account is deactivated, the workflow may stop functioning.

## Background — why not Graph API?

Microsoft Graph does not support app-only channel message sending for regular use. The `ChannelMessage.Send` permission is delegated-only (requires a signed-in user). The RSC permission `ChannelMessage.Send.Group` is restricted to data migration scenarios (`Teamwork.Migrate.All`) and does not function for standard messaging.

Full analysis and history: [archive/notify-teams-channelmessage-blocker.md](archive/notify-teams-channelmessage-blocker.md)
