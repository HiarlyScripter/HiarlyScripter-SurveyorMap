[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$Message,

    [switch]$SkipValidation
)

$ErrorActionPreference = 'Stop'
$RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
Set-Location $RepoRoot

Write-Host "Repository: $RepoRoot"
Write-Host ''
Write-Host 'Git status before finish:'
git status --short --branch

$ValidationReports = @(
    'tools\last-full-validation.md',
    'tools\last-full-validation.json',
    'tools\last-gameplay-validation.md',
    'tools\last-gameplay-validation.json',
    'tools\last-runtime-validation.md',
    'tools\last-runtime-validation.json'
)

if ($SkipValidation) {
    Write-Host ''
    Write-Host 'Validation: skipped by -SkipValidation.'
} else {
    $ExistingReports = foreach ($Report in $ValidationReports) {
        $Path = Join-Path $RepoRoot $Report
        if (Test-Path -LiteralPath $Path) {
            Get-Item -LiteralPath $Path
        }
    }

    if (-not $ExistingReports) {
        Write-Host ''
        Write-Host 'BLOCKED: validation scripts exist, but no validation report was found.'
        Write-Host 'Run the proper validation first, or rerun with -SkipValidation only for protocol-only/checkpoint work.'
        exit 3
    }

    Write-Host ''
    Write-Host 'Validation reports found:'
    $ExistingReports |
        Sort-Object LastWriteTime -Descending |
        Select-Object LastWriteTime, FullName |
        Format-Table -AutoSize
}

$HandoffPath = Join-Path $RepoRoot 'docs\HANDOFF_CURRENT.md'
Write-Host ''
Write-Host 'docs\HANDOFF_CURRENT.md:'
if (Test-Path -LiteralPath $HandoffPath) {
    Get-Content -LiteralPath $HandoffPath
} else {
    Write-Host 'BLOCKED: missing docs\HANDOFF_CURRENT.md'
    exit 4
}

$Pending = git status --porcelain
if ($Pending) {
    Write-Host ''
    Write-Host 'Creating checkpoint commit...'
    git add .
    git commit -m $Message
} else {
    Write-Host ''
    Write-Host 'No changes to commit.'
}

Write-Host ''
Write-Host 'Final commit:'
git log --oneline --decorate -1

Write-Host ''
Write-Host 'Recent history:'
git log --oneline --decorate -5

Write-Host ''
Write-Host 'Push was not performed.'

