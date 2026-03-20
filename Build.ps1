# Build.ps1 — publish notify for one or more target profiles
# Run from the solution root (Teams-Notify/).
#
# Usage:
#   ./Build.ps1                          # all profiles
#   ./Build.ps1 -Profiles win-x64        # single profile
#   ./Build.ps1 -Profiles win-x64,linux-x64

param(
    [string[]] $Profiles = @()
)

$ErrorActionPreference = "Stop"
$SolutionDir = (Resolve-Path .).Path.TrimEnd('\') + "\"
$ProjectPath = "src/Notify/Notify.csproj"

$DefaultProfiles = @(
    "win-x64",
    "linux-x64",
    "linux-arm64",
    "osx-x64",
    "osx-arm64"
)

$TargetProfiles = if ($Profiles.Count -gt 0) { $Profiles } else { $DefaultProfiles }
$Failed = @()

foreach ($pub in $TargetProfiles) {
    $outDir  = Join-Path $SolutionDir "artifacts/$pub"
    $zipPath = Join-Path $SolutionDir "artifacts/notify-$pub.zip"

    if (Test-Path $outDir) {
        Write-Host "    Cleaning $outDir ..." -ForegroundColor DarkGray
        Remove-Item $outDir -Recurse -Force -ErrorAction SilentlyContinue
    }
    if (Test-Path $zipPath) {
        Remove-Item $zipPath -Force -ErrorAction SilentlyContinue
    }

    Write-Host ""
    Write-Host "==> Publishing $pub ..." -ForegroundColor Cyan
    dotnet publish $ProjectPath -p:PublishProfile=$pub -p:SolutionDir=$SolutionDir
    if ($LASTEXITCODE -ne 0) {
        Write-Host "FAILED: $pub" -ForegroundColor Red
        $Failed += $pub
        continue
    }

    Write-Host "    Zipping $pub ..." -ForegroundColor DarkGray
    Compress-Archive -Path "$outDir\*" -DestinationPath $zipPath
    Write-Host "OK: $pub -> artifacts/notify-$pub.zip" -ForegroundColor Green
}

Write-Host ""
if ($Failed.Count -eq 0) {
    Write-Host "All profiles built successfully." -ForegroundColor Green
} else {
    Write-Host "Failed profiles: $($Failed -join ', ')" -ForegroundColor Red
    exit 1
}
