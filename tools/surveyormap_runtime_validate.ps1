# surveyormap_runtime_validate.ps1
# Validacao runtime SurveyorMap v2 — 8 criterios objetivos.
# PASS somente com todos verdadeiros.
# Uso: .\tools\surveyormap_runtime_validate.ps1 [-LaunchGame] [-WaitSeconds 60] [-NoLaunch] [-CloseGame] [-VerboseReport]

param(
    [switch]$LaunchGame,
    [int]$WaitSeconds = 60,
    [switch]$NoLaunch,
    [switch]$CloseGame,
    [switch]$VerboseReport
)

Set-StrictMode -Off
$ErrorActionPreference = "SilentlyContinue"

$proj         = Split-Path $PSScriptRoot -Parent
$buildDll     = Join-Path $proj "build\SurveyorMap.dll"
$profileRoot  = "C:\Users\Hiarly\AppData\Roaming\r2modmanPlus-local\REPO\profiles\REPO - Test"
$installedDll = "$profileRoot\BepInEx\plugins\HiarlyScripter-SurveyorMap\SurveyorMap.dll"
$logPath      = "$profileRoot\BepInEx\LogOutput.log"
$jsonPath     = "$profileRoot\BepInEx\plugins\HiarlyScripter-SurveyorMap\diagnostics\surveyormap-runtime-state.json"
$modsYml      = "$profileRoot\mods.yml"
$archiveDir   = Join-Path $proj "tools\archive"
$reportJson   = Join-Path $proj "tools\last-runtime-validation.json"
$reportMd     = Join-Path $proj "tools\last-runtime-validation.md"
$ts           = Get-Date -Format "yyyyMMdd-HHmmss"
$startTime    = Get-Date
$freshnessMin = 30

New-Item -ItemType Directory -Path $archiveDir -Force | Out-Null

function Head([string]$t) {
    Write-Host ""
    Write-Host "===================================================="
    Write-Host "  $t"
    Write-Host "===================================================="
}
function OK([string]$m)   { Write-Host "  [OK]   $m" }
function WARN([string]$m) { Write-Host "  [WARN] $m" }
function ERR([string]$m)  { Write-Host "  [FAIL] $m" }
function INFO([string]$m) { if ($VerboseReport) { Write-Host "  [INFO] $m" } }

function Get-MD5Short([string]$p) {
    if (-not (Test-Path $p)) { return "missing" }
    return (Get-FileHash -Path $p -Algorithm MD5).Hash.Substring(0,8).ToLower()
}
function Get-MD5Full([string]$p) {
    if (-not (Test-Path $p)) { return "missing" }
    return (Get-FileHash -Path $p -Algorithm MD5).Hash
}
function JVal([string]$field, $default) {
    if ($null -eq $script:jd) { return $default }
    $v = $script:jd.$field
    if ($null -eq $v) { return $default }
    return $v
}
function LogIcon([bool]$b) { if ($b) { "[OK]" } else { "[--]" } }

# ========================= A) PRE-CHECK =========================
Head "A) Pre-Check"

if (Test-Path $buildDll)     { OK "Build DLL found" }     else { ERR "Build DLL NOT found: $buildDll" }
if (Test-Path $installedDll) { OK "Installed DLL found" } else { ERR "Installed DLL NOT found" }

$buildHash     = Get-MD5Full $buildDll
$installedHash = Get-MD5Full $installedDll
$buildShort    = Get-MD5Short $buildDll
$hashMatch     = ($buildHash -ne "missing" -and $buildHash -eq $installedHash)

if ($hashMatch) { OK "Hash match: $buildShort" } else { ERR "Hash MISMATCH: build=$buildShort installed=$(Get-MD5Short $installedDll)" }

