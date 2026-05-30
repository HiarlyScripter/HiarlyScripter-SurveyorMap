# surveyormap_runtime_validate.ps1
# Validacao runtime - 12 criterios objetivos. PASS somente com todos verdadeiros.
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

if (Test-Path $buildDll) { OK "Build DLL found" } else { ERR "Build DLL NOT found: $buildDll" }
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

# ========================= F) 12 CRITERIA =========================
Head "F) 12 Criteria Validation"

$c = @{}

# C1 - Hash match
$c["01_hash"] = $hashMatch
if ($hashMatch) { OK "[C1]  hashMatch=true ($buildShort)" } else { ERR "[C1]  hashMatch=false" }

# C2 - Mod enabled
$c["02_mod"] = $modEnabled
if ($modEnabled) { OK "[C2]  modEnabled=true" } else { ERR "[C2]  modEnabled=false" }

# C3 - BuildTag in log
$logContent = @()
if (Test-Path $logPath) {
    $logContent = Get-Content $logPath -Encoding UTF8 -ErrorAction SilentlyContinue
}
$c["03_tag"] = (($logContent | Where-Object { $_ -match "BuildTag.*md5=$expectedTag" }).Count -gt 0)
if ($c["03_tag"]) { OK "[C3]  BuildTag '$expectedTag' in log" } else { ERR "[C3]  BuildTag '$expectedTag' NOT in log" }

# C4 - JSON fresh
$jsonExists = Test-Path $jsonPath
$jsonFresh  = $false
if ($jsonExists) {
    $jsonMtime = (Get-Item $jsonPath).LastWriteTime
    $jsonAgeMin = ((Get-Date) - $jsonMtime).TotalMinutes
    if ($NoLaunch) {
        $jsonFresh = $jsonAgeMin -le $freshnessMin
        if (-not $jsonFresh) { WARN "[C4]  JSON age=$([int]$jsonAgeMin)min > ${freshnessMin}min threshold - STALE" }
    } else {
        $jsonFresh = $jsonMtime -gt $gameStartTime
        if (-not $jsonFresh) { WARN "[C4]  JSON not updated since game launch" }
    }
}
$c["04_json"] = ($jsonExists -and $jsonFresh)
if ($c["04_json"]) { OK "[C4]  JSON exists and fresh (age=$([int]$jsonAgeMin)min)" } else { ERR "[C4]  JSON missing or stale" }

# Parse JSON
$script:jd = $null
$jsonParseOk = $false
if ($jsonExists) {
    try {
        $script:jd = (Get-Content $jsonPath -Raw -Encoding UTF8) | ConvertFrom-Json
        $jsonParseOk = $true
    } catch {
        ERR "[JSON] Parse failed: $_"
    }
}

# C5 - pluginAwakeCalled
$c["05_awake"] = ($jsonParseOk -and (JVal "pluginAwakeCalled" $false) -eq $true)
if ($c["05_awake"]) { OK "[C5]  pluginAwakeCalled=true" } else { ERR "[C5]  pluginAwakeCalled=false" }

# C6 - pluginUpdateCount > 0
$pluginUpd = [int](JVal "pluginUpdateCount" 0)
$c["06_plugin_upd"] = ($jsonParseOk -and $pluginUpd -gt 0)
if ($c["06_plugin_upd"]) { OK "[C6]  pluginUpdateCount=$pluginUpd > 0" } else { ERR "[C6]  pluginUpdateCount=0" }

# C7 - pluginOnGuiCount > 0
$pluginGui = [int](JVal "pluginOnGuiCount" 0)
$c["07_plugin_gui"] = ($jsonParseOk -and $pluginGui -gt 0)
if ($c["07_plugin_gui"]) { OK "[C7]  pluginOnGuiCount=$pluginGui > 0" } else { ERR "[C7]  pluginOnGuiCount=0" }

# C8 - runtimeProbeCreated
$c["08_probe"] = ($jsonParseOk -and (JVal "runtimeProbeCreated" $false) -eq $true)
if ($c["08_probe"]) { OK "[C8]  runtimeProbeCreated=true" } else { ERR "[C8]  runtimeProbeCreated=false" }

