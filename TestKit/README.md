# notify — Test Kit

Use this folder to verify that `notify` is correctly configured and able to send messages to your Teams channel after completing the Admin Center setup.

## Step 1 — Get the binary for your platform

Go to the [releases page](https://github.com/EvilGeniusCore/Notify/releases) and download the zip for your platform:

| Platform | Zip file | Binary inside |
|---|---|---|
| Windows 64-bit | `notify-win-x64.zip` | `notify.exe` |
| Linux 64-bit | `notify-linux-x64.zip` | `notify` |
| Linux ARM 64-bit | `notify-linux-arm64.zip` | `notify` |
| macOS Intel | `notify-osx-x64.zip` | `notify` |
| macOS Apple Silicon | `notify-osx-arm64.zip` | `notify` |

Extract the binary from the zip and place it in this `TestKit/` folder alongside the test scripts.

On Linux or macOS, mark the binary as executable after extracting:

```bash
chmod +x notify
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
.\Test-Notify.ps1
```

**Linux / macOS (Bash):**

```bash
./test-notify.sh
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
| `3` | Team or channel not found — check the names match exactly, or use IDs from `notify list` |
| `4` | Graph API error — the app may not have the required permissions, or the Teams app may not be installed in the target team |
| `5` | Config missing — a required value is blank in `test.env` |

If `list` passes but `send` returns exit code `4`, the most likely cause is that the `notify` Teams app has not been installed in the target team. Installing the app requires the **team owner** role — being a team member or having created a channel is not sufficient. If you are not the team owner, ask your team owner to install `notify` from the "Added by your org" section in the team's Apps tab. See `TeamsApp/README.md` for the full installation steps.