$modEnabled = $false
if (Test-Path $modsYml) {
    $modsContent = Get-Content $modsYml -Raw -Encoding UTF8
    if ($modsContent -match 'HiarlyScripter-SurveyorMap') {
        if ($modsContent -match 'enabled:\s*false') {
            WARN "SurveyorMap disabled in mods.yml - enabling..."
            Copy-Item $modsYml "$modsYml.backup-$ts" -Force
            $modsContent = $modsContent -replace '(enabled:\s*)false', '${1}true'
            Set-Content $modsYml $modsContent -Encoding UTF8
            OK "SurveyorMap enabled=true (backup saved)"
            $modEnabled = $true
        } else {
            $modEnabled = $true
            OK "SurveyorMap enabled in mods.yml"
        }
    } else {
        WARN "SurveyorMap not found in mods.yml (assuming enabled)"
        $modEnabled = $true
    }
} else {
    WARN "mods.yml not found (assuming enabled)"
    $modEnabled = $true
}

# ========================= B) ARCHIVE =========================
Head "B) Archive"

if (Test-Path $logPath) {
    Copy-Item $logPath (Join-Path $archiveDir "LogOutput-$ts.log") -Force
    OK "LogOutput.log archived"
}

if ($NoLaunch) {
    if (Test-Path $jsonPath) { INFO "JSON preserved (-NoLaunch)" }
} else {
    if (Test-Path $jsonPath) {
        Copy-Item $jsonPath (Join-Path $archiveDir "runtime-state-$ts.json") -Force
        Remove-Item $jsonPath -Force
        OK "runtime-state.json archived and cleaned"
    }
}

# ========================= C) DISCOVER LAUNCH =========================
Head "C) Launch Method"

$repoExe      = $null
$launchMethod = "assisted"

$directPaths = @(
    "E:\SteamLibrary\steamapps\common\REPO\REPO.exe",
    "D:\SteamLibrary\steamapps\common\REPO\REPO.exe",
    "C:\Program Files (x86)\Steam\steamapps\common\REPO\REPO.exe",
    "C:\Program Files\Steam\steamapps\common\REPO\REPO.exe"
)
foreach ($dp in $directPaths) {
    if (Test-Path $dp) { $repoExe = $dp; break }
}

if ($repoExe) {
    $winhttp = Join-Path (Split-Path $repoExe -Parent) "winhttp.dll"
    if (Test-Path $winhttp) { $launchMethod = "doorstop-cfg-patch" }
    else                    { $launchMethod = "steam-protocol" }
    INFO "REPO.exe: $repoExe"
}
Write-Host "  Launch method: $launchMethod"

# ========================= D) LAUNCH =========================
Head "D) Launch"

$assistedMode  = $false
$launchAuto    = $false
$gameProcess   = $null
$gameStartTime = Get-Date