# C9 - runtimeProbeUpdateCount > 0
$probeUpd = [int](JVal "runtimeProbeUpdateCount" 0)
$c["09_probe_upd"] = ($jsonParseOk -and $probeUpd -gt 0)
if ($c["09_probe_upd"]) { OK "[C9]  runtimeProbeUpdateCount=$probeUpd > 0" } else { ERR "[C9]  runtimeProbeUpdateCount=0" }

# C10 - runtimeProbeOnGuiCount > 0
$probeGui = [int](JVal "runtimeProbeOnGuiCount" 0)
$c["10_probe_gui"] = ($jsonParseOk -and $probeGui -gt 0)
if ($c["10_probe_gui"]) { OK "[C10] runtimeProbeOnGuiCount=$probeGui > 0" } else { ERR "[C10] runtimeProbeOnGuiCount=0" }

# C11 - lastException null
$lastEx = JVal "lastException" $null
$c["11_no_ex"] = ($jsonParseOk -and ($null -eq $lastEx -or $lastEx.ToString() -eq ""))
if ($c["11_no_ex"]) { OK "[C11] lastException=null" } else { ERR "[C11] lastException=$lastEx" }

# C12 - lastErrorStack null
$lastStack = JVal "lastErrorStack" $null
$c["12_no_stack"] = ($jsonParseOk -and ($null -eq $lastStack -or $lastStack.ToString() -eq ""))
if ($c["12_no_stack"]) { OK "[C12] lastErrorStack=null" } else { ERR "[C12] lastErrorStack=$lastStack" }

# --- Informational log checks (not blocking) ---
Write-Host ""
Write-Host "  -- Log evidence (informational, not blocking) --"
$pluginUpdateLog = (($logContent | Where-Object { $_ -match "Plugin\.Update absolute alive" }).Count -gt 0)
$pluginOnGuiLog  = (($logContent | Where-Object { $_ -match "OnGUI absolute proof reached" }).Count -gt 0)
$probeUpdateLog  = (($logContent | Where-Object { $_ -match "RuntimeProbe\.Update alive" }).Count -gt 0)
$probeOnGuiLog   = (($logContent | Where-Object { $_ -match "RuntimeProbe\.OnGUI first call" }).Count -gt 0)
Write-Host "  $(LogIcon $pluginUpdateLog)   Plugin.Update alive in log"
Write-Host "  $(LogIcon $pluginOnGuiLog)    Plugin.OnGUI alive in log"
Write-Host "  $(LogIcon $probeUpdateLog)    RuntimeProbe.Update alive in log"
Write-Host "  $(LogIcon $probeOnGuiLog)     RuntimeProbe.OnGUI first call in log"

# --- Level diagnostics from JSON (if present) ---
if ($jsonParseOk -and $null -ne $script:jd) {
    $runState  = JVal "currentRunState" ""
    $hudVis    = JVal "hudVisible" $null
    $nativeTex = JVal "nativeTextureReady" $null
    $capReason = JVal "lastCaptureReason" ""
    $tabOpen   = JVal "nativeTabOpen" $null
    $togCount  = JVal "toggleKeyDetectedCount" $null
    if ($runState -ne "") {
        Write-Host ""
        Write-Host "  -- Level diagnostics --"
        Write-Host "  currentRunState    = $runState"
        if ($null -ne $hudVis)    { Write-Host "  hudVisible         = $hudVis" }
        if ($null -ne $nativeTex) { Write-Host "  nativeTextureReady = $nativeTex" }
        if ($capReason -ne "")    { Write-Host "  lastCaptureReason  = $capReason" }
        if ($null -ne $tabOpen)   { Write-Host "  nativeTabOpen      = $tabOpen" }
        if ($null -ne $togCount)  { Write-Host "  toggleKeyDetected  = $togCount" }
    }
}

