#Requires -Version 5.1
<#
.SYNOPSIS
    Packages the Teams app manifest and icons into a zip ready for sideloading.

.DESCRIPTION
    Validates that required files are present then produces notify-app.zip
    in the TeamsApp folder. The zip is what you upload to Teams via
    Manage team > Apps > Upload a custom app.

.EXAMPLE
    .\Package-TeamsApp.ps1
#>

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$root     = $PSScriptRoot
$manifest = Join-Path $root 'manifest.json'
$color    = Join-Path $root 'color.png'
$outline  = Join-Path $root 'outline.png'
$output   = Join-Path $root 'notify-app.zip'

# ── Validate prerequisites ────────────────────────────────────────────────────

if (-not (Test-Path $manifest)) {
    Write-Error "manifest.json not found in $root"
}

if (-not (Test-Path $color)) {
    Write-Error "color.png not found — add a 192×192 px PNG icon before packaging."
}

if (-not (Test-Path $outline)) {
    Write-Error "outline.png not found — add a 32×32 px PNG outline icon before packaging."
}

# Check the manifest ID has been set
$json = Get-Content $manifest -Raw | ConvertFrom-Json
if ($json.id -eq 'REPLACE-WITH-NEW-GUID') {
    Write-Error "manifest.json still has the placeholder app ID. Run 'New-Guid' and update the 'id' field before packaging."
}

# ── Package ───────────────────────────────────────────────────────────────────

if (Test-Path $output) {
    Remove-Item $output -Force
}

Compress-Archive -Path $manifest, $color, $outline -DestinationPath $output -CompressionLevel Optimal

Write-Host "OK: $output" -ForegroundColor Green
Write-Host "    App ID:   $($json.id)"
Write-Host "    Version:  $($json.version)"
Write-Host ""
Write-Host "Upload notify-app.zip to each team via:"
Write-Host "  Manage team (⚙) > Apps > Upload an app > Upload a custom app"
