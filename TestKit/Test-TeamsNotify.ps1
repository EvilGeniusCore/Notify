#Requires -Version 5.1
<#
.SYNOPSIS
    Smoke-tests teams-notify against a real Teams environment.

.DESCRIPTION
    Loads credentials from test.env, then runs three checks:
      1. list   — verifies auth and read permissions
      2. send --dry-run — verifies team/channel resolution without posting
      3. send   — delivers a real test message to the configured channel

.EXAMPLE
    .\Test-TeamsNotify.ps1
#>

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$root    = $PSScriptRoot
$envFile = Join-Path $root 'test.env'
$binary  = Join-Path $root 'teams-notify.exe'

# ── Preflight ─────────────────────────────────────────────────────────────────

if (-not (Test-Path $envFile)) {
    Write-Error "test.env not found. Copy test.env.template to test.env and fill in the values."
}

if (-not (Test-Path $binary)) {
    Write-Error "teams-notify.exe not found in this folder. Download the win-x64 release zip from the releases page, extract teams-notify.exe here, and re-run."
}

# ── Load test.env ─────────────────────────────────────────────────────────────

foreach ($line in Get-Content $envFile) {
    $line = $line.Trim()
    if ($line -eq '' -or $line.StartsWith('#')) { continue }
    $parts = $line -split '=', 2
    if ($parts.Count -eq 2 -and $parts[1].Trim() -ne '') {
        [System.Environment]::SetEnvironmentVariable($parts[0].Trim(), $parts[1].Trim(), 'Process')
    }
}

# ── Helpers ───────────────────────────────────────────────────────────────────

$pass  = 0
$fail  = 0

function Run-Test {
    param([string]$Label, [scriptblock]$Block)
    Write-Host "`n[$Label]" -ForegroundColor Cyan
    try {
        & $Block
        if ($LASTEXITCODE -eq 0) {
            Write-Host "  PASS" -ForegroundColor Green
            $script:pass++
        } else {
            Write-Host "  FAIL — exit code $LASTEXITCODE" -ForegroundColor Red
            $script:fail++
            exit $LASTEXITCODE
        }
    } catch {
        Write-Host "  FAIL — $_" -ForegroundColor Red
        $script:fail++
        exit 1
    }
}

# ── Tests ─────────────────────────────────────────────────────────────────────

Write-Host "teams-notify smoke test" -ForegroundColor White
Write-Host "Binary: $binary"
Write-Host "Env:    $envFile"

Run-Test "1. List teams (auth + read permissions)" {
    & $binary list
}

Run-Test "2. Send dry-run (team/channel resolution)" {
    & $binary send --message "teams-notify dry-run test" --dry-run
}

$hostname  = $env:COMPUTERNAME
$timestamp = Get-Date -Format 'yyyy-MM-dd HH:mm:ss'

Run-Test "3. Send real message" {
    & $binary send --message "teams-notify smoke test — $hostname — $timestamp"
}

# ── Summary ───────────────────────────────────────────────────────────────────

Write-Host ""
Write-Host "Results: $pass passed, $fail failed" -ForegroundColor $(if ($fail -eq 0) { 'Green' } else { 'Red' })