if ($NoLaunch) {
    OK "Skipping launch (-NoLaunch)."
} elseif ($LaunchGame) {
    if ($launchMethod -eq "doorstop-cfg-patch" -and $repoExe) {
        Write-Host "  Patching doorstop_config.ini for REPO - Test..."
        $gameDir      = Split-Path $repoExe -Parent
        $doorstopCfg  = Join-Path $gameDir "doorstop_config.ini"
        $doorstopBak  = "$doorstopCfg.surveyormap.bak"
        $bepPreloader = "$profileRoot\BepInEx\core\BepInEx.Preloader.dll"
        if (-not (Test-Path $bepPreloader)) {
            $bepPreloader = "$profileRoot\BepInEx\core\BepInEx.Unity.Mono.Preloader.dll"
        }
        $doorstopPatched = $false
        try {
            if ((Test-Path $doorstopCfg) -and (Test-Path $bepPreloader)) {
                Copy-Item $doorstopCfg $doorstopBak -Force
                $cfgContent = Get-Content $doorstopCfg -Raw -Encoding UTF8
                $cfgContent = $cfgContent -replace '(?m)^target_assembly\s*=.*$', "target_assembly=$bepPreloader"
                $cfgContent = $cfgContent -replace '(?m)^enabled\s*=.*$', "enabled=true"
                Set-Content $doorstopCfg $cfgContent -Encoding UTF8
                $doorstopPatched = $true
                INFO "Doorstop patched: target=$bepPreloader"
            }
            Start-Sleep -Seconds 1
            $gameProcess = Start-Process -FilePath $repoExe -PassThru
            if ($gameProcess) {
                OK "Game launched (PID=$($gameProcess.Id))"
                $launchAuto    = $true
                $gameStartTime = Get-Date
                Start-Sleep -Seconds 2
            } else {
                WARN "Process.Start returned null - falling back to assisted"
                $assistedMode = $true
            }
        } catch {
            WARN "Auto-launch failed: $_ - falling back to assisted"
            $assistedMode = $true
        } finally {
            if ($doorstopPatched -and (Test-Path $doorstopBak)) {
                Start-Sleep -Seconds 3
                Copy-Item $doorstopBak $doorstopCfg -Force
                Remove-Item $doorstopBak -Force -ErrorAction SilentlyContinue
                INFO "Doorstop restored"
            }
        }
    } elseif ($launchMethod -eq "steam-protocol") {
        try {
            Start-Process "steam://rungameid/3241660"
            OK "Steam protocol sent"
            $launchAuto    = $true
            $gameStartTime = Get-Date
        } catch {
            WARN "Steam protocol failed - assisted mode"
            $assistedMode = $true
        }
    } else {
        $assistedMode = $true
    }

    if ($assistedMode) {
        Write-Host ""
        Write-Host "  ============================================================"
        Write-Host "  Launch automatico indisponivel."
        Write-Host "  Abra r2modman -> REPO - Test -> Start modded."
        Write-Host "  Aguarde o menu e pressione ENTER."
        Write-Host "  ============================================================"
        Write-Host ""
        $null = Read-Host "  ENTER quando o jogo estiver no menu"
        $gameStartTime = Get-Date
    }
} else {
    Write-Host "  -LaunchGame nao informado."
}

# ========================= E) SIGNAL DETECTION =========================
Head "E) Signal Detection"

$waited        = 0
$logFound      = $false
$buildTagFound = $false
$jsonFound     = $false
$expectedTag   = Get-MD5Short $buildDll

if ($NoLaunch) {
    Write-Host "  Reading state immediately (-NoLaunch)."
    $logFound  = (Test-Path $logPath)
    $jsonFound = (Test-Path $jsonPath)
    if ($logFound) {
        $lc = Get-Content $logPath -Encoding UTF8 -ErrorAction SilentlyContinue
        $buildTagFound = (($lc | Where-Object { $_ -match "BuildTag.*md5=$expectedTag" }).Count -gt 0)
    }
    if ($logFound)      { INFO "Log found" }       else { WARN "Log not found" }
    if ($buildTagFound) { INFO "BuildTag found" }  else { WARN "BuildTag '$expectedTag' not in log" }
    if ($jsonFound)     { INFO "JSON found" }      else { WARN "JSON not found" }
} elseif ($LaunchGame) {
    Write-Host "  Waiting up to ${WaitSeconds}s..."
    $interval = 3
    while ($waited -le $WaitSeconds) {
        Start-Sleep -Seconds $interval
        $waited += $interval
        if (-not $logFound -and (Test-Path $logPath)) {
            $mtime = (Get-Item $logPath).LastWriteTime
            if ($mtime -gt $gameStartTime) { $logFound = $true; INFO "Log appeared" }
        }
        if ($logFound -and -not $buildTagFound) {
            $lc = Get-Content $logPath -Encoding UTF8 -ErrorAction SilentlyContinue
            if (($lc | Where-Object { $_ -match "BuildTag.*md5=$expectedTag" }).Count -gt 0) {
                $buildTagFound = $true; INFO "BuildTag found"
            }
        }
        if (-not $jsonFound -and (Test-Path $jsonPath)) {
            $mtime = (Get-Item $jsonPath).LastWriteTime
            if ($mtime -gt $gameStartTime) { $jsonFound = $true; INFO "JSON appeared" }
        }
        if ($logFound -and $buildTagFound -and $jsonFound) { OK "All signals after ${waited}s"; break }
        Write-Host "  ${waited}s/${WaitSeconds}s  log=$logFound tag=$buildTagFound json=$jsonFound"
    }
    if (-not $logFound)      { WARN "Log not updated after ${WaitSeconds}s" }
    if (-not $buildTagFound) { WARN "BuildTag not found after ${WaitSeconds}s" }
    if (-not $jsonFound)     { WARN "JSON not created after ${WaitSeconds}s" }
}

