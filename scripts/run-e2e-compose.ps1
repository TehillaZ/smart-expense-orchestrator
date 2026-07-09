#Requires -Version 5.1
<#
.SYNOPSIS
  Runs Docker Compose E2E journey verification (N6 slower layer).

.DESCRIPTION
  Builds/starts the stack, waits for gateway health, then runs verify-journeys.ps1.

.EXAMPLE
  pwsh -File scripts/run-e2e-compose.ps1
#>
param(
    [string]$ComposeFile = "infra/docker-compose.yml",
    [string]$BaseUrl = "http://localhost:8080",
    [int]$WaitSeconds = 5,
    [switch]$SkipBuild
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
Set-Location $root

if ($SkipBuild) {
    docker compose -f $ComposeFile up -d
} else {
    docker compose -f $ComposeFile up --build -d
}

if ($LASTEXITCODE -ne 0) {
    throw "docker compose up failed with exit code $LASTEXITCODE."
}

$healthy = $false
for ($i = 1; $i -le 60; $i++) {
    try {
        $health = Invoke-RestMethod -Uri "$BaseUrl/health" -Method GET
        if ($health.status -eq "Healthy") {
            $healthy = $true
            break
        }
    }
    catch {
        # keep waiting
    }
    Start-Sleep -Seconds 5
}

if (-not $healthy) {
    docker compose -f $ComposeFile ps
    docker compose -f $ComposeFile logs --tail=100
    throw "Gateway did not become healthy at $BaseUrl."
}

try {
    & "$root/scripts/verify-journeys.ps1" -BaseUrl $BaseUrl -ComposeFile $ComposeFile -WaitSeconds $WaitSeconds
    $verifyExit = $LASTEXITCODE
}
finally {
    docker compose -f $ComposeFile down -v | Out-Null
}

exit $verifyExit
