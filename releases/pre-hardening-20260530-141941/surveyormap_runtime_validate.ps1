# surveyormap_runtime_validate.ps1
# Validacao runtime completa do SurveyorMap.
# Uso: .\tools\surveyormap_runtime_validate.ps1 -LaunchGame -WaitSeconds 60 -VerboseReport

param(
    [switch]$LaunchGame,
    [int]$WaitSeconds = 60,
    [switch]$NoLaunch,
    [switch]$CloseGame,
    [switch]$VerboseReport
)

Set-StrictMode -Off
$ErrorActionPreference = "SilentlyContinue"

$proj        = Split-Path $PSScriptRoot -Parent
$buildDll    = Join-Path $proj "build\SurveyorMap.dll"
$profileRoot = "C:\Users\Hiarly\AppData\Roaming\r2modmanPlus-local\REPO\profiles\REPO - Test"
$installedDll = "$profileRoot\BepInEx\plugins\HiarlyScripter-SurveyorMap\SurveyorMap.dll"
$logPath      = "$profileRoot\BepInEx\LogOutput.log"
$jsonPath     = "$profileRoot\BepInEx\plugins\HiarlyScripter-SurveyorMap\diagnostics\surveyormap-runtime-state.json"
$modsYml      = "$profileRoot\mods.yml"
$archiveDir   = Join-Path $proj "tools\archive"
$reportJson   = Join-Path $proj "tools\last-runtime-validation.json"
$reportMd     = Join-Path $proj "tools\last-runtime-validation.md"
$ts           = Get-Date -Format "yyyyMMdd-HHmmss"

New-Item -ItemType Directory -Path $archiveDir -Force | Out-Null

function Sep  { Write-Host "------------------------------------------------------" }
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

function Get-MD5Short([string]$path) {
    if (-not (Test-Path $path)) { return "missing" }
    $hash = (Get-FileHash -Path $path -Algorithm MD5).Hash
    return $hash.Substring(0, 8).ToLower()
}
function Get-MD5Full([string]$path) {
    if (-not (Test-Path $path)) { return "missing" }
    return (Get-FileHash -Path $path -Algorithm MD5).Hash
}

# ─── A) PRE-CHECK ────────────────────────────────────────────────────────────
Head "A) Pre-Check"

if (Test-Path $buildDll) {
    OK "Build DLL found: $buildDll"
} else {
    ERR "Build DLL NOT found: $buildDll"
}

if (Test-Path $installedDll) {
    OK "Installed DLL found: $installedDll"
} else {
    ERR "Installed DLL NOT found: $installedDll"
}

$buildHash     = Get-MD5Full $buildDll
$installedHash = Get-MD5Full $installedDll
$buildShort    = Get-MD5Short $buildDll
$hashMatch     = ($buildHash -ne "missing" -and $buildHash -eq $installedHash)

if ($hashMatch) {
    OK "Hash match: $buildShort (build == installed)"
} else {
    ERR "Hash MISMATCH: build=$buildShort  installed=$(Get-MD5Short $installedDll)"
}

# mods.yml
$modEnabled = $false
if (Test-Path $modsYml) {
    $modsYmlContent = Get-Content $modsYml -Raw -Encoding UTF8
    if ($modsYmlContent -match 'HiarlyScripter-SurveyorMap') {
        if ($modsYmlContent -match 'enabled:\s*false') {
            WARN "SurveyorMap may be disabled in mods.yml - attempting to enable..."
            Copy-Item $modsYml "$modsYml.backup-$ts" -Force
            $modsYmlContent = $modsYmlContent -replace '(enabled:\s*)false', '${1}true'
            Set-Content $modsYml $modsYmlContent -Encoding UTF8
            OK "SurveyorMap enabled=true set (backup saved)"
            $modEnabled = $true
        } else {
            $modEnabled = $true
            OK "SurveyorMap found and enabled in mods.yml"
        }
    } else {
        WARN "SurveyorMap not found in mods.yml"
        $modEnabled = $true
    }
} else {
    WARN "mods.yml not found at $modsYml (assuming enabled)"
    $modEnabled = $true
}

# ─── B) ARCHIVE ──────────────────────────────────────────────────────────────
Head "B) Archive Previous State"

