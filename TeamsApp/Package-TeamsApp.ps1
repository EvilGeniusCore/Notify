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
$images   = Join-Path $root '..\Images'
$manifest = Join-Path $root 'manifest.json'
$color    = Join-Path $images 'Notify-192x192.png'
$outline  = Join-Path $images 'Notify- black - 32x32.png'
$output   = Join-Path $root 'notify-app.zip'

# ── Validate prerequisites ────────────────────────────────────────────────────

if (-not (Test-Path $manifest)) {
    Write-Error "manifest.json not found in $root"
}

if (-not (Test-Path $color)) {
    Write-Error "color icon not found: $color"
}

if (-not (Test-Path $outline)) {
    Write-Error "outline icon not found: $outline"
}

# Check the manifest ID has been set
$json = Get-Content $manifest -Raw | ConvertFrom-Json
if ($json.id -eq 'REPLACE-WITH-NEW-GUID') {
    Write-Error "manifest.json still has the placeholder app ID. Run 'New-Guid' and update the 'id' field before packaging."
}

# Check the App Registration client ID has been set
if ($json.webApplicationInfo.id -eq 'REPLACE-WITH-APP-REGISTRATION-CLIENT-ID') {
    Write-Error "manifest.json still has the placeholder App Registration client ID. Update 'webApplicationInfo.id' with your Entra ID App Registration's Application (client) ID before packaging."
}

# ── Stage icons with Teams-required filenames ─────────────────────────────────

$colorStaged   = Join-Path $root 'color.png'
$outlineStaged = Join-Path $root 'outline.png'

try {
    Copy-Item $color   $colorStaged   -Force
    Copy-Item $outline $outlineStaged -Force

    # ── Package ───────────────────────────────────────────────────────────────

    if (Test-Path $output) {
        Remove-Item $output -Force
    }

    Compress-Archive -Path $manifest, $colorStaged, $outlineStaged -DestinationPath $output -CompressionLevel Optimal
}
finally {
    # Clean up staged copies regardless of success or failure
    if (Test-Path $colorStaged)   { Remove-Item $colorStaged   -Force }
    if (Test-Path $outlineStaged) { Remove-Item $outlineStaged -Force }
}

Write-Host "OK: $output" -ForegroundColor Green
Write-Host "    App ID:   $($json.id)"
Write-Host "    Version:  $($json.version)"
Write-Host ""
Write-Host "Upload notify-app.zip to Teams Admin Center:"
Write-Host "  Manage apps > Upload an app > Upload a custom app"
Write-Host ""
Write-Host "Or install directly into a team (sideloading):"
Write-Host "  Manage team (⚙) > Apps > Upload an app > Upload a custom app"
