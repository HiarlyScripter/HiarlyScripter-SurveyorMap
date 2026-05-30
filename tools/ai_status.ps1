[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
Set-Location $RepoRoot

Write-Host "Repository: $RepoRoot"

$Branch = (git branch --show-current).Trim()
if ([string]::IsNullOrWhiteSpace($Branch)) {
    $Branch = '(detached HEAD)'
}

Write-Host "Branch: $Branch"
Write-Host ''
Write-Host 'Git status:'
git status --short --branch

$Pending = git status --porcelain
Write-Host ''
if ($Pending) {
    Write-Host 'Uncommitted changes: YES'
} else {
    Write-Host 'Uncommitted changes: NO'
}

Write-Host ''
Write-Host 'Last 10 commits:'
git log --oneline --decorate -10

Write-Host ''
Write-Host 'Relevant tags:'
$Tags = git tag --list '*baseline*' --sort=-creatordate
if ($Tags) {
    $Tags
} else {
    Write-Host '(none)'
}

$NextActionsPath = Join-Path $RepoRoot 'docs\NEXT_ACTIONS.md'
Write-Host ''
if (Test-Path -LiteralPath $NextActionsPath) {
    Write-Host 'docs\NEXT_ACTIONS.md:'
    Get-Content -LiteralPath $NextActionsPath -TotalCount 80
} else {
    Write-Host 'docs\NEXT_ACTIONS.md: not found'
}

