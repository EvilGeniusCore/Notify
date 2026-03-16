# `teams-notify` — Planning Document
> A C# / .NET 10 global CLI tool for sending Microsoft Teams notifications via the Graph API

## Purpose

This tool exists to send Teams messages to a specified channel or user so that scripts can be automated on servers in either Mac, Linux or Windows. It is NOT meant to replace built in integrations of platforms like Gitlab/Github or other platforms.

The consumers of the application would be any Admin, .NET user, or developer who needs to send Teams messages in a secured environment. This is an open source project distributed as an installable package. Existing tools did not provide the cross-platform ease of use required.

## Problems With the Alternatives

| Approach | Why It Falls Short |
|---|---|
| Raw `curl` + bash | Not cross platform and takes a lot of scaffolding to do right |
| Incoming Webhooks | Explored — see below |
| Power Automate | Microsoft-specific tool |
| Existing Graph SDK directly | Too complicated from shell |

### Incoming Webhooks — Explored and Rejected

Incoming Webhooks were seriously considered as they require no Azure AD App Registration and are trivial to implement — just an HTTP POST to a URL. However, they were rejected for the following reasons:

- **Deprecated.** Microsoft deprecated Office 365 Connectors in 2024. The replacement is Workflows via Power Automate, which is already ruled out as a Microsoft-specific dependency.
- **One URL per channel.** Each webhook is tied to a single channel. Managing multiple channels means managing multiple URLs, whereas a single Graph API App Registration covers every team and channel the app has access to.
- **No user messaging.** Webhooks cannot send direct messages to a user — the `--to` flag would be impossible to implement.
- **No channel discovery.** The `list` command cannot be built on webhooks — there is no API to enumerate teams or channels.
- **Sender identity.** Messages always appear from a connector bot name, not a real identity.

The Graph API requires a one-time Azure AD App Registration and admin consent for `ChannelMessage.Send`, but that cost is paid once per organisation and unlocks all current and future channels without additional configuration.

## Core Use Cases

| Priority | Use Case | Example | Notes |
|---|---|---|---|
| Required | Send to a known channel | `teams-notify send --team DevOps --channel Alerts -m "done"` | Required |
| Very Low | Send to a user by email | `teams-notify send --to user@company.com -m "done"` | If we can without a lot of work, I have other ways to send mail in scripts |
| Medium | Pipe stdin as the message | `echo "done" \| teams-notify send --channel Alerts` | Would be nice but not required |
| Required | Send with a title / subject | `--subject "Build #42 Failed"` | Must have |
| Low | Send an Adaptive Card | structured notification with buttons/fields | Future possibility, need a use case where its needed |
| High | Read config from a file | for reuse across multiple channels | Will allow reuse and a single config location |
| Low | Multiple recipients at once | `--to a@x.com --to b@x.com` | Multi-channel support is nice but not a requirement — could be done in the script itself |
| High | Dry run / preview mode | `--dry-run` prints what would be sent | Should have a method where I can test it in a vacuum |

## Out of Scope

- Receiving / reading messages
- Managing teams or channels (create, delete)
- Uploading files or attachments
- Scheduling messages
- A daemon / long-running listener

## CLI Design — Commands & Flags

A single command that can be run on Linux, Windows or Mac called `teams-notify`.

``` bash
teams-notify [command] [options]

Commands:
  send        Send a message
  configure   Save default config (tenant, client, channel)
  list        List teams and channels — run with no arguments to list all teams, add --team <name|id> to list channels within a team
  version     Show version info

Send options:
  -m, --message <text>      Message body — required unless --file or stdin is used
  -f, --file <path>         Read message body from a file — required unless --message or stdin is used
  -t, --team <name|id>      Target team
  -c, --channel <name|id>   Target channel
      --to <email>          Send to a user chat instead
      --subject <text>      Optional subject/title
      --html                Treat message body as HTML (Teams subset: bold, italic, lists, links, code blocks)
      --dry-run             Print request, don't send
  -q, --quiet               Suppress output (exit code only)

Global options (apply to all commands):
      --env-file <path>     Load credentials and defaults from a key=value file, overrides env vars
```

### Message input — required

Every `send` call must supply exactly one of:

- `-m, --message <text>` — inline string
- `-f, --file <path>` — path to a file whose contents become the message body
- stdin — piped input, used only when neither `--message` nor `--file` is provided

If none of these are present the command fails immediately with exit code `1` and a clear error. If more than one is provided, `--file` takes precedence over `--message`, and both take precedence over stdin.

`--html` applies regardless of which input source is used. Combine `--file report.html --html` to send a pre-built HTML file.

