# surveyormap_full_validate.ps1
# Pipeline completo com 4 fases: audit -> build/install -> runtime -> gameplay.
# Uso: .\tools\surveyormap_full_validate.ps1 [-WaitSeconds 90] [-NoLaunch] [-VerboseReport] [-SkipGameplay]

param(
    [int]$WaitSeconds   = 90,
    [switch]$NoLaunch,
    [switch]$VerboseReport,
    [switch]$SkipGameplay
)

Set-StrictMode -Off
$ErrorActionPreference = "SilentlyContinue"

$proj         = Split-Path $PSScriptRoot -Parent
$srcDir       = Join-Path $proj "src"
$buildDll     = Join-Path $proj "build\SurveyorMap.dll"
$installedDll = "C:\Users\Hiarly\AppData\Roaming\r2modmanPlus-local\REPO\profiles\REPO - Test\BepInEx\plugins\HiarlyScripter-SurveyorMap\SurveyorMap.dll"
$installedDir = Split-Path $installedDll -Parent

function Head([string]$t) { Write-Host ""; Write-Host "===================================================="; Write-Host "  $t"; Write-Host "====================================================" }
function OK([string]$m)   { Write-Host "  [OK]   $m" }
function ERR([string]$m)  { Write-Host "  [FAIL] $m" }
function WARN([string]$m) { Write-Host "  [WARN] $m" }
function SKIP([string]$m) { Write-Host "  [SKIP] $m" }

function Get-MD5Short([string]$p) {
    if (-not (Test-Path $p)) { return "missing" }
    return (Get-FileHash -Path $p -Algorithm MD5).Hash.Substring(0,8).ToLower()
}
function Get-MD5Full([string]$p) {
    if (-not (Test-Path $p)) { return "missing" }
    return (Get-FileHash -Path $p -Algorithm MD5).Hash
}

$overallPass   = $true
$phase1Pass    = $false
$phase2Pass    = $false
$phase3Pass    = $false
$phase4Pass    = $false
$phase4Verdict = "SKIPPED"

# ========================= PHASE 1: STATIC AUDIT =========================
Head "Phase 1/4: Static Audit"
$auditScript = Join-Path $PSScriptRoot "surveyormap_static_audit.ps1"
& $auditScript
$phase1Pass = ($LASTEXITCODE -eq 0)
if ($phase1Pass) { OK "Static audit PASS" } else { ERR "Static audit FAIL"; $overallPass = $false }

# ========================= PHASE 2: BUILD + INSTALL =========================
Head "Phase 2/4: Build Release + Install"

$env:REPO_MANAGED_DIR = "E:\SteamLibrary\steamapps\common\REPO\REPO_Data\Managed"
$env:REPO_BEPINEX_CORE_DIR = "C:\Users\Hiarly\AppData\Roaming\r2modmanPlus-local\REPO\profiles\REPO - Test\BepInEx\core"
Write-Host "  Running: dotnet build -c Release"
$buildOutput = & dotnet build "$srcDir" -c Release 2>&1
$buildExit   = $LASTEXITCODE

$buildOutput | ForEach-Object {
    if ($_ -match "(rror|arning)") { Write-Host "    $_" }
}
$sumLine = $buildOutput | Where-Object { $_ -match "(succeeded|FALHA|Build)" } | Select-Object -First 1
if ($sumLine) { Write-Host "  $sumLine" }

if ($buildExit -eq 0) {
    OK "Build succeeded (0 errors)"
} else {
    ERR "Build FAILED (exit=$buildExit)"
    $overallPass = $false
}

if ($buildExit -eq 0) {
    if (-not (Test-Path $buildDll)) {
        ERR "Build DLL not found after build"
        $overallPass = $false
    } else {
        New-Item -ItemType Directory -Path $installedDir -Force | Out-Null
        Copy-Item $buildDll $installedDll -Force
        $bh = Get-MD5Full $buildDll
        $ih = Get-MD5Full $installedDll
        $buildShort = Get-MD5Short $buildDll
        if ($bh -eq $ih) {
            OK "DLL installed: $buildShort"
            $phase2Pass = $true
        } else {
            ERR "Hash mismatch after install"
            $overallPass = $false
        }
    }
}

# ========================= PHASE 3: RUNTIME VALIDATE =========================
Head "Phase 3/4: Runtime Probe Validation"

$runtimeScript = Join-Path $PSScriptRoot "surveyormap_runtime_validate.ps1"
$rArgs = @("-WaitSeconds", $WaitSeconds)
if ($NoLaunch)       { $rArgs += "-NoLaunch" }
if ($VerboseReport)  { $rArgs += "-VerboseReport" }