# ========================= F) 8 CRITERIA =========================
Head "F) 8 Criteria Validation"

$c = @{}

# C1 - Hash match
$c["01_hash"] = $hashMatch
if ($hashMatch) { OK "[C1] hashMatch=true ($buildShort)" } else { ERR "[C1] hashMatch=false" }

# C2 - Mod enabled
$c["02_mod"] = $modEnabled
if ($modEnabled) { OK "[C2] modEnabled=true" } else { ERR "[C2] modEnabled=false" }

# C3 - Plugin loaded in log
$logContent = @()
if (Test-Path $logPath) {
    $logContent = Get-Content $logPath -Encoding UTF8 -ErrorAction SilentlyContinue
}
$pluginLoaded = (($logContent | Where-Object { $_ -match "\[SurveyorMap\].*v1\.0\.0 loaded" }).Count -gt 0)
$c["03_loaded"] = $pluginLoaded
if ($pluginLoaded) { OK "[C3] Plugin v1.0.0 loaded in log" } else { ERR "[C3] Plugin load line NOT in log" }

# C4 - BuildTag (MD5 short) in log
$c["04_tag"] = (($logContent | Where-Object { $_ -match "BuildTag.*md5=$expectedTag" }).Count -gt 0)
if ($c["04_tag"]) { OK "[C4] BuildTag '$expectedTag' in log" } else { ERR "[C4] BuildTag '$expectedTag' NOT in log" }

# C5 - JSON exists and fresh
$jsonExists = Test-Path $jsonPath
$jsonFresh  = $false
$jsonAgeMin = 9999
if ($jsonExists) {
    $jsonMtime  = (Get-Item $jsonPath).LastWriteTime
    $jsonAgeMin = ((Get-Date) - $jsonMtime).TotalMinutes
    if ($NoLaunch) {
        $jsonFresh = $jsonAgeMin -le $freshnessMin
        if (-not $jsonFresh) { WARN "[C5] JSON age=$([int]$jsonAgeMin)min > ${freshnessMin}min threshold - STALE" }
    } else {
        $jsonFresh = $jsonMtime -gt $gameStartTime
        if (-not $jsonFresh) { WARN "[C5] JSON not updated since game launch" }
    }
}
$c["05_json"] = ($jsonExists -and $jsonFresh)
if ($c["05_json"]) { OK "[C5] JSON exists and fresh (age=$([int]$jsonAgeMin)min)" } else { ERR "[C5] JSON missing or stale" }

# Parse JSON
$script:jd   = $null
$jsonParseOk = $false
if ($jsonExists) {
    try {
        $script:jd   = (Get-Content $jsonPath -Raw -Encoding UTF8) | ConvertFrom-Json
        $jsonParseOk = $true
    } catch { ERR "[JSON] Parse failed: $_" }
}

# C6 - buildTimestamp in JSON (confirms Awake() ran)
$jsonBuildTs = JVal "buildTimestamp" ""
$c["06_awake"] = ($jsonParseOk -and $jsonBuildTs -ne "")
if ($c["06_awake"]) { OK "[C6] buildTimestamp in JSON: $jsonBuildTs" } else { ERR "[C6] buildTimestamp missing — Awake() may not have run" }

# C7 - hudVisible=true (default state, confirms Update() ran)
$hudVis = JVal "hudVisible" $null
$c["07_hud"] = ($jsonParseOk -and $null -ne $hudVis)
if ($c["07_hud"]) { OK "[C7] hudVisible=$hudVis in JSON" } else { ERR "[C7] hudVisible field missing in JSON" }

# C8 - No lastException
$lastEx = JVal "lastException" $null
$c["08_no_ex"] = ($jsonParseOk -and ($null -eq $lastEx -or $lastEx.ToString() -eq ""))
if ($c["08_no_ex"]) { OK "[C8] lastException empty" } else { ERR "[C8] lastException=$lastEx" }