### Team and channel resolution

`--team` and `--channel` accept either a GUID (used directly as an ID) or a name (resolved via a Graph API lookup). If the lookup finds no match the command fails with exit code `3`. Use `teams-notify list` once to find IDs for use in production scripts where stability matters.

If `--team` and `--channel` are omitted the tool falls back to `TEAMS_NOTIFY_DEFAULT_TEAM` and `TEAMS_NOTIFY_DEFAULT_CHANNEL` from whichever config source is active. If a destination cannot be resolved from any source the command fails with exit code `5` — a destination is required.

## Authentication

This tool authenticates using Client Credentials (app-only) via an Azure AD App Registration. Managed Identity is not supported as this tool is designed for cross-platform script environments, not Azure-hosted infrastructure.

### Reasoning

- The primary use case is automating scripts on Linux, Mac and Windows servers with no user present — Client Credentials is the only option that works unattended
- Consumers are admins and developers running the tool in pipelines and cron jobs, not interactively
- Being open source means users supply their own Azure AD App Registration — credentials via env vars is the standard pattern for this
- Client Credentials works identically on Linux, Windows, and Mac with no platform-specific dependencies
- Device Code Flow adds first-run browser friction, which contradicts the goal of replacing tools that require too much scaffolding
- Managed Identity only works inside Azure, which this tool explicitly does not require

### Requirements

- Azure AD App Registration with `client_id` + `client_secret`
- Admin consent granted for the `ChannelMessage.Send` permission
- Credentials supplied via `--env-file`, environment variables, or config file — see Configuration Design

## Configuration Design

### Credential resolution order

The tool resolves credentials and defaults in this order, stopping at the first source that provides a value:

1. `--env-file <path>` — explicitly passed key=value file, takes precedence over everything. If you pass this you intend it to be the authoritative source, overriding any ambient environment variables on the machine.
2. Environment variables — set in the shell, crontab, or container. Used when no `--env-file` is provided.
3. Config file — platform-appropriate location resolved by .NET (`AppData\Roaming` on Windows, `~/.config` on Linux/Mac). Written by `teams-notify configure`. Used as a persistent fallback for developer machines.
4. Error — exits with code `5` if required credentials are still missing.

### Supported variables

```
TEAMS_NOTIFY_TENANT_ID
TEAMS_NOTIFY_CLIENT_ID
TEAMS_NOTIFY_CLIENT_SECRET
TEAMS_NOTIFY_DEFAULT_TEAM
TEAMS_NOTIFY_DEFAULT_CHANNEL
```

All variables apply regardless of source — `--env-file`, environment, or config file.

### Typical usage patterns

| Scenario | Recommended source |
|---|---|
| Running a script on a system or in a container | Environment variables injected by the platform |
| Cron job or script folder | `--env-file ./teams.env` alongside other script artifacts |
| Developer laptop | `teams-notify configure` writes the config file once |

## Technical Stack

### Core Libraries

| Role | Package | Notes |
|---|---|---|
| CLI framework | `System.CommandLine` | Stable in .NET 10, first-class MS support |
| Graph API | `Microsoft.Graph` v5 | Strongly typed, handles paging + retry |
| Azure Auth | `Azure.Identity` | Client Credentials via app registration |
| Configuration | `Microsoft.Extensions.Configuration` | + JSON + EnvVars providers |
| Logging | `Microsoft.Extensions.Logging` | Wire to console, respect `--quiet` |
| JSON | `System.Text.Json` | Already in the runtime |

### .NET 10 Features Worth Using

- **`[GeneratedRegex]`** — used for GUID detection when resolving `--team` and `--channel` values. The pattern is compiled into the binary at build time rather than at runtime. Not a performance requirement — the Graph API call that follows dwarfs any regex cost — but it is the correct modern .NET pattern and costs nothing to adopt.

### .NET Features We Are Avoiding

- **Native AOT** — `Microsoft.Graph` and `Azure.Identity` rely heavily on runtime reflection which is incompatible with AOT without significant annotation work that could break with SDK updates. The self-contained single file publish achieves no-runtime-dependency with none of the complexity.
- **`IHostedService` / Generic Host** — designed for long-running background services with a full DI container lifecycle. This tool executes a single command and exits. The Generic Host adds startup overhead and boilerplate that serves no purpose for a CLI with a sub-second lifetime.

## Project Structure