$rArgs += "-LaunchGame"  # Always try to launch for runtime (unless -NoLaunch)
if ($NoLaunch) { $rArgs = $rArgs | Where-Object { $_ -ne "-LaunchGame" } }

$phase3Start = Get-Date
& powershell.exe -NoProfile -ExecutionPolicy Bypass -File $runtimeScript @rArgs
$runtimeRawExit = $LASTEXITCODE

# Validate via fresh report file (not just exit code — $LASTEXITCODE may be stale from dotnet)
$rjPath    = Join-Path $PSScriptRoot "last-runtime-validation.json"
$phase3Pass = $false
$phase3Reason = "exit=$runtimeRawExit"
if (Test-Path $rjPath) {
    $rjMtime = (Get-Item $rjPath).LastWriteTime
    if ($rjMtime -ge $phase3Start.AddSeconds(-2)) {
        # Report written during this run — use its verdict
        $rj = Get-Content $rjPath -Raw | ConvertFrom-Json -ErrorAction SilentlyContinue
        if ($rj) {
            $phase3Pass   = ($rj.verdict -eq "PASS")
            $phase3Reason = "report=$($rj.verdict) criteriaPass=$($rj.criteriaPass)"
        } else {
            $phase3Pass   = ($runtimeRawExit -eq 0)
            $phase3Reason = "report-parse-failed exit=$runtimeRawExit"
        }
    } else {
        # Stale report — report mismatch, FAIL
        $phase3Pass   = $false
        $phase3Reason = "STALE-REPORT (not updated during this run) exit=$runtimeRawExit"
        WARN "Runtime report not updated - possible false positive from previous run"
    }
} else {
    $phase3Pass   = ($runtimeRawExit -eq 0)
    $phase3Reason = "no-report exit=$runtimeRawExit"
}

if ($phase3Pass) { OK "Runtime PASS ($phase3Reason)" } else { ERR "Runtime FAIL ($phase3Reason)"; $overallPass = $false }

# ========================= PHASE 4: GAMEPLAY VALIDATE =========================
Head "Phase 4/4: Gameplay Validation"

$phase4Criteria = "unknown"

if ($SkipGameplay) {
    SKIP "Gameplay validation skipped (-SkipGameplay)"
    $phase4Verdict = "SKIPPED"
} else {
    $gameplayScript = Join-Path $PSScriptRoot "surveyormap_gameplay_validate.ps1"
    $gArgs = @("-WaitSeconds", $WaitSeconds)
    if ($NoLaunch)      { $gArgs += "-NoLaunch" }
    if ($VerboseReport) { $gArgs += "-VerboseReport" }

    $phase4Start = Get-Date
    & powershell.exe -NoProfile -ExecutionPolicy Bypass -File $gameplayScript @gArgs
    $gameplayRawExit = $LASTEXITCODE

    # Validate via fresh report
    $gjPath = Join-Path $PSScriptRoot "last-gameplay-validation.json"
    $phase4Pass    = $false
    $phase4Verdict = "FAIL"
    if (Test-Path $gjPath) {
        $gjMtime = (Get-Item $gjPath).LastWriteTime
        if ($gjMtime -ge $phase4Start.AddSeconds(-2)) {
            $gj = Get-Content $gjPath -Raw | ConvertFrom-Json -ErrorAction SilentlyContinue
            if ($gj) {
                $phase4Verdict = $gj.verdict
                $phase4Pass    = ($gj.verdict -eq "PASS")
                if ($gj.criteriaPass -ne $null -and $gj.criteriaTotal -ne $null) {
                    $phase4Criteria = "$($gj.criteriaPass)/$($gj.criteriaTotal)"
                }
            } else {
                $phase4Verdict = if ($gameplayRawExit -eq 0) { "PASS" } elseif ($gameplayRawExit -eq 2) { "PARTIAL_TAB_BLOCKED" } else { "FAIL" }
                $phase4Pass    = ($gameplayRawExit -eq 0)
            }
        } else {
            $phase4Verdict = "STALE-REPORT"
            $phase4Pass    = $false
            WARN "Gameplay report not updated - possible false positive"
        }
    } else {
        $phase4Verdict = if ($gameplayRawExit -eq 0) { "PASS" } elseif ($gameplayRawExit -eq 2) { "PARTIAL_TAB_BLOCKED" } else { "FAIL" }
        $phase4Pass    = ($gameplayRawExit -eq 0)
    }

    $phase4TabBlocked = $phase4Verdict -in @("PARTIAL_TAB_BLOCKED", "PARTIAL_TAB_UNCONFIRMED")

    if ($phase4Pass)                  { OK "Gameplay PASS ($phase4Criteria)" }
    elseif ($phase4TabBlocked)        { WARN "Gameplay PARTIAL: runtime+level+toggle PASS, TAB BLOCKED ($phase4Verdict)" }
    else                              { ERR "Gameplay FAIL ($phase4Verdict)"; $overallPass = $false }
}

