# notify — Teams Admin Setup Guide

End-to-end setup for connecting `notify` to a Microsoft Teams environment. Covers the Entra ID App Registration, Teams app packaging, and deployment.

## Prerequisites

- Global Admin or Application Administrator access in Entra ID
- The `notify` repo cloned locally
- Icons present in `Images/` at the repo root (`Notify-192x192.png`, `Notify- black - 32x32.png`)

## Step 1 — Entra ID App Registration

Go to [portal.azure.com](https://portal.azure.com) and sign in.

**Create the registration:**

1. In the top search bar, search for **App registrations** and click it (under Services)
2. Click **New registration**
3. Give it a name (e.g. `notify`), leave all other defaults → **Register**

**Copy your credentials:**

4. You are now on the **Overview** page of your new app. Copy and save:
   - **Application (client) ID** → this is your `NOTIFY_TEAMS_CLIENT_ID`
   - **Directory (tenant) ID** → this is your `NOTIFY_TEAMS_TENANT_ID`

**Create a client secret:**

5. In the left sidebar, click **Certificates & secrets** → **New client secret**
6. Set a description and expiry → **Add**
7. Copy the **Value** column immediately — it is only shown once → this is your `NOTIFY_TEAMS_CLIENT_SECRET`

**Add API permissions:**

8. In the left sidebar, click **API permissions** → **Add a permission**
9. Select **Microsoft Graph** → **Application permissions**
10. Search for and add each of the following:
    - `Team.ReadBasic.All`
    - `Channel.ReadBasic.All`
11. Click the **Grant admin consent for [your organisation]** button at the top of the permissions list (above the table) → confirm in the dialog. The warning triangles next to each permission should turn into green checkmarks.

These two permissions cover `notify list` and name-to-ID resolution. Message sending is handled by RSC (see Step 3) — there is no tenant-wide application permission for it.

## Step 2 — Package the Teams App

From the `TeamsApp/` folder:

1. Copy the manifest template:
   ```powershell
   Copy-Item manifest.json.template manifest.json
   ```
2. Open `manifest.json` and set `webApplicationInfo.id` to the **Application (client) ID** from Step 1
3. Run the packaging script:
   ```powershell
   .\Package-TeamsApp.ps1
   ```

This produces `notify-app.zip`. The script validates all prerequisites and will fail with a clear error if anything is missing.

## Step 3 — Deploy the Teams App

RSC consent (`ChannelMessage.Send.Group`) is granted at the point of Teams app installation. The app must be installed in every team that needs to receive notifications.

Two deployment paths are available depending on context.

### Developer path — Sideload directly into a team

Custom app uploads must be enabled org-wide before sideloading works. In [Teams Admin Center](https://admin.teams.microsoft.com):

1. **Teams apps** → **Manage apps** → **Actions** → **Org-wide app settings**
2. Under **Custom apps**, enable **Let users install and use available apps by default**
3. **Save** — takes effect after a few hours

Once enabled, any team owner can sideload:

1. In Teams, open the target team → **Manage team** → **Apps**
2. **Upload an app** → **Upload a custom app** → select `notify-app.zip`
3. **Add**

Sideloading is per-team and per-install. Repeat for each team. A new `notify-app.zip` must be re-uploaded if the manifest changes.

### Admin path — Org app catalogue

Uploading to the org catalogue makes `notify` available to all team owners as a self-service install from the **Added by your org** section, with no further IT involvement per team.

Requires the **Teams Administrator** role.

In [Teams Admin Center](https://admin.teams.microsoft.com):

1. **Teams apps** → **Manage apps** → select **Upload new app** (top of the page)
2. Select `notify-app.zip` → confirm the upload
3. Find the app in the list and confirm its status is **Allowed** — if it shows **Blocked**, select it and change the status to **Allowed**

Team owners then install it themselves:

1. Open the target team → **Manage team** → **Apps** → search **notify** under **Added by your org**
2. **Add**

A Teams Administrator only needs to upload once. Each subsequent version is a re-upload to the same app entry — the stable app ID (`e0f04c45-7db3-417b-9d05-d02b30d675a4`) ensures Teams matches it to the existing installation.

## Step 4 — Verify

Run these against a team where the app has been installed:

```powershell
# Confirm auth and permissions are working
notify list --env-file .\teams.env

# Confirm the target team and channel resolve
notify send --env-file .\teams.env --team "your-team" --channel "your-channel" --message "notify setup verified" --dry-run

# Send a real message
notify send --env-file .\teams.env --team "your-team" --channel "your-channel" --message "notify setup verified"
```

`list` succeeds → App Registration and tenant permissions are correct.
`send` succeeds → RSC is in place and the Teams app is installed.
`send` fails with exit code `4` after `list` succeeds → Teams app is not installed in the target team.

## Credentials handoff

Once setup is complete, hand the following to whoever is configuring `notify` in their scripts:

```
NOTIFY_TEAMS_TENANT_ID
NOTIFY_TEAMS_CLIENT_ID
NOTIFY_TEAMS_CLIENT_SECRET
```

Default team and channel can be set via `NOTIFY_TEAMS_DEFAULT_TEAM` and `NOTIFY_TEAMS_DEFAULT_CHANNEL`, or passed per-invocation with `--team` and `--channel`.

See [notify-cli-manual.md](../../Documentation/notify-cli-manual.md) for full CLI usage.
