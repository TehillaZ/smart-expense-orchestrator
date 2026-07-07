#Requires -Version 5.1
<#
.SYNOPSIS
  Installs project Git hooks into .git/hooks for Windows (Git Bash / Git for Windows).

.DESCRIPTION
  Copies hooks from scripts/git-hooks/ into .git/hooks/ and marks them executable.

  Hooks installed:
    - pre-commit  : git fetch + merge origin/main before each commit
    - commit-msg  : enforce lowercase Conventional Commits (feat:, fix:, test:, docs:, chore:)

.EXAMPLE
  pwsh -File scripts/install-git-hooks.ps1
#>
$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
$sourceDir = Join-Path $repoRoot "scripts\git-hooks"
$targetDir = Join-Path $repoRoot ".git\hooks"

if (-not (Test-Path $targetDir)) {
  throw "Git hooks directory not found: $targetDir. Run this from a Git repository root."
}

$hooks = @("pre-commit", "commit-msg")

foreach ($hook in $hooks) {
  $source = Join-Path $sourceDir $hook
  $target = Join-Path $targetDir $hook

  if (-not (Test-Path $source)) {
    throw "Missing hook source file: $source"
  }

  Copy-Item -Path $source -Destination $target -Force
  Write-Host "Installed $hook -> $target" -ForegroundColor Green
}

# Mark hooks executable for Git (works with Git for Windows).
foreach ($hook in $hooks) {
  & git -C $repoRoot update-index --chmod=+x "scripts/git-hooks/$hook" 2>$null
}

Write-Host ""
Write-Host "Git hooks installed successfully." -ForegroundColor Cyan
Write-Host "  pre-commit : syncs with origin/main before every commit"
Write-Host "  commit-msg : validates lowercase Conventional Commits"
Write-Host ""
Write-Host "Re-run this script after cloning the repo on a new machine."
