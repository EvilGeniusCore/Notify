# `notify` — Planning Document
> A C# / .NET 10 CLI tool and reusable NuGet library for sending Microsoft Teams notifications via Power Automate webhooks

## Purpose

This tool exists to send Teams messages to a specified channel or user so that scripts can be automated on servers in either Mac, Linux or Windows. It is NOT meant to replace built in integrations of platforms like Gitlab/Github or other platforms.

The consumers of the application would be any Admin, .NET user, or developer who needs to send Teams messages in a secured environment. This is an open source project distributed in two forms: a cross-platform CLI tool for scripts and pipelines, and a NuGet library (`Notify.Teams`) for .NET applications that want to send Teams messages without shelling out to the CLI. Existing tools did not provide the cross-platform ease of use required, and no general-purpose Graph API wrapper for Teams messaging exists on NuGet.

## Problems With the Alternatives

| Approach | Why It Falls Short |
|---|---|
| Raw `curl` + bash | Not cross platform and takes a lot of scaffolding to do right |
| Graph API (app-only) | `ChannelMessage.Send` does not exist as an Application permission — delegated only. RSC workaround confirmed non-functional for regular messaging. See archived blocker analysis. |
| Bot Framework | Significant complexity — Azure Bot resource, two auth paths, proactive messaging bootstrapping. Rejected. |
| Service account (ROPC) | Deprecated auth flow, M365 licence cost, incompatible with MFA. Rejected. |

### Why Webhook (Power Automate)

The original design targeted the Graph API with an Entra ID App Registration. After full implementation and testing against a real tenant, `notify send` fails with a permission error that is a confirmed permanent platform limitation — not a bug. Microsoft Graph does not support app-only channel messaging for regular use. No supported fix is forthcoming.

The Power Automate webhook is the only remaining path that:
- Works today without IT escalation
- Requires no Entra ID App Registration for sending
- Is the established industry standard (GitLab, GitHub, ArgoCD, Atlassian all use this path)

**Trade-offs accepted:**
- One URL per channel — each target channel requires its own webhook URL managed as a secret
- No channel discovery — the target is encoded in the URL; `notify list` is not applicable to the send path
- Power Automate dependency — Microsoft controls the relay; standard M365 licences include the HTTP trigger connector

See [Providers/teams/archive/notify-teams-channelmessage-blocker.md](Providers/teams/archive/notify-teams-channelmessage-blocker.md) for the full analysis of all four options.

## Core Use Cases

| Priority | Use Case | Example | Notes |
|---|---|---|---|
| Required | Send to a channel via webhook | `notify send --webhook https://... -m "done"` | Primary send path |
| Required | Send with a title / subject | `--subject "Build #42 Failed"` | Must have |
| High | Read config from a file | `--env-file ./notify.env` | Webhook URL stored in env file alongside scripts |
| Medium | Pipe stdin as the message | `echo "done" \| notify send` | Useful in pipelines |
| High | Dry run / preview mode | `--dry-run` prints what would be sent | For testing without sending |
| Low | Send an Adaptive Card | structured notification with buttons/fields | Future — needs a concrete use case |

## Out of Scope

- Receiving / reading messages
- Managing teams or channels (create, delete)
- Uploading files or attachments
- Scheduling messages
- A daemon / long-running listener

## CLI Design — Commands & Flags

A single command that can be run on Linux, Windows or Mac called `notify`.

