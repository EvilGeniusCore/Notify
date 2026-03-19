# notify — Teams App Package

This folder contains the Microsoft Teams app manifest used to grant RSC (Resource-Specific Consent) permission for `notify` to post messages to channels in a team.

## Why this is needed

`ChannelMessage.Send` does not exist as a tenant-wide Application permission in Microsoft Graph. It is delegated-only. The supported path for unattended app-only sending is RSC, which scopes the permission to a specific team via a Teams app installation.

Once installed in a team, `notify` can post to **any standard channel** in that team.

## Before you package

### 1. Create your manifest.json

Copy the template to create your local manifest:

```powershell
Copy-Item manifest.json.template manifest.json
```

`manifest.json` is gitignored — it holds your App Registration client ID and must not be committed.

### 2. Set the App Registration client ID

Open `manifest.json` and replace the `webApplicationInfo.id` placeholder with the **Application (client) ID** of the Entra ID App Registration created for `notify` — the same value as `NOTIFY_TEAMS_CLIENT_ID` in your credentials file:

```json
"webApplicationInfo": {
  "id": "your-app-registration-client-id-here",
  "resource": "https://RscBasedStoreApp"
}
```

This links the Teams app to the App Registration so RSC consent flows to the correct identity. The `resource` value is a fixed Teams RSC placeholder — leave it as-is.

### 3. Verify the app ID

The `"id"` field in `manifest.json` is already set to a stable GUID (`e0f04c45-7db3-417b-9d05-d02b30d675a4`). Do not change it — Teams uses this ID to match updates to existing installations.

### 4. Verify icons are present

The packaging script pulls icons from the `Images/` folder at the repo root. Confirm these files exist before packaging:

| File | Size |
|---|---|
| `Images/Notify-192x192.png` | 192 × 192 px full colour icon |
| `Images/Notify- black - 32x32.png` | 32 × 32 px outline icon |

The script copies and renames them automatically — you do not need to add any files to this folder manually.

### 5. Grant tenant-level Application permissions

RSC handles `ChannelMessage.Send.Group` (send), but two read permissions must still be granted at tenant level by a Global Admin so the `list` command and name resolution work:

- `Team.ReadBasic.All` — list teams and resolve team names to IDs
- `Channel.ReadBasic.All` — list channels and resolve channel names to IDs

Add these in the Azure portal under the App Registration → API permissions → Microsoft Graph → Application permissions, then grant admin consent.

## Packaging

Run the packaging script from this folder:

```powershell
.\Package-TeamsApp.ps1
```

This produces `notify-app.zip` ready for installation. The script validates that all prerequisites are met before packaging.

## Installing in a team

1. In Microsoft Teams, go to the target team
2. Click **Manage team** → **Apps** tab
3. Click **Upload an app** → **Upload a custom app**
4. Select `notify-app.zip`
5. Click **Add** to grant the RSC permission and install the app

Repeat for each team that needs to receive notifications. The app must be installed in a team before `notify send` can post to any channel in that team.

Installing a Teams app requires the **team owner** role. Being a team member or having created a channel within the team does not grant this permission.

## Updating the app

If `manifest.json` changes (e.g. version bump), repackage and re-upload to each team. The `"id"` GUID must stay the same across updates — Teams uses it to match the existing installation.
