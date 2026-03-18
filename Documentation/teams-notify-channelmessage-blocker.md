# Known Blocker — `ChannelMessage.Send` Does Not Exist as an Application Permission

## Problem

The original design assumes app-only (Client Credentials) authentication using the `ChannelMessage.Send` **Application permission** in Microsoft Graph. This design is fundamentally flawed.

Per the [Microsoft Graph permissions reference](https://learn.microsoft.com/en-us/graph/permissions-reference), `ChannelMessage.Send` is a **Delegated permission only**:

> *"Allows an app to send channel messages in Microsoft Teams, on behalf of the signed-in user."*

There is no Application permission equivalent. Microsoft has never exposed app-only channel message sending as a standard tenant-wide Graph permission. The permission not appearing in the Azure portal during setup was not a gating or consent issue — it does not exist. The entire `ClientSecretCredential` auth path in `AuthService` and `GraphService` will fail at runtime for the send operation regardless of what permissions are granted.

This is a **design-level v1 blocker**, not a configuration issue. The codebase must be updated before integration testing or shipping.

## Proposed Solutions

### Option 1 — RSC (Resource-Specific Consent)

Package the app as a Teams application and install it directly into each team it needs to post to. RSC provides a `ChannelMessage.Send.Group` permission scoped to that specific team, which does work app-only without a signed-in user. This is Microsoft's supported path for unattended app-only channel messaging.

**What changes in the code:**
- A Teams app manifest (`manifest.json`) must be created declaring the `ChannelMessage.Send.Group` RSC permission
- The manifest must be packaged as a `.zip` and sideloaded or published to the organisation's Teams app catalogue
- `AuthService` stays on Client Credentials — the auth mechanism does not change
- `GraphService.SendMessageAsync()` may need to confirm RSC-granted permissions are honoured by the standard channel message endpoint — this requires verification during integration testing

**Fallout:**
- The app must be manually installed into every team it needs to post to — this is ongoing operational overhead
- Sideloading requires IT/admin involvement for each team, which brings the Technical Services Committee into every new deployment
- Teams app packages have their own versioning and update lifecycle separate from the CLI binary and NuGet package
- Breaks the original "drop credentials in, point at any team" design goal — the app must already be installed in a team before it can post there
- Not suitable as a truly general-purpose tool across an organisation without a centralised app catalogue deployment

**Verdict:** The most viable path. Client Credentials auth is preserved, no licence cost, and it is Microsoft's documented supported approach for this scenario. The operational overhead is the main trade-off.

### Option 2 — Service Account (Delegated Permissions)

Create a dedicated Microsoft 365 user account (e.g. `teams-notify@organisation.com`), grant it the Delegated `ChannelMessage.Send` permission, and authenticate as that user using the Resource Owner Password Credentials (ROPC) flow — the only non-interactive delegated flow suitable for unattended automation.

A variation of this option is using an existing personal account rather than a dedicated service account. This avoids the licence cost but introduces its own problems — see fallout below.

**What changes in the code:**
- `AuthService` must be rewritten to use `UsernamePasswordCredential` from `Azure.Identity` instead of `ClientSecretCredential`
- `TeamsCredentials` must be extended to carry a username and password instead of a client secret
- All environment variable names and config file keys change — a breaking change for any consumer of `TeamsNotify.Core`
- All documentation, README, CLI manual, and the NuGet package readme require updates

**Fallout:**
- A dedicated M365 user licence costs money annually — explicitly identified as undesirable
- ROPC is deprecated by Microsoft and not recommended for new applications
- Storing a username and password as credentials is a weaker security model than Client Credentials
- MFA policies and password expiry on the service account can silently break unattended scripts with no warning
- Messages appear to come from the named user account rather than a neutral app identity, which looks odd in Teams
- Breaks the app-only design goal entirely

**Using a personal account instead of a dedicated service account:**
- Avoids the licence cost but every automated message appears in Teams as posted by that person personally
- MFA is almost certainly enabled on personal accounts in a government-managed tenant — ROPC does not support MFA and will fail outright
- The individual's password is stored in plaintext in env files and CI/CD secrets — a significant personal security risk
- Any password change, account lock, or policy rotation by IT immediately breaks all automation silently
- Government tenants typically enforce strict password rotation policies — this will fail on a predictable schedule
- Ties organisational automation to a single person's account — if they leave, everything breaks

**Verdict:** Not recommended in either form. Dedicated service account carries licence cost and deprecated auth. Personal account is worse — MFA incompatibility alone makes it non-viable on a government tenant, and it is poor security practice regardless.

### Option 3 — Bot Framework (Azure Bot)

Register an Azure Bot via the Azure portal, connect it to Teams, and install it in each target team. The Bot Framework has its own token endpoint and its own permission model — entirely separate from Graph API application permissions. It was designed specifically for automated/unattended messaging scenarios and is Microsoft's primary supported path for bots sending proactive messages to channels.

This is how most third-party tools that post to Teams programmatically without a signed-in user actually work under the hood.

**What changes in the code:**
- `AuthService` must be rewritten or extended to acquire Bot Framework tokens (`https://api.botframework.com`) using the bot's App ID and password, rather than Client Credentials against Graph
- `GraphService.SendMessageAsync()` must be replaced with Bot Framework REST API calls — the endpoint and payload format are different from Graph
- `TeamsCredentials` must be updated to carry a Bot App ID and Bot App Password instead of (or alongside) tenant/client/secret
- The `list` and name-resolution features still use Graph API and would retain the existing `AuthService`/`GraphService` path — this means the codebase would carry two auth paths
- All documentation, README, CLI manual, and NuGet package readme require updates

**Fallout:**
- Requires creating and configuring an Azure Bot resource in the Azure portal — additional one-time setup beyond the App Registration
- The bot must still be installed in each team it needs to post to — same operational overhead as RSC
- Two auth paths in the codebase adds complexity — Bot Framework for send, Client Credentials + Graph for list/resolve
- Bot Framework proactive messaging requires storing a conversation reference for each channel, which means the bot must first be installed and a conversation initiated before it can post — adds a bootstrapping step
- Messages appear to come from the bot identity (a named bot) rather than a neutral app, which may look different in Teams depending on how the bot is configured
- Azure Bot is an additional Azure resource to manage, monitor, and keep credentials for

**Verdict:** Viable and Microsoft-supported, but adds significant complexity over RSC — two auth paths, an additional Azure resource, and a proactive messaging bootstrapping requirement. RSC is simpler for this use case. Worth revisiting if RSC proves unworkable in practice.

### Option 4 — Webhook (Power Automate Workflow HTTP Trigger)

This is the same concept used by GitLab, GitHub, and most CI/CD platforms for their built-in Teams notification integrations — POST a JSON payload to a URL and Teams receives the message. No Entra ID App Registration, no Graph API, no RSC, no bot.

**Important distinction on the old vs new webhook:** GitLab and similar tools historically posted to **Office 365 Connector webhook URLs** — a URL Teams provided natively with no external dependency. Microsoft deprecated those connectors in 2024:

| Date | Event |
|---|---|
| August 15, 2024 | Creation of new connector webhooks blocked |
| January 31, 2025 | Existing URLs had to be migrated to new format or stopped working (403 errors) |
| April 30, 2026 | Hard shutdown — all remaining connector webhooks stop working |

New connector webhooks cannot be created. The direct replacement Microsoft provides is a **Power Automate workflow with an HTTP trigger** which generates an equivalent URL. From `teams-notify`'s perspective the code is identical — `HttpClient.PostAsync(url, payload)`. The difference is only in how the recipient creates the URL, and that Power Automate may require a licence depending on the organisation's M365 plan.

The complexity of creating the receiving workflow is a one-time task for the person configuring the channel, not the tool's concern.

**Industry adoption:** Research conducted March 2026 confirms that the Power Automate Workflow webhook is the overwhelming industry consensus for replacing Teams notifications in CI/CD tools:

- **GitLab** — updated docs to instruct users to set up a Power Automate workflow manually. No native Graph API path built
- **GitHub** — official Teams app uses a bot (unaffected by deprecation); simple Actions-based notifications must migrate to Power Automate workflow URLs
- **ArgoCD** — shipped a dedicated `teams-workflows` notification service targeting Power Automate URLs alongside the old connector path
- **Flux CD** — auto-detects old vs new webhook URL by hostname and routes accordingly
- **Atlassian (Jira/Confluence)** — dropped the native connector, replaced with a generic HTTP action pointing at a Power Automate workflow URL

No major CI/CD platform has built a Graph API integration for channel notifications. The Power Automate workflow webhook is the standard replacement path across the industry.

**What changes in the code:**
- `AuthService` and `GraphService` are not used for sending — replaced with a simple `HttpClient` POST to the webhook URL
- A new `WebhookService` handles the POST and Adaptive Card payload formatting
- `TeamsCredentials` is replaced with a webhook URL per channel as the primary credential
- The `list` command and name-resolution features cannot be built on this approach — the target channel is encoded in the URL
- The send path becomes very simple; the Graph path can be retained in parallel if `list`/resolve is still wanted

**Fallout:**
- Each target channel requires its own Power Automate workflow and its own URL — one URL per channel rather than one App Registration covering all channels
- The workflow URL is a credential — anyone who has it can post to that channel, so URLs must be treated as secrets
- Power Automate may require a premium licence depending on the organisation's M365 plan
- No `list` command and no name-to-GUID resolution — the tool cannot discover teams or channels dynamically
- Does not scale well for organisations with many channels — managing a collection of webhook URLs is operationally messier than a single App Registration
- Developer sentiment across the industry on the Power Automate migration has been sharply negative — described as a "greedy cash grab" due to licensing requirements (source: The Register, Computerworld, 2024)

**Verdict:** The simplest implementation and the proven industry standard path — it is what every major CI/CD platform has landed on for Teams notifications. The trade-off is one URL per channel, no discovery, and a Power Automate dependency. A strong option if RSC cannot be set up, and a realistic interim path while RSC is being arranged with IT.

## Industry Context — Why Nobody Uses Graph API for This

Research across GitLab, GitHub, ArgoCD, Flux CD, and Atlassian confirms that no major tool has adopted the Graph API path for simple channel notifications. The reasons are consistent:

- RSC requires the app to be installed into each team by an admin before it can post — the same operational overhead as the webhook setup, but with more IT involvement
- Graph API credentials (App Registration, client secret) are more complex to configure than pasting a webhook URL
- The webhook model maps directly to how developers already think about outbound notifications in CI/CD pipelines

The Graph API remains the right choice for tools that need **dynamic channel discovery**, **name resolution**, or **organisation-wide access** — which are explicit goals of `teams-notify`. The webhook approach trades those capabilities for simplicity. Both are valid depending on the deployment context.

## Recommended Path

Given the industry research, the recommended path is:

1. **Option 4 (Webhook) first** — implement webhook support as the immediate path to a working tool. It is what every other platform uses, it requires no IT involvement beyond creating a Power Automate flow, and it unblocks testing without waiting for the Technical Services Committee
2. **Option 1 (RSC) in parallel** — continue pursuing RSC with IT for the full Graph API path. RSC delivers the general-purpose capability (any team, any channel, discovery) that makes `teams-notify` more than just another webhook poster
3. **Ship with both modes** — `--webhook <url>` for the simple case, full Graph API credentials for the general-purpose case. Users choose based on their setup
4. Do not pursue Option 2 (Service Account) or Option 3 (Bot Framework)

Integration tests for the Graph API path remain blocked until RSC is set up with the Technical Services Committee. Webhook integration tests can proceed immediately.

## Chosen Solution — RSC via Org App Catalogue

### What we are building

Option 1 (RSC) deployed through the organisation's Teams app catalogue — the **"Added by your org"** section in Microsoft Teams. The app is uploaded once to Teams Admin Center by someone with the Teams Administrator role. After that, any team owner can install it in their team themselves from the catalogue, without raising an IT ticket.

Once installed in a team, `teams-notify` can post to any **standard channel** in that team using the existing Client Credentials auth path — no changes to `AuthService` or the credential model.

**Note on private channels:** RSC is granted at the team level and covers standard channels only. Private channels are a separate security boundary and are not accessible regardless of RSC consent. If someone needs a dedicated notification channel visible only to certain people, a standard channel with a descriptive name (e.g. `#backups`) is the right approach — private channel support is not something `teams-notify` can provide without a separate permission model.

**Note on direct messages:** `ChatMessage.Send` — the permission for sending DMs to individuals — is also delegated-only, the same blocker as `ChannelMessage.Send`. Direct messages to individuals are not supported. For personal notifications, create a dedicated standard channel in the relevant team. The `--to` flag is out of scope for v1.

### Intended use case

A nightly backup script on a Linux server posts the result to a `#backups` channel in the team. Every morning the team can see at a glance what succeeded and what failed without logging into any server. This is the canonical use case — an unattended script running on a schedule, posting a plain-text or simple HTML notification to a shared team channel.

### The deployment flow

**IT does once, ever:**
1. Create the Entra ID App Registration with `Team.ReadBasic.All` and `Channel.ReadBasic.All` application permissions and grant admin consent
2. Upload `teams-notify-app.zip` to Teams Admin Center → Manage apps → Upload custom app
3. Ensure the org app permission policy allows team owners to install org apps (most orgs leave this open by default)

**Optional — IT deploys org-wide via app setup policy:**

Instead of waiting for each team owner to self-install, a Teams admin can configure an app setup policy that automatically deploys `teams-notify` to every team in the org. This means the app is already present in every team with no action required from team owners. This is a larger ask of IT but reduces per-team friction to zero. Both models are valid — self-service from catalogue for orgs that prefer opt-in, policy deployment for orgs that want it everywhere from day one.

**Team owner does when they want notifications — self-service, no IT ticket:**
1. Open their team → Apps → search "teams-notify" under "Added by your org"
2. Click install — RSC consent (`ChannelMessage.Send.Group`) is granted automatically at that moment for that team
3. The bot posts a welcome message to General with the team ID

**Person configuring the sender:**
1. Run `teams-notify list` to see teams and channel IDs
2. Add the team ID and channel name to config or the send command — done

### Why we chose this over the alternatives

**Why not Webhook (Option 4)?**

The webhook path (Power Automate workflow HTTP trigger) is what every major CI/CD platform has landed on because it requires zero Entra ID setup. We evaluated it seriously and rejected it for this project for three reasons:

- **One URL per channel.** Every channel that needs notifications requires its own Power Automate workflow and its own URL managed as a secret. A single App Registration covers every team and channel the app has access to — the credential set never grows as more channels are added.
- **No discovery.** The `list` command and name-to-ID resolution are explicit design goals of `teams-notify`. Neither can be built on webhooks — the target is encoded in the URL and there is no API to enumerate teams or channels.
- **Power Automate licence dependency.** The HTTP trigger for Teams notifications may require a premium Power Automate licence depending on the organisation's M365 plan. This introduces an external cost and approval process that is outside our control and varies per organisation. RSC has no ongoing cost beyond the App Registration that is already required.

The webhook is a valid fallback for organisations that cannot or will not do the one-time Admin Center setup, and we may implement `--webhook <url>` as a secondary mode in a future version. It is not the primary path.

**Why not Service Account (Option 2) or Bot Framework (Option 3)?**

Service Account requires purchasing an M365 licence, uses a deprecated auth flow (ROPC), and is incompatible with MFA — which rules it out entirely on a government-managed tenant. Bot Framework requires an additional Azure resource, two auth paths in the codebase, and the same per-team installation overhead as RSC but with more complexity. Neither option offers anything RSC does not, at higher cost and friction.

**Why RSC via org catalogue and not RSC via direct sideloading?**

Sideloading requires IT to upload the app directly into each target team. Every new team is a new IT ticket. The org catalogue changes this completely — IT uploads once to Admin Center and team owners self-install from that point on. The ongoing operational overhead drops to zero after the initial setup.

### Caveats

- **Teams Administrator role required** for the one-time Admin Center upload. This is the only IT touchpoint that cannot be self-served.
- **Org app permission policy** must allow team owners to install org apps. This is the default in most tenants but can be locked down by the Teams admin. Worth confirming before expecting self-service installation to work.
- **Standard channels only.** Private channels are not accessible via RSC.
- **Integration tests remain blocked** until the App Registration and Admin Center deployment are in place. The auth path is implemented; it cannot be validated until RSC is live.