```bash
notify [command] [options]

Commands:
  send        Send a message to a Teams channel
  configure   Save default config (webhook URL)
  version     Show version info

Send options:
  -m, --message <text>      Message body — required unless --file or stdin is used
  -f, --file <path>         Read message body from a file — required unless --message or stdin is used
      --webhook <url>       Webhook URL to post to — overrides NOTIFY_TEAMS_WEBHOOK_URL
      --subject <text>      Optional subject/title
      --html                Treat message body as HTML (Teams subset: bold, italic, lists, links, code blocks)
      --dry-run             Print the payload, don't send
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

### Webhook URL resolution

The webhook URL is the target channel credential. It is resolved in this order:

1. `--webhook <url>` — explicit per-invocation override
2. `NOTIFY_TEAMS_WEBHOOK_URL` — from `--env-file`, environment variable, or config file

If no webhook URL can be resolved the command fails with exit code `5`.

## Authentication

No Entra ID App Registration is required for sending. The webhook URL is the credential — it encodes the target channel and acts as a shared secret. Treat it like a password: store it in an env file or CI/CD secret, never in source control.

The URL is obtained by the channel owner from the Power Automate Workflows connector in Teams. See [Providers/teams/Notify.Teams.md](Providers/teams/Notify.Teams.md) for setup steps.

## Configuration Design

### Credential resolution order

The tool resolves credentials and defaults in this order, stopping at the first source that provides a value:

1. `--env-file <path>` — explicitly passed key=value file, takes precedence over everything. If you pass this you intend it to be the authoritative source, overriding any ambient environment variables on the machine.
2. Environment variables — set in the shell, crontab, or container. Used when no `--env-file` is provided.
3. Config file — platform-appropriate location resolved by .NET (`AppData\Roaming` on Windows, `~/.config` on Linux/Mac). Written by `notify configure`. Used as a persistent fallback for developer machines.
4. Error — exits with code `5` if required credentials are still missing.

### Supported variables

```
NOTIFY_TEAMS_WEBHOOK_URL     Power Automate webhook URL for the target channel
```

All variables apply regardless of source — `--env-file`, environment, or config file.

### Typical usage patterns

| Scenario | Recommended source |
|---|---|
| Running a script on a system or in a container | Environment variables injected by the platform |
| Cron job or script folder | `--env-file ./teams.env` alongside other script artifacts |
| Developer laptop | `notify configure` writes the config file once |

## Technical Stack

### `Notify.Core` — Provider Abstractions

| Role | Package | Notes |
|---|---|---|
| Provider interface | (none) | Pure abstractions — no external dependencies |

### `Notify.Teams` — Teams Provider Dependencies

| Role | Package | Notes |
|---|---|---|
| HTTP client | `System.Net.Http` | Already in the runtime — `HttpClient` for webhook POST |
| Logging | `Microsoft.Extensions.Logging.Abstractions` | Abstractions only — consumers supply the implementation |
| JSON | `System.Text.Json` | Already in the runtime — payload serialisation |

### `Notify` — CLI-Only Dependencies

| Role | Package | Notes |
|---|---|---|
| CLI framework | `System.CommandLine` | Stable in .NET 10, first-class MS support |
| Configuration | `Microsoft.Extensions.Configuration` | + JSON + EnvVars providers |
| Logging impl | `Microsoft.Extensions.Logging.Console` | Wires console output, respects `--quiet` |

`Microsoft.Graph` and `Azure.Identity` are pulled in transitively from `Notify.Teams` — the CLI does not reference them directly.

### .NET 10 Features Worth Using

- **`[GeneratedRegex]`** — used for GUID detection when resolving `--team` and `--channel` values. The pattern is compiled into the binary at build time rather than at runtime. Not a performance requirement — the Graph API call that follows dwarfs any regex cost — but it is the correct modern .NET pattern and costs nothing to adopt.

### .NET Features We Are Avoiding

- **Native AOT** — `Microsoft.Graph` and `Azure.Identity` rely heavily on runtime reflection which is incompatible with AOT without significant annotation work that could break with SDK updates. The self-contained single file publish achieves no-runtime-dependency with none of the complexity.
- **`IHostedService` / Generic Host** — designed for long-running background services with a full DI container lifecycle. This tool executes a single command and exits. The Generic Host adds startup overhead and boilerplate that serves no purpose for a CLI with a sub-second lifetime.

## Project Structure

Three projects ship from this repository: a provider abstraction library (`Notify.Core`), the Teams notification provider (`Notify.Teams`), and the CLI tool (`Notify`) that wires them together.

```
notify/
├── src/
│   ├── Notify.Core/                    ← Provider abstractions (INotificationProvider)
│   │   └── Abstractions/
│   │       └── INotificationProvider.cs
│   ├── Notify.Teams/                   ← NuGet library (Notify.Teams)
│   │   ├── Models/
│   │   │   ├── WebhookCredentials.cs   ← record: WebhookUrl
│   │   │   └── SendMessageRequest.cs   ← record: Body, IsHtml, Subject
│   │   ├── Services/
│   │   │   └── WebhookService.cs       ← POSTs payload to webhook URL via HttpClient
│   │   └── Notify.Teams.csproj         ← deps: Logging.Abstractions (no Graph, no Azure.Identity)
│   └── Notify/                         ← CLI tool (dotnet global tool + self-contained binaries)
│       ├── Program.cs                  ← entry point, command wiring
│       ├── Commands/
│       │   ├── SendCommand.cs
│       │   └── ConfigureCommand.cs
│       ├── Models/
│       │   └── AppConfig.cs            ← CLI config model; holds WebhookUrl
│       ├── Services/
│       │   └── ConfigService.cs        ← loads credentials into AppConfig (--env-file / env / file)
│       └── Notify.csproj               ← deps: System.CommandLine, Configuration, ProjectRef→Core, Teams
├── tests/
│   ├── Notify.Teams.Tests/             ← unit tests for Teams provider library
│   └── Notify.Tests/                   ← unit tests for CLI (arg parsing, config resolution)
├── Documentation/
│   ├── notify-planning.md
│   ├── notify-cli-manual.md
│   └── Providers/
│       └── teams/
│           ├── Notify.Teams.md         ← Teams provider docs (webhook setup, config, usage)
│           └── archive/                ← RSC/Graph API analysis — historical reference
├── CLAUDE.md
├── README.md
└── notify.slnx
```

### Separation of concerns

| Layer | Project | Knows about |
|---|---|---|
| Provider abstraction | `Notify.Core` | `INotificationProvider` interface only |
| Webhook send logic | `Notify.Teams` | `HttpClient`, `WebhookCredentials`, `SendMessageRequest` |
| Config loading | `Notify` (`ConfigService`) | env vars, files, platform paths → `AppConfig` → `WebhookCredentials` |
| CLI surface | `Notify` (`Commands/*`) | `System.CommandLine`, `ConfigService`, Teams provider services |

`Notify.Teams` has zero CLI dependencies and can be consumed directly by any .NET application via NuGet without shelling out to the CLI.

## Build Plan

Ordered by dependency — each phase can begin once the previous is complete.

### Phase 1 — Core Library (`Notify.Teams`)

- [ ] `WebhookCredentials` — replace `TeamsCredentials`; record with single `WebhookUrl` required property
- [x] `SendMessageRequest` — record: `Body`, `IsHtml`, `Subject` — remove `TeamId`/`ChannelId`
- [ ] `WebhookService` — implement `SendMessageAsync(SendMessageRequest, WebhookCredentials)` — POST JSON payload to webhook URL via `HttpClient`
- [ ] Remove `AuthService`, `GraphService` — no longer needed for send path
- [ ] `Notify.Teams.csproj` — remove `Microsoft.Graph` and `Azure.Identity` dependencies
- [ ] `Notify.Teams.Tests` — unit tests: payload serialisation, HTTP error handling

### Phase 2 — CLI (`Notify`)

- [ ] `AppConfig` — replace `TenantId`/`ClientId`/`ClientSecret`/`DefaultTeam`/`DefaultChannel` with `WebhookUrl`
- [ ] `AppConfig.Validate()` — assert `WebhookUrl` is non-null
- [x] `ConfigService.LoadAsync()` — resolve credentials from `--env-file` → env vars → config file → exit code `5`
- [x] `ConfigService.SaveAsync()` / `GetConfigFilePath()` — persist defaults written by `notify configure`
- [x] `Program.cs` — wire `--env-file` as a global option and thread it through to `ConfigService`
- [ ] `SendCommand` — add `--webhook` option; remove `--team`/`--channel`; wire to `WebhookService`
- [ ] Remove `ListCommand` — no longer applicable to webhook-based send path
- [ ] `ConfigureCommand` — update to save `WebhookUrl` instead of tenant/client/secret/channel defaults
- [x] Version command — output assembly version and target framework
- [ ] `Notify.Tests` — update unit tests: arg parsing, config resolution, exit code mapping, `--dry-run` output

### Phase 3 — Polish & Release

- [ ] Review all `--help` text across every command for clarity and completeness
- [x] `Build.ps1` and `Build.sh` — automate self-contained publish for all five platform targets
- [x] Smoke test self-contained binaries on Windows, Linux, and macOS
- [ ] Integration tests against a real Teams channel via webhook
- [ ] `Notify.Teams` NuGet metadata review — description, tags, package README
- [ ] Repository README — update for webhook-based setup, remove Entra ID / RSC references
- [ ] Update `TestKit/` — replace Graph API env vars with webhook URL, update test scripts

### Phase 4 — RSC Teams App (abandoned)

The RSC path was fully implemented and tested but is confirmed permanently blocked. Microsoft Graph does not support app-only channel messaging for regular use. All RSC-related artefacts (`TeamsApp/`) are retained in the repository as historical reference but are no longer part of the active build or release.

See [Providers/teams/archive/notify-teams-channelmessage-blocker.md](Providers/teams/archive/notify-teams-channelmessage-blocker.md) for the full history.

## Distribution

Two artifacts are published from this repository.

### `Notify.Teams` — NuGet Library

Published to NuGet.org as `Notify.Teams`. Consumed by .NET applications that want to send Teams messages directly without the CLI:

```bash
dotnet add package Notify.Teams
```

### `Notify` — CLI Tool

Distributed in two forms:

**Self-contained binaries** — all .NET dependencies bundled into a single executable. No .NET runtime required on the target machine. Drop it anywhere and run it.

All targets built with `PublishSingleFile=true` and `--self-contained true`:

- `win-x64` — `notify.exe`
- `linux-x64` — `notify`
- `linux-arm64` — `notify`
- `osx-x64` — `notify`
- `osx-arm64` — `notify`

Build scripts (PowerShell and Bash) will be provided to automate publishing across all targets.

**dotnet global tool** — for users who already have the .NET runtime and prefer a smaller install:

```bash
dotnet tool install -g notify
```

## Error Handling & Exit Codes

CI/CD pipelines depend on exit codes. Define them up front.

| Code | Meaning |
|---|---|
| `0` | Success |
| `1` | General error |
| `2` | HTTP error posting to webhook (4xx/5xx from Power Automate) |
| `5` | Configuration missing — webhook URL not set |

### HTTP error handling

`notify` POSTs to the webhook URL and checks the HTTP response code. A non-success response (4xx/5xx) fails with exit code `2`. No automatic retry — the caller is responsible for retry logic in their script if needed.

## Testing Strategy

Unit tests and integration tests are kept separate with a clear boundary between them. Two test projects map to the two source projects.

### `Notify.Teams.Tests` — Library unit tests

Test Teams provider logic that does not require a network or credentials:

- GUID detection for `--team` and `--channel` resolution
- Name-to-ID resolution logic (input parsing, error cases)
- `AppConfig` validation (missing required fields)

No mocking of the Graph client. If a piece of logic requires mocking the Graph client to test it, that is a signal the logic should be separated from the API call.

### `Notify.Tests` — CLI unit tests

Test CLI behaviour that does not require a network or credentials:

- Command argument parsing and validation (required fields, mutual exclusivity of `--message` / `--file` / stdin)
- Config resolution order (`--env-file` → env vars → config file → error)
- Exit code mapping for each error condition
- `--dry-run` output formatting

### Integration tests

Run against a real Entra ID tenant and real Teams environment. These are not run in standard CI — they require credentials and are run manually or in a dedicated pipeline with secrets configured.

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

## Teams Send Path — Resolved

The original Graph API design was abandoned after confirming that Microsoft does not support app-only channel messaging for regular use. Four options were evaluated; the Power Automate webhook is the chosen send path.

Full analysis: [Providers/teams/archive/notify-teams-channelmessage-blocker.md](Providers/teams/archive/notify-teams-channelmessage-blocker.md)

## Future Considerations

Items that are explicitly out of scope for v1 but worth revisiting in later versions.

### Adaptive Cards — `Microsoft.Teams.Cards`

The official Microsoft package [`Microsoft.Teams.Cards`](https://www.nuget.org/packages/Microsoft.Teams.Cards) (Dec 2025, .NET 8+) builds Adaptive Card payloads for Teams, including Teams-specific card elements. It does not send — it is a payload builder only.

When the `--card` flag is implemented, this package should be evaluated as the card payload builder rather than rolling custom DTOs. It is actively maintained by Microsoft and targets .NET 8+, making it compatible with this project.

### File Payloads

Sending file attachments to a Teams channel or chat via the Graph API. The Graph API supports this but it is a multi-step operation: upload the file to SharePoint/OneDrive first, then attach the resulting share link or file ID to the message. This is non-trivial and has no current use case driving it, but it is a natural extension of the `send` command — e.g. `--attach report.pdf`.

This is currently listed as out of scope. Revisit when a concrete use case exists.