if (Test-Path $logPath) {
    Copy-Item $logPath (Join-Path $archiveDir "LogOutput-$ts.log") -Force
    OK "LogOutput.log archived"
}
if (-not $NoLaunch -and (Test-Path $jsonPath)) {
    Copy-Item $jsonPath (Join-Path $archiveDir "runtime-state-$ts.json") -Force
    Remove-Item $jsonPath -Force
    OK "runtime-state.json archived and cleaned"
} elseif ($NoLaunch -and (Test-Path $jsonPath)) {
    INFO "runtime-state.json preserved (-NoLaunch mode reads existing state)"
}

# ─── C) DISCOVER LAUNCH ──────────────────────────────────────────────────────
Head "C) Discover Launch Method"

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
    INFO "REPO.exe: $repoExe"
    $gameDir = Split-Path $repoExe -Parent
    $winhttp = Join-Path $gameDir "winhttp.dll"
    if (Test-Path $winhttp) {
        $launchMethod = "doorstop-env"
        INFO "Doorstop found: $winhttp"
    } else {
        $launchMethod = "steam-protocol"
    }
} else {
    INFO "REPO.exe not found in standard paths"
}

Write-Host "  Launch method: $launchMethod"

# ─── D) LAUNCH ───────────────────────────────────────────────────────────────
Head "D) Launch"

$assistedMode  = $false
$launchAuto    = $false
$gameProcess   = $null
$gameStartTime = Get-Date