```
teams-notify/
├── src/
│   └── TeamsNotify/
│       ├── Program.cs              ← entry point, command wiring
│       ├── Commands/
│       │   ├── SendCommand.cs
│       │   ├── ConfigureCommand.cs
│       │   └── ListCommand.cs
│       ├── Services/
│       │   ├── GraphService.cs     ← Graph API calls
│       │   ├── AuthService.cs      ← credential resolution
│       │   └── ConfigService.cs    ← load/save config
│       ├── Models/
│       │   └── AppConfig.cs
│       └── TeamsNotify.csproj
├── tests/
│   └── TeamsNotify.Tests/
│       └── ...
├── README.md
└── teams-notify.sln
```

## Distribution

The CI build produces a self-contained single file executable per platform — all .NET dependencies and DLLs bundled into the binary. No .NET runtime required on the target machine. Drop it anywhere and run it. Documentation will be in the repository and referred to in the help.

### Build targets

All targets must be built with `PublishSingleFile=true` and `--self-contained true`.

- `win-x64` — `teams-notify.exe`
- `linux-x64` — `teams-notify`
- `linux-arm64` — `teams-notify`
- `osx-x64` — `teams-notify`
- `osx-arm64` — `teams-notify`

Build scripts (PowerShell and Bash) will be provided to automate publishing across all targets.

### As a .NET Global Tool (optional)

For users who already have the .NET runtime and prefer a smaller install:

```bash
dotnet tool install -g teams-notify
```

## Error Handling & Exit Codes

CI/CD pipelines depend on exit codes. Define them up front.

| Code | Meaning |
|---|---|
| `0` | Success |
| `1` | General error |
| `2` | Auth failure |
| `3` | Team/channel not found — value was provided as a name, Graph lookup returned no match |
| `4` | Graph API error (throttled 429, server error 5xx) |
| `5` | Configuration missing or destination cannot be resolved |

### Graph API throttling

The `Microsoft.Graph` SDK has built-in retry handling for 429 and 503 responses, including honouring the `Retry-After` header returned by the API. This is sufficient — no custom retry logic will be added. If the SDK exhausts its retry attempts the command fails with exit code `4`. This behaviour is documented so callers can handle it in their scripts if needed.

## Testing Strategy

Unit tests and integration tests are kept separate with a clear boundary between them.

### Unit tests

Test logic that does not require a network or credentials:

- Command argument parsing and validation (required fields, mutual exclusivity of `--message` / `--file` / stdin)
- Config resolution order (`--env-file` → env vars → config file → error)
- GUID detection for `--team` and `--channel` resolution
- Exit code mapping for each error condition
- `--dry-run` output formatting

No mocking of the Graph client. If a piece of logic requires mocking the Graph client to test it, that is a signal the logic should be separated from the API call.

### Integration tests

Run against a real Azure AD tenant and real Teams environment. These are not run in standard CI — they require credentials and are run manually or in a dedicated pipeline with secrets configured.

- `send` successfully delivers a message to a known channel
- `list` returns teams and channels for the configured app registration
- Auth failure produces exit code `2`
- Invalid team name produces exit code `3`
- `--env-file` credentials override environment variables

## HTML Specification — Teams Supported Tags

When `--html` is passed, the message body is sent with `contentType: html`. Teams renders the following HTML subset. Anything outside this list is stripped or ignored by Teams.

### Supported tags

| Tag | Renders as | Notes |
|---|---|---|
| `<b>`, `<strong>` | Bold | |
| `<i>`, `<em>` | Italic | |
| `<s>` | Strikethrough | |
| `<u>` | Underline | |
| `<pre>` | Code block | Monospace, preserves whitespace |
| `<code>` | Inline code | Monospace, inline |
| `<br>` | Line break | |
| `<p>` | Paragraph | Adds vertical spacing |
| `<ul>` / `<li>` | Unordered list | Bullet points |
| `<ol>` / `<li>` | Ordered list | Numbered list |
| `<blockquote>` | Quote block | Indented with left border |
| `<a href="...">` | Hyperlink | URL must be absolute |
| `<at>` | @mention | Teams-specific — requires the mention metadata object alongside the body |
| `<span>` | Inline container | Useful for combining with style attributes if Teams honours them |

### Unsupported / stripped

- Headings (`<h1>` through `<h6>`)
- Images (`<img>`)
- Tables (`<table>`, `<tr>`, `<td>`)
- CSS `style` attributes (generally ignored)
- `<script>`, `<iframe>`, and all other interactive/unsafe tags

### Example

```html
<b>Build Failed</b> — branch <code>main</code><br>
<ul>
  <li>Step: Unit Tests</li>
  <li>Exit code: 1</li>
</ul>
<a href="https://ci.example.com/builds/42">View build #42</a>
```