if ($VerboseReport -and $jsonParseOk -and $null -ne $script:jd) {
    Write-Host ""
    Write-Host "  -- Full JSON --"
    $script:jd.PSObject.Properties | ForEach-Object { Write-Host "    $($_.Name) = $($_.Value)" }
}

# ========================= G) VERDICT =========================
Head "G) Verdict"

$passCount = ($c.Values | Where-Object { $_ -eq $true }).Count
$failCount = 12 - $passCount
$passAll   = ($failCount -eq 0)
$verdict   = if ($passAll) { "PASS" } else { "FAIL" }

$failCause = ""
if (-not $passAll) {
    if (-not $c["01_hash"])       { $failCause = "C1: DLL hash mismatch - reinstall from build/" }
    elseif (-not $c["02_mod"])    { $failCause = "C2: SurveyorMap disabled - enable via r2modman" }
    elseif (-not $c["03_tag"])    { $failCause = "C3: BuildTag not in log - wrong DLL or BepInEx load error" }
    elseif (-not $c["04_json"])   { $failCause = "C4: JSON missing or stale - run with -LaunchGame or ensure game ran recently" }
    elseif (-not $c["05_awake"])  { $failCause = "C5: pluginAwakeCalled=false - Awake() failed; check log for errors" }
    elseif (-not $c["06_plugin_upd"])  { $failCause = "C6: pluginUpdateCount=0 - Update() never called; check hideFlags/DontDestroyOnLoad" }
    elseif (-not $c["07_plugin_gui"])  { $failCause = "C7: pluginOnGuiCount=0 - OnGUI() never called" }
    elseif (-not $c["08_probe"])       { $failCause = "C8: runtimeProbeCreated=false - EnsureCreated() failed; check log" }
    elseif (-not $c["09_probe_upd"])   { $failCause = "C9: runtimeProbeUpdateCount=0 - probe Update() never called" }
    elseif (-not $c["10_probe_gui"])   { $failCause = "C10: runtimeProbeOnGuiCount=0 - probe OnGUI() never called" }
    elseif (-not $c["11_no_ex"])       { $failCause = "C11: lastException=$lastEx" }
    elseif (-not $c["12_no_stack"])    { $failCause = "C12: lastErrorStack set - check log" }
    else { $failCause = "Unknown" }
}

Write-Host ""
Write-Host "  Criteria: $passCount/12 pass, $failCount/12 fail"
Write-Host ""
if ($passAll) {
    Write-Host "  *** RESULT: PASS (12/12) ***"
} else {
    Write-Host "  *** RESULT: FAIL ($passCount/12) ***"
    Write-Host "  Root cause: $failCause"
}

# ========================= H) REPORTS =========================
Head "H) Reports"

# Lean JSON (only primitives)
$rpt = [ordered]@{
    timestamp      = (Get-Date -Format "yyyy-MM-ddTHH:mm:ssZ")
    verdict        = $verdict
    criteriaPass   = $passCount
    criteriaTotal  = 12
    failCause      = if ($passAll) { $null } else { $failCause }
    buildHash      = $buildShort
    installedHash  = (Get-MD5Short $installedDll)
    hashMatch      = $hashMatch
    modEnabled     = $modEnabled
    buildTagInLog  = $c["03_tag"]
    jsonFresh      = $c["04_json"]
    launchMethod   = if ($assistedMode) { "assisted" } else { $launchMethod }
    launchAuto     = $launchAuto
    waitSeconds    = $WaitSeconds
}