# --- Informational from JSON (not blocking for runtime — require gameplay) ---
Write-Host ""
Write-Host "  -- Gameplay signals (informational — require in-level run) --"
$texReady = JVal "nativeTextureReady" $false
$gpActive = JVal "gameplayActive"     $false
$tabAct   = JVal "tabActive"          $false
$rvTotal  = JVal "revealRoomsTotal"   0
$rvExp    = JVal "revealRoomsExplored" 0
$enemyCnt = JVal "enemyMarkerCount"   0
Write-Host "  $(LogIcon $texReady) nativeTextureReady=$texReady"
Write-Host "  $(LogIcon $gpActive) gameplayActive=$gpActive"
Write-Host "  $(LogIcon ($rvTotal -gt 0)) revealRooms=$rvExp/$rvTotal"
Write-Host "  $(LogIcon ($enemyCnt -gt 0)) enemyMarkerCount=$enemyCnt"
Write-Host "  [--] tabActive=$tabAct (informational)"

# --- Log evidence ---
Write-Host ""
Write-Host "  -- Log evidence --"
$cameraFound = (($logContent | Where-Object { $_ -match "Map camera found" }).Count -gt 0)
$revealLog   = (($logContent | Where-Object { $_ -match "RevealRooms: explored=" }).Count -gt 0)
$markerLog   = (($logContent | Where-Object { $_ -match "Enemy marker added" }).Count -gt 0)
$toggleLog   = (($logContent | Where-Object { $_ -match "M toggle" }).Count -gt 0)
Write-Host "  $(LogIcon $cameraFound) Map camera found in log"
Write-Host "  $(LogIcon $revealLog)   RevealRooms log line"
Write-Host "  $(LogIcon $markerLog)   Enemy marker added in log"
Write-Host "  $(LogIcon $toggleLog)   M toggle in log"

if ($VerboseReport -and $jsonParseOk -and $null -ne $script:jd) {
    Write-Host ""
    Write-Host "  -- Full JSON --"
    $script:jd.PSObject.Properties | ForEach-Object { Write-Host "    $($_.Name) = $($_.Value)" }
}

# ========================= G) VERDICT =========================
Head "G) Verdict"

$passCount = ($c.Values | Where-Object { $_ -eq $true }).Count
$failCount = 8 - $passCount
$passAll   = ($failCount -eq 0)
$verdict   = if ($passAll) { "PASS" } else { "FAIL" }

$failCause = ""
if (-not $passAll) {
    if (-not $c["01_hash"])   { $failCause = "C1: DLL hash mismatch — reinstall from build/" }
    elseif (-not $c["02_mod"])   { $failCause = "C2: SurveyorMap disabled — enable via r2modman" }
    elseif (-not $c["03_loaded"]) { $failCause = "C3: Plugin load line not in log — BepInEx error or wrong DLL" }
    elseif (-not $c["04_tag"])   { $failCause = "C4: BuildTag '$expectedTag' not in log — old DLL may still be loaded" }
    elseif (-not $c["05_json"])  { $failCause = "C5: JSON missing/stale — run game with new DLL first" }
    elseif (-not $c["06_awake"]) { $failCause = "C6: buildTimestamp missing — Awake() failed; check log" }
    elseif (-not $c["07_hud"])   { $failCause = "C7: hudVisible not in JSON — Update()/WriteDiagJson() not running" }
    elseif (-not $c["08_no_ex"]) { $failCause = "C8: lastException=$lastEx" }
    else { $failCause = "Unknown" }
}

Write-Host ""
Write-Host "  Criteria: $passCount/8 pass, $failCount/8 fail"
Write-Host ""
if ($passAll) {
    Write-Host "  *** RESULT: PASS (8/8) ***"
} else {
    Write-Host "  *** RESULT: FAIL ($passCount/8) ***"
    Write-Host "  Root cause: $failCause"
}

# ========================= H) REPORTS =========================
Head "H) Reports"