if ($NoLaunch) {
    OK "Skipping launch (-NoLaunch). Reading existing logs."
} elseif ($LaunchGame) {
    if ($launchMethod -eq "doorstop-env" -and $repoExe) {
        Write-Host "  Attempting auto-launch (temporarily updating doorstop_config.ini)..."
        $gameDir    = Split-Path $repoExe -Parent
        $doorstopCfg = Join-Path $gameDir "doorstop_config.ini"
        $doorstopBak = "$doorstopCfg.surveyormap.bak"
        $bepPreloader = "$profileRoot\BepInEx\core\BepInEx.Preloader.dll"
        if (-not (Test-Path $bepPreloader)) {
            $bepPreloader = "$profileRoot\BepInEx\core\BepInEx.Unity.Mono.Preloader.dll"
        }
        $doorstopPatched = $false

        try {
            if ((Test-Path $doorstopCfg) -and (Test-Path $bepPreloader)) {
                # Backup and patch doorstop_config.ini to point at REPO-Test profile
                Copy-Item $doorstopCfg $doorstopBak -Force
                $cfg = Get-Content $doorstopCfg -Raw -Encoding UTF8
                $safePath = $bepPreloader.Replace('\', '\\')
                $cfg = $cfg -replace '(?m)^target_assembly\s*=.*$', "target_assembly=$bepPreloader"
                $cfg = $cfg -replace '(?m)^enabled\s*=.*$', "enabled=true"
                Set-Content $doorstopCfg $cfg -Encoding UTF8
                $doorstopPatched = $true
                INFO "doorstop_config.ini patched: target_assembly=$bepPreloader"
            }

            Start-Sleep -Seconds 1
            $gameProcess = Start-Process -FilePath $repoExe -PassThru
            if ($gameProcess) {
                OK "Game launched (PID=$($gameProcess.Id))"
                $launchAuto = $true
                Start-Sleep -Seconds 2
            } else {
                WARN "Process.Start returned null, falling back to assisted"
                $assistedMode = $true
            }
        } catch {
            WARN "Auto-launch failed: $_ -- falling back to assisted"
            $assistedMode = $true
        } finally {
            if ($doorstopPatched -and (Test-Path $doorstopBak)) {
                Start-Sleep -Seconds 3
                Copy-Item $doorstopBak $doorstopCfg -Force
                Remove-Item $doorstopBak -Force -ErrorAction SilentlyContinue
                INFO "doorstop_config.ini restored"
            }
        }
    } elseif ($launchMethod -eq "steam-protocol") {
        try {
            Start-Process "steam://rungameid/3241660"
            OK "Steam protocol sent (may not use REPO-Test profile mods)"
            $launchAuto = $true
        } catch {
            WARN "Steam protocol failed: $_ -- falling back to assisted"
            $assistedMode = $true
        }
    } else {
        $assistedMode = $true
    }

    if ($assistedMode) {
        Write-Host ""
        Write-Host "  ============================================================"
        Write-Host "  NAO consegui iniciar automaticamente."
        Write-Host "  Abra o r2modman, selecione REPO - Test e clique Start modded."
        Write-Host "  Depois pressione ENTER aqui para continuar."
        Write-Host "  ============================================================"
        Write-Host ""
        $null = Read-Host "  Pressione ENTER quando o jogo estiver rodando"
        $gameStartTime = Get-Date
    }
} else {
    Write-Host "  -LaunchGame nao informado. Lendo logs existentes."
}

# ─── E) WAIT ─────────────────────────────────────────────────────────────────
Head "E) Waiting"

$waited        = 0
$logFound      = $false
$buildTagFound = $false
$jsonFound     = $false
$expectedTag   = Get-MD5Short $buildDll

if ($NoLaunch) {
    # Read immediately — no loop, no time-gate, just check what's on disk now
    Write-Host "  -NoLaunch: reading current state immediately."
    $logFound  = Test-Path $logPath
    $jsonFound = Test-Path $jsonPath
    if ($logFound) {
        $lc = Get-Content $logPath -Encoding UTF8 -ErrorAction SilentlyContinue
        $tagLine = $lc | Where-Object { $_ -match "BuildTag.*md5=$expectedTag" } | Select-Object -First 1
        if ($tagLine) { $buildTagFound = $true }
    }
    if ($logFound)      { INFO "Log: $logPath" }     else { WARN "Log not found" }
    if ($buildTagFound) { INFO "BuildTag found" }    else { WARN "BuildTag '$expectedTag' not found in log" }
    if ($jsonFound)     { INFO "JSON found" }        else { WARN "runtime-state.json not found" }
} elseif ($LaunchGame) {
    Write-Host "  Waiting up to ${WaitSeconds}s for game to produce log and JSON..."
    $interval = 3
    while ($waited -le $WaitSeconds) {
        Start-Sleep -Seconds $interval
        $waited += $interval

        if (-not $logFound -and (Test-Path $logPath)) {
            $mtime = (Get-Item $logPath).LastWriteTime
            if ($mtime -gt $gameStartTime) {
                $logFound = $true
                INFO "Log appeared/updated (mtime=$mtime)"
            }
        }

        if ($logFound -and -not $buildTagFound) {
            $lc = Get-Content $logPath -Encoding UTF8 -ErrorAction SilentlyContinue
            $tagLine = $lc | Where-Object { $_ -match "BuildTag.*md5=$expectedTag" } | Select-Object -First 1
            if ($tagLine) { $buildTagFound = $true; INFO "BuildTag found in log" }
        }

        if (-not $jsonFound -and (Test-Path $jsonPath)) {
            $jmtime = (Get-Item $jsonPath).LastWriteTime
            if ($jmtime -gt $gameStartTime) { $jsonFound = $true; INFO "runtime-state.json appeared" }
        }

        if ($logFound -and $buildTagFound -and $jsonFound) {
            OK "All signals found after ${waited}s"
            break
        }
        Write-Host "  Waiting ${waited}s/${WaitSeconds}s  log=$logFound  tag=$buildTagFound  json=$jsonFound"
    }

    if (-not $logFound)      { WARN "Log not updated after ${WaitSeconds}s" }
    if (-not $buildTagFound) { WARN "BuildTag '$expectedTag' not in log after ${WaitSeconds}s" }
    if (-not $jsonFound)     { WARN "runtime-state.json not created after ${WaitSeconds}s" }
}

# ─── F) VALIDATE ─────────────────────────────────────────────────────────────
Head "F) Validation"

$r = @{}

$r["hash_match"]  = $hashMatch
$r["mod_enabled"] = $modEnabled
$r["log_exists"]  = (Test-Path $logPath)

if ($r["hash_match"])  { OK "Hash match: $buildShort" } else { ERR "Hash mismatch" }
if ($r["mod_enabled"]) { OK "SurveyorMap enabled" }     else { ERR "SurveyorMap disabled" }
if ($r["log_exists"])  { OK "LogOutput.log exists" }    else { ERR "LogOutput.log not found" }

$logContent = @()
if (Test-Path $logPath) {
    $logContent = Get-Content $logPath -Encoding UTF8 -ErrorAction SilentlyContinue
}

$r["build_tag_in_log"]    = (($logContent | Where-Object { $_ -match "BuildTag.*md5=$expectedTag" }).Count -gt 0)
$r["plugin_update_alive"] = (($logContent | Where-Object { $_ -match "Plugin\.Update absolute alive" }).Count -gt 0)
$r["plugin_ongui_alive"]  = (($logContent | Where-Object { $_ -match "OnGUI absolute proof reached" }).Count -gt 0)
$r["probe_update_alive"]  = (($logContent | Where-Object { $_ -match "RuntimeProbe\.Update alive" }).Count -gt 0)
$r["probe_ongui_first"]   = (($logContent | Where-Object { $_ -match "RuntimeProbe\.OnGUI first call" }).Count -gt 0)

if ($r["build_tag_in_log"])    { OK "BuildTag '$expectedTag' in log" }          else { ERR "BuildTag '$expectedTag' NOT in log" }
if ($r["plugin_update_alive"]) { OK "Plugin.Update absolute alive in log" }     else { ERR "Plugin.Update absolute alive NOT in log" }
if ($r["plugin_ongui_alive"])  { OK "OnGUI absolute proof reached in log" }     else { WARN "OnGUI absolute proof not in log (non-blocking)" }
if ($r["probe_update_alive"])  { OK "RuntimeProbe.Update alive in log" }        else { ERR "RuntimeProbe.Update alive NOT in log" }
if ($r["probe_ongui_first"])   { OK "RuntimeProbe.OnGUI first call in log" }    else { ERR "RuntimeProbe.OnGUI first call NOT in log" }

$r["json_exists"] = (Test-Path $jsonPath)
if ($r["json_exists"]) { OK "runtime-state.json exists" } else { ERR "runtime-state.json NOT created" }

$jsonData         = $null
$r["plugin_awake"]       = $false
$r["probe_created"]      = $false
$r["probe_upd_count"]    = 0
$r["probe_gui_count"]    = 0
$r["plugin_upd_count"]   = 0
$r["no_exception"]       = $false
$r["last_exception"]     = "json-not-created"

if ($r["json_exists"]) {
    try {
        $jsonRaw  = Get-Content $jsonPath -Raw -Encoding UTF8
        $jsonData = $jsonRaw | ConvertFrom-Json

        $r["plugin_awake"]     = ($jsonData.pluginAwakeCalled -eq $true)
        $r["probe_created"]    = ($jsonData.runtimeProbeCreated -eq $true)
        $r["probe_upd_count"]  = [int]$jsonData.runtimeProbeUpdateCount
        $r["probe_gui_count"]  = [int]$jsonData.runtimeProbeOnGuiCount
        $r["plugin_upd_count"] = [int]$jsonData.pluginUpdateCount
        $lastEx                = $jsonData.lastException
        $r["last_exception"]   = if ($null -eq $lastEx) { $null } else { $lastEx.ToString() }
        $r["no_exception"]     = ($null -eq $lastEx -or $lastEx.ToString() -eq "")

        if ($r["plugin_awake"])        { OK "pluginAwakeCalled=true" }      else { ERR "pluginAwakeCalled=false" }
        if ($r["probe_created"])       { OK "runtimeProbeCreated=true" }    else { ERR "runtimeProbeCreated=false" }
        if ($r["probe_upd_count"] -gt 0) { OK "runtimeProbeUpdateCount=$($r['probe_upd_count'])" } else { ERR "runtimeProbeUpdateCount=0" }
        if ($r["probe_gui_count"]  -gt 0){ OK "runtimeProbeOnGuiCount=$($r['probe_gui_count'])" }  else { ERR "runtimeProbeOnGuiCount=0" }
        if ($r["no_exception"])        { OK "lastException=null" }          else { ERR "lastException=$($r['last_exception'])" }

        if ($VerboseReport) {
            Write-Host ""
            Write-Host "  JSON:"
            Write-Host "    buildTag            = $($jsonData.buildTag)"
            Write-Host "    buildMd5Short       = $($jsonData.buildMd5Short)"
            Write-Host "    assemblyLocation    = $($jsonData.assemblyLocation)"
            Write-Host "    timestampUtc        = $($jsonData.timestampUtc)"
            Write-Host "    scene               = $($jsonData.scene)"
            Write-Host "    frameCount          = $($jsonData.frameCount)"
            Write-Host "    pluginUpdateCount   = $($jsonData.pluginUpdateCount)"
            Write-Host "    pluginOnGuiCount    = $($jsonData.pluginOnGuiCount)"
            Write-Host "    controllerExists    = $($jsonData.controllerExists)"
            Write-Host "    hudExists           = $($jsonData.hudExists)"
            Write-Host "    forceHudProofOfLife = $($jsonData.forceHudProofOfLife)"
            Write-Host "    proofHudVisible     = $($jsonData.proofHudVisible)"
        }
    } catch {
        ERR "Failed to parse runtime-state.json: $_"
    }
}

# ─── G) VERDICT ──────────────────────────────────────────────────────────────
Head "G) Verdict"

$passAll = $r["hash_match"] -and
           $r["mod_enabled"] -and
           $r["build_tag_in_log"] -and
           $r["probe_update_alive"] -and
           $r["probe_ongui_first"] -and
           $r["json_exists"] -and
           $r["plugin_awake"] -and
           $r["probe_created"] -and
           ($r["probe_upd_count"] -gt 0) -and
           ($r["probe_gui_count"] -gt 0) -and
           $r["no_exception"]

$failCause = ""
if (-not $passAll) {
    if (-not $r["hash_match"])         { $failCause = "DLL hash mismatch - reinstall from build/" }
    elseif (-not $r["mod_enabled"])    { $failCause = "SurveyorMap disabled - enable via r2modman" }
    elseif (-not $r["build_tag_in_log"]) { $failCause = "BuildTag '$expectedTag' not in log - BepInEx did not load SurveyorMap or wrong DLL" }
    elseif (-not $r["plugin_awake"])   { $failCause = "pluginAwakeCalled=false - Awake() never ran; check BepInEx error log" }
    elseif (-not $r["json_exists"])    { $failCause = "runtime-state.json never created - RuntimeProbe not created or Update never called" }
    elseif (-not $r["probe_created"])  { $failCause = "runtimeProbeCreated=false - EnsureCreated() failed; check log for 'probe-create' error" }
    elseif ($r["probe_upd_count"] -eq 0)  { $failCause = "runtimeProbeUpdateCount=0 - RuntimeProbe.Update() never called; probe GO may have been destroyed" }
    elseif ($r["probe_gui_count"]  -eq 0) { $failCause = "runtimeProbeOnGuiCount=0 - RuntimeProbe.OnGUI() never called; check ForceHudProofOfLife=true" }
    elseif (-not $r["no_exception"])   { $failCause = "Exception detected: $($r['last_exception'])" }
    elseif (-not $r["probe_update_alive"]) { $failCause = "RuntimeProbe.Update alive not in log - increase WaitSeconds or check probe lifecycle" }
    elseif (-not $r["probe_ongui_first"]) { $failCause = "RuntimeProbe.OnGUI first call not in log - check ForceHudProofOfLife=true in config" }
    else { $failCause = "Unknown failure - check log manually" }
}

$verdict = if ($passAll) { "PASS" } else { "FAIL" }

$recentLog = @()
if ($logContent.Count -gt 0) {
    $recentLog = ($logContent | Where-Object { $_ -match "\[SurveyorMap\]" }) | Select-Object -Last 20
}

Write-Host ""
if ($passAll) {
    Write-Host "  *** RESULT: PASS ***"
    Write-Host "  All runtime criteria satisfied."
    Write-Host "  Next: Set ForceHudProofOfLife=false and verify minimap visual."
} else {
    Write-Host "  *** RESULT: FAIL ***"
    Write-Host "  Root cause: $failCause"
}

# ─── H) REPORTS ──────────────────────────────────────────────────────────────
Head "H) Reports"

$reportObj = @{
    timestamp        = (Get-Date -Format "yyyy-MM-ddTHH:mm:ssZ")
    verdict          = $verdict
    failCause        = if ($passAll) { $null } else { $failCause }
    buildHash        = $buildShort
    installedHash    = (Get-MD5Short $installedDll)
    hashMatch        = $hashMatch
    modEnabled       = $modEnabled
    launchMethod     = if ($assistedMode) { "assisted" } else { $launchMethod }
    launchAuto       = $launchAuto
    buildTagExpected = $expectedTag
    buildTagInLog    = $r["build_tag_in_log"]
    logPath          = $logPath
    jsonPath         = $jsonPath
    waitSeconds      = $WaitSeconds
    probeUpdateCount = $r["probe_upd_count"]
    probeOnGuiCount  = $r["probe_gui_count"]
    pluginUpdateCount = $r["plugin_upd_count"]
    lastException    = $r["last_exception"]
    recentLogLines   = $recentLog
}

$reportObj | ConvertTo-Json -Depth 5 | Set-Content $reportJson -Encoding UTF8
OK "JSON report: $reportJson"

$mdOut = @()
$mdOut += "# SurveyorMap Runtime Validation"
$mdOut += ""
$mdOut += "**Date:** $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')"
$mdOut += "**Verdict:** **$verdict**"
$mdOut += ""
if (-not $passAll) {
    $mdOut += "## Root Cause"
    $mdOut += ""
    $mdOut += $failCause
    $mdOut += ""
}
$mdOut += "## Criteria"
$mdOut += ""
$mdOut += "| Check | Result |"
$mdOut += "|---|---|"
$mdOut += "| Hash match | $(if ($r['hash_match']) { 'PASS' } else { 'FAIL' }) |"
$mdOut += "| Mod enabled | $(if ($r['mod_enabled']) { 'PASS' } else { 'FAIL' }) |"
$mdOut += "| BuildTag in log | $(if ($r['build_tag_in_log']) { 'PASS' } else { 'FAIL' }) |"
$mdOut += "| Plugin.Update alive | $(if ($r['plugin_update_alive']) { 'PASS' } else { 'FAIL' }) |"
$mdOut += "| RuntimeProbe.Update alive | $(if ($r['probe_update_alive']) { 'PASS' } else { 'FAIL' }) |"
$mdOut += "| RuntimeProbe.OnGUI first | $(if ($r['probe_ongui_first']) { 'PASS' } else { 'FAIL' }) |"
$mdOut += "| runtime-state.json | $(if ($r['json_exists']) { 'PASS' } else { 'FAIL' }) |"
$mdOut += "| pluginAwakeCalled | $(if ($r['plugin_awake']) { 'PASS' } else { 'FAIL' }) |"
$mdOut += "| runtimeProbeCreated | $(if ($r['probe_created']) { 'PASS' } else { 'FAIL' }) |"
$mdOut += "| probeUpdateCount > 0 | $(if ($r['probe_upd_count'] -gt 0) { 'PASS' } else { 'FAIL' }) |"
$mdOut += "| probeOnGuiCount > 0 | $(if ($r['probe_gui_count'] -gt 0) { 'PASS' } else { 'FAIL' }) |"
$mdOut += "| lastException null | $(if ($r['no_exception']) { 'PASS' } else { 'FAIL' }) |"
$mdOut += ""
if ($recentLog.Count -gt 0) {
    $mdOut += "## Recent SurveyorMap Log Lines"
    $mdOut += ""
    $mdOut += '```'
    $recentLog | ForEach-Object { $mdOut += $_ }
    $mdOut += '```'
    $mdOut += ""
}
$mdOut += "## Build hash: $buildShort"
$mdOut += ""
$mdOut += "## Next Step"
$mdOut += ""
if ($passAll) {
    $mdOut += "PASS. Set ForceHudProofOfLife=false and test minimap visual baseline."
} else {
    $mdOut += "Fix: $failCause"
}

$mdOut | Set-Content $reportMd -Encoding UTF8
OK "Markdown report: $reportMd"

if ($CloseGame -and $gameProcess -and -not $gameProcess.HasExited) {
    WARN "Closing game (PID=$($gameProcess.Id))"
    $gameProcess.Kill()
}

Head "FINAL VERDICT: $verdict"
Write-Host ""
if (-not $passAll) { exit 1 } else { exit 0 }
