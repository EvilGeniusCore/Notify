# notify — Teams App Package

This folder contains the Microsoft Teams app manifest used to grant RSC (Resource-Specific Consent) permission for `notify` to post messages to channels in a team.

## Why this is needed

`ChannelMessage.Send` does not exist as a tenant-wide Application permission in Microsoft Graph. It is delegated-only. The supported path for unattended app-only sending is RSC, which scopes the permission to a specific team via a Teams app installation.

Once installed in a team, `notify` can post to **any channel** in that team.

## Before you package

### 1. Generate a unique app ID

Open PowerShell and run:

```powershell
New-Guid
```

Replace the `"id"` value in `manifest.json` with the generated GUID. This ID must be unique and stable — do not regenerate it on subsequent releases.

### 2. Add icons

Teams requires two icon files in this folder:

| File | Size | Notes |
|---|---|---|
| `color.png` | 192 × 192 px | Full colour app icon |
| `outline.png` | 32 × 32 px | White/transparent outline icon for the Teams sidebar |

Both files must be PNG. The `color.png` should have a solid background. The `outline.png` should use only white and transparent pixels.

### 3. Grant tenant-level Application permissions

RSC handles `ChannelMessage.Send.Group` (send), but two read permissions must still be granted at tenant level by a Global Admin so the `list` command and name resolution work:

- `Team.ReadBasic.All` — list teams and resolve team names to IDs
- `Channel.ReadBasic.All` — list channels and resolve channel names to IDs

Add these in the Azure portal under the App Registration → API permissions → Microsoft Graph → Application permissions, then grant admin consent.

## Packaging

Run the packaging script from this folder:

```powershell
.\Package-TeamsApp.ps1
```

This produces `notify-app.zip` ready for sideloading.

Or package manually:

```powershell
Compress-Archive -Path manifest.json, color.png, outline.png -DestinationPath notify-app.zip -Force
```

## Installing in a team

1. In Microsoft Teams, go to the target team
2. Click **Manage team** (⚙) → **Apps** tab
3. Click **Upload an app** → **Upload a custom app**
4. Select `notify-app.zip`
5. Click **Add** to grant the RSC permission and install the app

Repeat for each team that needs to receive notifications. The app must be installed in a team before `notify send` can post to any channel in that team.

## Updating the app

If `manifest.json` changes (e.g. version bump), repackage and re-upload to each team. The `"id"` GUID must stay the same across updates — Teams uses it to match the existing installation.