$rpt = [ordered]@{
    timestamp      = (Get-Date -Format "yyyy-MM-ddTHH:mm:ssZ")
    verdict        = $verdict
    criteriaPass   = $passCount
    criteriaTotal  = 8
    failCause      = if ($passAll) { $null } else { $failCause }
    buildHash      = $buildShort
    installedHash  = (Get-MD5Short $installedDll)
    hashMatch      = $hashMatch
    modEnabled     = $modEnabled
    pluginLoaded   = $pluginLoaded
    buildTagInLog  = $c["04_tag"]
    jsonFresh      = $c["05_json"]
    launchMethod   = if ($assistedMode) { "assisted" } else { $launchMethod }
    launchAuto     = $launchAuto
    waitSeconds    = $WaitSeconds
}

if ($jsonParseOk -and $null -ne $script:jd) {
    foreach ($f in @("buildTimestamp","buildMd5Short","assemblyPath","hudVisible","nativeTextureReady",
                     "gameplayActive","tabActive","revealRoomsTotal","revealRoomsExplored",
                     "enemyMarkerCount","lastException","writeTimeUtc")) {
        $v = JVal $f $null
        if ($null -ne $v) { $rpt[$f] = $v }
    }
}

$recentLog = @()
if ($logContent.Count -gt 0) {
    $recentLog = ($logContent | Where-Object { $_ -match "\[SurveyorMap\]" }) | Select-Object -Last 15 | ForEach-Object { [string]$_ }
    if ($recentLog.Count -gt 0) { $rpt["recentLogLines"] = $recentLog }
}

$rpt | ConvertTo-Json -Depth 3 | Set-Content $reportJson -Encoding UTF8
OK "JSON: $reportJson"

$md = @()
$md += "# SurveyorMap Runtime Validation v2"
$md += ""
$md += "**Date:** $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')   **Verdict:** **$verdict** ($passCount/8)"
$md += ""
if (-not $passAll) {
    $md += "## Root Cause"
    $md += ""
    $md += $failCause
    $md += ""
}
$md += "## 8 Criteria"
$md += ""
$md += "| # | Criterion | Result |"
$md += "|---|---|---|"
$md += "| C1 | hashMatch: build == installed ($buildShort) | $(if ($c['01_hash']) {'PASS'} else {'FAIL'}) |"
$md += "| C2 | modEnabled | $(if ($c['02_mod']) {'PASS'} else {'FAIL'}) |"
$md += "| C3 | Plugin v1.0.0 loaded in log | $(if ($c['03_loaded']) {'PASS'} else {'FAIL'}) |"
$md += "| C4 | BuildTag '$expectedTag' in log | $(if ($c['04_tag']) {'PASS'} else {'FAIL'}) |"
$md += "| C5 | runtime-state.json fresh | $(if ($c['05_json']) {'PASS'} else {'FAIL'}) |"
$md += "| C6 | buildTimestamp in JSON (Awake ran) | $(if ($c['06_awake']) {'PASS'} else {'FAIL'}) |"
$md += "| C7 | hudVisible in JSON (Update/WriteDiag ran) | $(if ($c['07_hud']) {'PASS'} else {'FAIL'}) |"
$md += "| C8 | lastException empty | $(if ($c['08_no_ex']) {'PASS'} else {'FAIL'}) |"
$md += ""
if ($recentLog.Count -gt 0) {
    $md += "## Recent SurveyorMap Log"
    $md += '```'
    $recentLog | ForEach-Object { $md += $_ }
    $md += '```'
    $md += ""
}
$md += "## Next Step"
$md += ""
if ($passAll) {
    $md += "PASS 8/8. Advance to gameplay validation."
} else {
    $md += "Fix: $failCause"
}
$md | Set-Content $reportMd -Encoding UTF8
OK "Markdown: $reportMd"

if ($CloseGame -and $null -ne $gameProcess -and -not $gameProcess.HasExited) {
    WARN "Closing game (PID=$($gameProcess.Id))"
    $gameProcess.Kill()
}

Head "FINAL: $verdict ($passCount/8)"
Write-Host ""
if (-not $passAll) { exit 1 } else { exit 0 }