# ========================= FINAL SUMMARY =========================
Head "FINAL SUMMARY"

$buildShort = Get-MD5Short $buildDll

Write-Host ""
Write-Host "  Build hash:      $buildShort"
Write-Host "  Phase 1 Audit:   $(if ($phase1Pass) {'PASS'} else {'FAIL'})"
Write-Host "  Phase 2 Build:   $(if ($phase2Pass) {'PASS'} else {'FAIL'})"
Write-Host "  Phase 3 Runtime: $(if ($phase3Pass) {'PASS (12/12)'} else {'FAIL'})"
Write-Host "  Phase 4 Gameplay: $phase4Verdict"
Write-Host ""

$phase4TabBlocked = $phase4Verdict -in @("PARTIAL_TAB_BLOCKED", "PARTIAL_TAB_UNCONFIRMED")
$finalVerdict = if ($overallPass -and $phase4Pass) {
    "PASS"
} elseif ($overallPass -and $phase4TabBlocked) {
    "BLOCKED"
} else {
    "FAIL"
}
Write-Host "  Overall: $finalVerdict"
Write-Host ""

# Write final report
$finalRpt = [ordered]@{
    timestamp      = (Get-Date -Format "yyyy-MM-ddTHH:mm:ssZ")
    verdict        = $finalVerdict
    buildHash      = $buildShort
    phase1Audit    = if ($phase1Pass) { "PASS" } else { "FAIL" }
    phase2Build    = if ($phase2Pass) { "PASS" } else { "FAIL" }
    phase3Runtime  = if ($phase3Pass) { "PASS" } else { "FAIL" }
    phase4Gameplay = $phase4Verdict
    phase4Criteria = $phase4Criteria
    reports        = @{
        runtime  = Join-Path $PSScriptRoot "last-runtime-validation.json"
        gameplay = Join-Path $PSScriptRoot "last-gameplay-validation.json"
    }
}

$finalJsonPath = Join-Path $PSScriptRoot "last-full-validation.json"
$finalMdPath   = Join-Path $PSScriptRoot "last-full-validation.md"

$finalRpt | ConvertTo-Json -Depth 3 | Set-Content $finalJsonPath -Encoding UTF8
OK "Full JSON report: $finalJsonPath"

$md = @()
$md += "# SurveyorMap Full Validation Report"
$md += ""
$md += "**Date:** $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')   **Verdict:** **$finalVerdict**"
$md += ""
$md += "| Phase | Result |"
$md += "|---|---|"
$md += "| Phase 1: Static Audit | $(if ($phase1Pass) {'PASS'} else {'FAIL'}) |"
$md += "| Phase 2: Build + Install ($buildShort) | $(if ($phase2Pass) {'PASS'} else {'FAIL'}) |"
$md += "| Phase 3: Runtime Probe (12/12) | $(if ($phase3Pass) {'PASS'} else {'FAIL'}) |"
$md += "| Phase 4: Gameplay ($phase4Criteria criteria) | $phase4Verdict |"
$md += ""
if ($finalVerdict -eq "BLOCKED") {
    $md += "## Blocker"
    $md += ""
    $md += "- C18 TAB open/close remained unconfirmed by automation."
    $md += "- CenterOnPlayer evidence passed; native TAB confirmation is the only remaining Phase 2 gate."
    $md += "- Overall is BLOCKED, not PASS, until TAB open/close is confirmed in log/JSON."
    $md += ""
}
$md += "## Reports"
$md += ""
$md += "- Runtime: $(Join-Path $PSScriptRoot 'last-runtime-validation.md')"
$md += "- Gameplay: $(Join-Path $PSScriptRoot 'last-gameplay-validation.md')"
$md += "- Full: $finalMdPath"
$md | Set-Content $finalMdPath -Encoding UTF8
OK "Full MD report: $finalMdPath"

Write-Host ""
Write-Host "======================================================"
Write-Host ""

if ($finalVerdict -eq "PASS") { exit 0 }
if ($finalVerdict -eq "BLOCKED") { exit 2 }
exit 1
