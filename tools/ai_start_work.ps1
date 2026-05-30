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

switch ($Branch) {
    'claude-work' { Write-Host 'Role: Claude Executor' }
    'codex-exec' { Write-Host 'Role: Codex Executor' }
    default { Write-Host 'Role: unassigned branch' }
}

Write-Host ''
Write-Host 'Git status:'
git status --short --branch

$Pending = git status --porcelain
if ($Pending) {
    Write-Host ''
    Write-Host 'BLOCKED: uncommitted changes detected.'
    Write-Host 'Create a checkpoint commit or clean the working tree before starting new work.'
    exit 2
}

Write-Host ''
Write-Host 'Last commit:'
git log --oneline --decorate -1

$HandoffPath = Join-Path $RepoRoot 'docs\HANDOFF_CURRENT.md'
$StartHerePath = Join-Path $RepoRoot 'docs\START_HERE_FOR_AI.md'

Write-Host ''
Write-Host 'docs\HANDOFF_CURRENT.md:'
if (Test-Path -LiteralPath $HandoffPath) {
    Get-Content -LiteralPath $HandoffPath
} else {
    Write-Host 'Missing docs\HANDOFF_CURRENT.md'
}

Write-Host ''
Write-Host 'docs\START_HERE_FOR_AI.md:'
if (Test-Path -LiteralPath $StartHerePath) {
    Get-Content -LiteralPath $StartHerePath
} else {
    Write-Host 'Missing docs\START_HERE_FOR_AI.md'
}

