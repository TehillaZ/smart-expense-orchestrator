#Requires -Version 5.1
<#
.SYNOPSIS
  Runs fast multi-layer backend tests (Unit + Integration + Api), excluding Journey.

.EXAMPLE
  pwsh -File scripts/run-fast-tests.ps1
#>
$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot

dotnet test "$root/backend/ApprovalFlow.sln" `
  --configuration Release `
  --filter "Category!=Journey" `
  --verbosity minimal

if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}
