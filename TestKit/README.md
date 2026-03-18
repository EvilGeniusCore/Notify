# teams-notify — Test Kit

Use this folder to verify that `teams-notify` is correctly configured and able to send messages to your Teams channel after completing the Admin Center setup.

## Step 1 — Get the binary for your platform

Go to the [releases page](https://github.com/Bonejob/teams-notify/releases) and download the zip for your platform:

| Platform | Zip file | Binary inside |
|---|---|---|
| Windows 64-bit | `teams-notify-win-x64.zip` | `teams-notify.exe` |
| Linux 64-bit | `teams-notify-linux-x64.zip` | `teams-notify` |
| Linux ARM 64-bit | `teams-notify-linux-arm64.zip` | `teams-notify` |
| macOS Intel | `teams-notify-osx-x64.zip` | `teams-notify` |
| macOS Apple Silicon | `teams-notify-osx-arm64.zip` | `teams-notify` |

Extract the binary from the zip and place it in this `TestKit/` folder alongside the test scripts.

On Linux or macOS, mark the binary as executable after extracting:

```bash
chmod +x teams-notify
```

## Step 2 — Configure credentials

Copy `test.env.template` to `test.env`:

```bash
# Linux / macOS
cp test.env.template test.env

# Windows PowerShell
Copy-Item test.env.template test.env
```

Open `test.env` and fill in the values. All five fields are required. See the comments in the file for where to find each value.

`test.env` is ignored by git and will not be committed.

## Step 3 — Run the test script

**Windows (PowerShell):**

```powershell
.\Test-TeamsNotify.ps1
```

**Linux / macOS (Bash):**

```bash
./test-teams-notify.sh
```

## What the tests do

The script runs three checks in order:

| Test | What it verifies |
|---|---|
| `list` | Authentication is working and the app can read teams and channels |
| `send --dry-run` | The team and channel resolve correctly without posting a real message |
| `send` | A real message is delivered to the configured channel |

Each test reports pass or fail. If a test fails the script stops and prints the exit code with a description of what it means.

## Troubleshooting

| Exit code | Meaning |
|---|---|
| `2` | Auth failed — check tenant ID, client ID, and client secret in `test.env` |
| `3` | Team or channel not found — check the names match exactly, or use IDs from `teams-notify list` |
| `4` | Graph API error — the app may not have the required permissions, or the Teams app may not be installed in the target team |
| `5` | Config missing — a required value is blank in `test.env` |

If `list` passes but `send` returns exit code `4`, the most likely cause is that the `teams-notify` app has not been installed in the target team via Teams Admin Center. See the main `TeamsApp/README.md` for installation steps.