if ($jsonParseOk -and $null -ne $script:jd) {
    $rpt["pluginAwakeCalled"]       = [bool](JVal "pluginAwakeCalled" $false)
    $rpt["pluginUpdateCount"]       = [int](JVal "pluginUpdateCount" 0)
    $rpt["pluginOnGuiCount"]        = [int](JVal "pluginOnGuiCount" 0)
    $rpt["runtimeProbeCreated"]     = [bool](JVal "runtimeProbeCreated" $false)
    $rpt["runtimeProbeUpdateCount"] = [int](JVal "runtimeProbeUpdateCount" 0)
    $rpt["runtimeProbeOnGuiCount"]  = [int](JVal "runtimeProbeOnGuiCount" 0)
    $rpt["lastException"]           = (JVal "lastException" $null)
    $rpt["lastErrorStack"]          = (JVal "lastErrorStack" $null)
    $rpt["scene"]                   = (JVal "scene" "")
    $rpt["buildTag"]                = (JVal "buildTag" "")
    $rpt["timestampUtc"]            = (JVal "timestampUtc" "")
    foreach ($lf in @("currentRunState","hudVisible","nativeTextureReady","nativeMapCaptureReady","nativeTabOpen","toggleKeyDetectedCount","minimapBaselineVisible","lastGateReason","lastCaptureReason")) {
        $v = JVal $lf $null
        if ($null -ne $v) { $rpt[$lf] = $v }
    }
}

$recentLog = @()
if ($logContent.Count -gt 0) {
    $recentLog = ($logContent | Where-Object { $_ -match "\[SurveyorMap\]" }) | Select-Object -Last 12 | ForEach-Object { [string]$_ }
    if ($recentLog.Count -gt 0) { $rpt["recentLogLines"] = $recentLog }
}

$rpt | ConvertTo-Json -Depth 3 | Set-Content $reportJson -Encoding UTF8
OK "JSON: $reportJson"

$md = @()
$md += "# SurveyorMap Runtime Validation"
$md += ""
$md += "**Date:** $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')   **Verdict:** **$verdict** ($passCount/12)"
$md += ""
if (-not $passAll) {
    $md += "## Root Cause"
    $md += ""
    $md += $failCause
    $md += ""
}
$md += "## 12 Criteria"
$md += ""
$md += "| # | Criterion | Result |"
$md += "|---|---|---|"
$md += "| C1  | hashMatch: build == installed ($buildShort) | $(if ($c['01_hash']) {'PASS'} else {'FAIL'}) |"
$md += "| C2  | modEnabled: SurveyorMap enabled | $(if ($c['02_mod']) {'PASS'} else {'FAIL'}) |"
$md += "| C3  | BuildTag '$expectedTag' in LogOutput.log | $(if ($c['03_tag']) {'PASS'} else {'FAIL'}) |"
$md += "| C4  | runtime-state.json exists and fresh | $(if ($c['04_json']) {'PASS'} else {'FAIL'}) |"
$md += "| C5  | pluginAwakeCalled=true | $(if ($c['05_awake']) {'PASS'} else {'FAIL'}) |"
$md += "| C6  | pluginUpdateCount > 0 | $(if ($c['06_plugin_upd']) {'PASS'} else {'FAIL'}) |"
$md += "| C7  | pluginOnGuiCount > 0 | $(if ($c['07_plugin_gui']) {'PASS'} else {'FAIL'}) |"
$md += "| C8  | runtimeProbeCreated=true | $(if ($c['08_probe']) {'PASS'} else {'FAIL'}) |"
$md += "| C9  | runtimeProbeUpdateCount > 0 | $(if ($c['09_probe_upd']) {'PASS'} else {'FAIL'}) |"
$md += "| C10 | runtimeProbeOnGuiCount > 0 | $(if ($c['10_probe_gui']) {'PASS'} else {'FAIL'}) |"
$md += "| C11 | lastException=null | $(if ($c['11_no_ex']) {'PASS'} else {'FAIL'}) |"
$md += "| C12 | lastErrorStack=null | $(if ($c['12_no_stack']) {'PASS'} else {'FAIL'}) |"
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
    $md += "PASS 12/12. Advance to Fase 1 level validation if applicable."
} else {
    $md += "Fix: $failCause"
}
$md | Set-Content $reportMd -Encoding UTF8
OK "Markdown: $reportMd"

if ($CloseGame -and $null -ne $gameProcess -and -not $gameProcess.HasExited) {
    WARN "Closing game (PID=$($gameProcess.Id))"
    $gameProcess.Kill()
}

Head "FINAL: $verdict ($passCount/12)"
Write-Host ""
if (-not $passAll) { exit 1 } else { exit 0 }
