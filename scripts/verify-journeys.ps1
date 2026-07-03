#Requires -Version 5.1
<#
.SYNOPSIS
  Verifies the four required ApprovalFlow journeys against a running Docker stack.

.DESCRIPTION
  Run after:
    docker compose -f infra/docker-compose.yml up --build -d

  Tests:
    - INV-1001: auto-approve and pay
    - INV-1003: escalate, human approve, pay
    - INV-1007: duplicate submission paid once
    - RS-90021: payment failure with compensation

.EXAMPLE
  pwsh -File scripts/verify-journeys.ps1
  pwsh -File scripts/verify-journeys.ps1 -BaseUrl http://localhost:8080 -WaitSeconds 10
#>
param(
    [string]$BaseUrl = "http://localhost:8080",
    [int]$WaitSeconds = 8
)

$ErrorActionPreference = "Stop"

function Write-Pass([string]$Message) {
    Write-Host "[PASS] $Message" -ForegroundColor Green
}

function Write-Fail([string]$Message) {
    Write-Host "[FAIL] $Message" -ForegroundColor Red
}

function Submit-Invoice {
    param(
        [string]$Vendor,
        [string]$InvoiceNumber,
        [decimal]$Amount,
        [string]$Category
    )

    $body = @{
        vendor = $Vendor
        invoiceNumber = $InvoiceNumber
        amount = $Amount
        category = $Category
    } | ConvertTo-Json

    return Invoke-RestMethod `
        -Uri "$BaseUrl/submissions" `
        -Method POST `
        -Body $body `
        -ContentType "application/json"
}

function Get-SubmissionStatus {
    param([string]$TrackingId)

    return Invoke-RestMethod `
        -Uri "$BaseUrl/submissions/$TrackingId" `
        -Method GET
}

function Approve-Submission {
    param([string]$TrackingId)

    $body = @{
        decision = "Approve"
        comment = "Approved by verify-journeys.ps1"
    } | ConvertTo-Json

    return Invoke-RestMethod `
        -Uri "$BaseUrl/approvals/$TrackingId/decision" `
        -Method POST `
        -Body $body `
        -ContentType "application/json"
}

function Wait-ForStatus {
    param(
        [string]$TrackingId,
        [string[]]$ExpectedStatuses,
        [int]$MaxAttempts = 6
    )

    for ($attempt = 1; $attempt -le $MaxAttempts; $attempt++) {
        Start-Sleep -Seconds $WaitSeconds
        $status = Get-SubmissionStatus -TrackingId $TrackingId
        if ($ExpectedStatuses -contains $status.status) {
            return $status
        }
    }

    $last = Get-SubmissionStatus -TrackingId $TrackingId
    throw "Timed out waiting for $($ExpectedStatuses -join ' or '). Last status: $($last.status)"
}

$failures = 0

Write-Host "ApprovalFlow journey verification" -ForegroundColor Cyan
Write-Host "Gateway: $BaseUrl"
Write-Host ""

try {
    $health = Invoke-RestMethod -Uri "$BaseUrl/health" -Method GET
    if ($health.status -ne "Healthy") {
        throw "Gateway health returned unexpected status: $($health.status)"
    }
    Write-Pass "Gateway health check"
}
catch {
    Write-Fail "Gateway is not reachable at $BaseUrl. Start Docker Compose first."
    exit 1
}

# Journey 1: INV-1001 auto-approve + pay
try {
    $submit = Submit-Invoice -Vendor "Acme Corp" -InvoiceNumber "INV-1001" -Amount 100 -Category "meals"
    if ($submit.isDuplicate) { throw "First submit should not be duplicate." }

    $final = Wait-ForStatus -TrackingId $submit.trackingId -ExpectedStatuses @("Paid")
    Write-Pass "INV-1001 auto-approve and pay (trackingId=$($submit.trackingId), status=$($final.status))"
}
catch {
    Write-Fail "INV-1001 auto-approve and pay — $($_.Exception.Message)"
    $failures++
}

# Journey 2: INV-1003 escalate + approve + pay
try {
    $submit = Submit-Invoice -Vendor "Acme Corp" -InvoiceNumber "INV-1003" -Amount 500 -Category "meals"
    $escalated = Wait-ForStatus -TrackingId $submit.trackingId -ExpectedStatuses @("Escalated")
    Approve-Submission -TrackingId $submit.trackingId | Out-Null
    $final = Wait-ForStatus -TrackingId $submit.trackingId -ExpectedStatuses @("Paid")
    Write-Pass "INV-1003 escalate, approve, and pay (trackingId=$($submit.trackingId), status=$($final.status))"
}
catch {
    Write-Fail "INV-1003 escalate and resume — $($_.Exception.Message)"
    $failures++
}

# Journey 3: INV-1007 duplicate paid once
try {
    $first = Submit-Invoice -Vendor "Acme Corp" -InvoiceNumber "INV-1007" -Amount 120 -Category "meals"
    $duplicate = Submit-Invoice -Vendor "Acme Corp" -InvoiceNumber "INV-1007" -Amount 120 -Category "meals"

    if ($first.isDuplicate) { throw "First INV-1007 submit should not be duplicate." }
    if (-not $duplicate.isDuplicate) { throw "Second INV-1007 submit should be duplicate." }
    if ($first.trackingId -ne $duplicate.trackingId) { throw "Duplicate submit returned a different tracking id." }

    $final = Wait-ForStatus -TrackingId $first.trackingId -ExpectedStatuses @("Paid")
    Write-Pass "INV-1007 duplicate short-circuit (trackingId=$($first.trackingId), status=$($final.status))"
}
catch {
    Write-Fail "INV-1007 duplicate paid once — $($_.Exception.Message)"
    $failures++
}

# Journey 4: RS-90021 payment failure + compensation
try {
    $submit = Submit-Invoice -Vendor "Acme Corp" -InvoiceNumber "RS-90021" -Amount 9500 -Category "meals"
    Wait-ForStatus -TrackingId $submit.trackingId -ExpectedStatuses @("Escalated") | Out-Null
    Approve-Submission -TrackingId $submit.trackingId | Out-Null
    $final = Wait-ForStatus -TrackingId $submit.trackingId -ExpectedStatuses @("Compensated")
    Write-Pass "RS-90021 payment failure and compensation (trackingId=$($submit.trackingId), status=$($final.status))"
}
catch {
    Write-Fail "RS-90021 payment failure and compensation — $($_.Exception.Message)"
    $failures++
}

Write-Host ""
if ($failures -eq 0) {
    Write-Host "All journey checks passed." -ForegroundColor Green
    exit 0
}

Write-Host "$failures journey check(s) failed." -ForegroundColor Red
exit 1
