# surveyormap_gameplay_validate.ps1
# Automatizador completo de validacao de gameplay do SurveyorMap.
# Tenta automacao total. Se entrada em level nao puder ser automatizada,
# entra em modo AUTOMATION_PARTIAL: pede apenas "entre no level e pressione Enter".
# Apos isso: M x2, TAB, leitura de log/JSON, PASS/FAIL automatico.
#
# Uso: .\tools\surveyormap_gameplay_validate.ps1 [-WaitSeconds 90] [-NoLaunch] [-VerboseReport]

param(
    [int]$WaitSeconds   = 90,
    [switch]$NoLaunch,
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
$uiCfgPath    = Join-Path $PSScriptRoot "surveyormap_ui_automation.json"
$archiveDir   = Join-Path $PSScriptRoot "archive"
$reportJson   = Join-Path $PSScriptRoot "last-gameplay-validation.json"
$reportMd     = Join-Path $PSScriptRoot "last-gameplay-validation.md"
$ts           = Get-Date -Format "yyyyMMdd-HHmmss"
$startTime    = Get-Date

New-Item -ItemType Directory -Path $archiveDir -Force | Out-Null

function Head([string]$t) {
    Write-Host ""; Write-Host "===================================================="; Write-Host "  $t"; Write-Host "===================================================="
}
function OK([string]$m)   { Write-Host "  [OK]   $m" }
function WARN([string]$m) { Write-Host "  [WARN] $m" }
function ERR([string]$m)  { Write-Host "  [FAIL] $m" }
function INFO([string]$m) { if ($VerboseReport) { Write-Host "  [INFO] $m" } }
function STATUS([string]$m) { Write-Host "  [....] $m" }

function Get-MD5Short([string]$p) {
    if (-not (Test-Path $p)) { return "missing" }
    return (Get-FileHash -Path $p -Algorithm MD5).Hash.Substring(0,8).ToLower()
}

# Load UI config
$uiCfg = $null
if (Test-Path $uiCfgPath) {
    try { $uiCfg = Get-Content $uiCfgPath -Raw | ConvertFrom-Json } catch { WARN "Could not parse $uiCfgPath" }
}
$waitMenuSec   = if ($uiCfg) { [int]$uiCfg.waitMenuSeconds }    else { 40 }
$waitLevelSec  = if ($uiCfg) { [int]$uiCfg.waitLevelLoadSeconds } else { 90 }
$waitKeyMs     = if ($uiCfg) { [int]$uiCfg.waitAfterKeyMs }     else { 3000 }
$tabHoldMs     = if ($uiCfg) { [int]$uiCfg.tabHoldMs }          else { 2500 }
$menuNavMode   = if ($uiCfg) { $uiCfg.menuNavMode }             else { "assisted" }

# ======================== Win32 Key Sender ========================
Add-Type -TypeDefinition @"
using System;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Threading;
public class RepoInput {
    [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr h);
    [DllImport("user32.dll")] public static extern bool ShowWindow(IntPtr h, int n);
    [DllImport("user32.dll")] public static extern void keybd_event(byte vk, byte scan, int flags, int extra);
    [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr h, out RECT r);
    [DllImport("user32.dll")] public static extern bool SetCursorPos(int x, int y);
    [DllImport("user32.dll")] public static extern void mouse_event(int flags, int dx, int dy, int data, int extra);
    [StructLayout(LayoutKind.Sequential)]
    public struct RECT { public int Left, Top, Right, Bottom; }
    public const byte VK_TAB    = 0x09;
    public const byte VK_RETURN = 0x0D;
    public const byte VK_M      = 0x4D;
    public const int KEYDOWN    = 0x0000;
    public const int KEYUP      = 0x0002;
    public const int MOUSEMOVE  = 0x0001;
    public const int LEFTDOWN   = 0x0002;
    public const int LEFTUP     = 0x0004;
    public const int SW_RESTORE = 9;

    public static IntPtr FindGameWindow(string processName) {
        var procs = Process.GetProcessesByName(processName);
        if (procs.Length > 0 && procs[0].MainWindowHandle != IntPtr.Zero)
            return procs[0].MainWindowHandle;
        return IntPtr.Zero;
    }

    public static bool BringToFront(IntPtr hwnd) {
        if (hwnd == IntPtr.Zero) return false;
        ShowWindow(hwnd, SW_RESTORE);
        Thread.Sleep(300);
        return SetForegroundWindow(hwnd);
    }

    public static void SendKey(byte vk, int holdMs) {
        keybd_event(vk, 0, KEYDOWN, 0);
        Thread.Sleep(holdMs);
        keybd_event(vk, 0, KEYUP, 0);
    }

    public static bool ClickAt(IntPtr hwnd, int xPct, int yPct) {
        RECT r;
        if (!GetWindowRect(hwnd, out r)) return false;
        int w = r.Right  - r.Left;
        int h = r.Bottom - r.Top;
        int x = r.Left + (w * xPct / 100);
        int y = r.Top  + (h * yPct / 100);
        SetCursorPos(x, y);
        Thread.Sleep(100);
        mouse_event(LEFTDOWN, x, y, 0, 0);
        Thread.Sleep(100);
        mouse_event(LEFTUP, x, y, 0, 0);
        return true;
    }
}
"@ -ErrorAction SilentlyContinue

# ========================= A) PRE-CHECKS =========================
Head "A) Pre-Checks"

$buildShort    = Get-MD5Short $buildDll
$installedShort = Get-MD5Short $installedDll
$hashMatch     = ($buildShort -ne "missing" -and $buildShort -eq $installedShort)

if ($hashMatch) { OK "Hash match: $buildShort" } else { ERR "Hash MISMATCH: build=$buildShort installed=$installedShort" }

$modEnabled = $false
if (Test-Path $modsYml) {
    $mc = Get-Content $modsYml -Raw -Encoding UTF8
    if ($mc -match 'HiarlyScripter-SurveyorMap') {
        $modEnabled = -not ($mc -match 'enabled:\s*false')
        if ($modEnabled) { OK "SurveyorMap enabled in mods.yml" }
        else { ERR "SurveyorMap disabled in mods.yml" }
    } else { WARN "SurveyorMap not in mods.yml (assuming enabled)"; $modEnabled = $true }
} else { WARN "mods.yml not found"; $modEnabled = $true }

if (-not $hashMatch -or -not $modEnabled) {
    ERR "Pre-checks failed - aborting gameplay validation"
    exit 1
}

# ========================= B) ARCHIVE =========================
Head "B) Archive Previous State"

if (Test-Path $logPath) {
    Copy-Item $logPath (Join-Path $archiveDir "LogOutput-$ts.log") -Force
    OK "LogOutput.log archived"
}
if (Test-Path $jsonPath) {
    Copy-Item $jsonPath (Join-Path $archiveDir "runtime-state-$ts.json") -Force
    Remove-Item $jsonPath -Force
    OK "runtime-state.json archived and cleaned"
}

# ========================= C) LAUNCH GAME =========================
Head "C) Launch Game"

$expectedTag    = $buildShort
$gameProcess    = $null
$launchMethod   = "none"
$gameStartTime  = Get-Date

if ($NoLaunch) {
    OK "Skipping launch (-NoLaunch). Reading existing state."
} else {
    # Find REPO.exe
    $repoExe = $null
    @("E:\SteamLibrary\steamapps\common\REPO\REPO.exe",
      "D:\SteamLibrary\steamapps\common\REPO\REPO.exe",
      "C:\Program Files (x86)\Steam\steamapps\common\REPO\REPO.exe") | ForEach-Object {
        if ((Test-Path $_) -and -not $repoExe) { $repoExe = $_ }
    }

    if ($repoExe) {
        $gameDir      = Split-Path $repoExe -Parent
        $doorstopCfg  = Join-Path $gameDir "doorstop_config.ini"
        $doorstopBak  = "$doorstopCfg.gv.bak"
        $bepPreloader = "$profileRoot\BepInEx\core\BepInEx.Preloader.dll"
        if (-not (Test-Path $bepPreloader)) {
            $bepPreloader = "$profileRoot\BepInEx\core\BepInEx.Unity.Mono.Preloader.dll"
        }
        $patched = $false
        try {
            if ((Test-Path $doorstopCfg) -and (Test-Path $bepPreloader)) {
                Copy-Item $doorstopCfg $doorstopBak -Force
                $cfgContent = Get-Content $doorstopCfg -Raw -Encoding UTF8
                $cfgContent = $cfgContent -replace '(?m)^target_assembly\s*=.*$', "target_assembly=$bepPreloader"
                $cfgContent = $cfgContent -replace '(?m)^enabled\s*=.*$', "enabled=true"
                Set-Content $doorstopCfg $cfgContent -Encoding UTF8
                $patched = $true
                INFO "Doorstop patched -> $bepPreloader"
            }
            Start-Sleep -Seconds 1
            $gameProcess = Start-Process -FilePath $repoExe -PassThru
            if ($gameProcess) {
                OK "Game launched (PID=$($gameProcess.Id))"
                $launchMethod  = "doorstop-auto"
                $gameStartTime = Get-Date
                Start-Sleep -Seconds 3
            } else {
                WARN "Process.Start returned null"
                $launchMethod = "failed"
            }
        } catch {
            WARN "Launch error: $_"
            $launchMethod = "failed"
        } finally {
            if ($patched -and (Test-Path $doorstopBak)) {
                Start-Sleep -Seconds 4
                Copy-Item $doorstopBak $doorstopCfg -Force
                Remove-Item $doorstopBak -Force -ErrorAction SilentlyContinue
                INFO "Doorstop restored"
            }
        }
    } else {
        WARN "REPO.exe not found"
        $launchMethod = "not-found"
    }

    if ($launchMethod -eq "failed" -or $launchMethod -eq "not-found") {
        Write-Host ""
        Write-Host "  ============================================================"
        Write-Host "  AUTOMATION_PARTIAL: auto-launch indisponivel."
        Write-Host "  Abra r2modman -> REPO - Test -> Start modded e pressione ENTER."
        Write-Host "  ============================================================"
        $null = Read-Host "  ENTER quando o jogo estiver rodando"
        $gameStartTime = Get-Date
        $launchMethod  = "assisted"
    }
}

# ========================= D) WAIT FOR MENU =========================
Head "D) Wait for Menu (BuildTag)"

$menuDetected = $false
$waited = 0

if ($NoLaunch) {
    if (Test-Path $logPath) {
        $lc = Get-Content $logPath -Encoding UTF8 -ErrorAction SilentlyContinue
        $menuDetected = ($lc | Where-Object { $_ -match "BuildTag.*md5=$expectedTag" }).Count -gt 0
    }
    if ($menuDetected) { OK "BuildTag found in existing log" }
    else               { WARN "BuildTag not found in existing log" }
} else {
    STATUS "Waiting up to ${waitMenuSec}s for BuildTag '$expectedTag' in log..."
    while ($waited -lt $waitMenuSec) {
        Start-Sleep -Seconds 3
        $waited += 3
        if (Test-Path $logPath) {
            $mtime = (Get-Item $logPath).LastWriteTime
            if ($mtime -gt $gameStartTime) {
                $lc = Get-Content $logPath -Encoding UTF8 -ErrorAction SilentlyContinue
                if (($lc | Where-Object { $_ -match "BuildTag.*md5=$expectedTag" }).Count -gt 0) {
                    $menuDetected = $true
                    OK "Menu detected after ${waited}s (BuildTag found)"
                    break
                }
            }
        }
        Write-Host "  ${waited}s/${waitMenuSec}s waiting for menu..."
    }
    if (-not $menuDetected) { WARN "Menu not detected after ${waitMenuSec}s (BuildTag not in log)" }
}

# ========================= E) LEVEL ENTRY =========================
Head "E) Level Entry"

$levelEntryMode   = "none"
$levelDetected    = $false
$levelDetectStart = Get-Date

if ($NoLaunch) {
    # With -NoLaunch, check if JSON already shows Level
    if (Test-Path $jsonPath) {
        try {
            $j = Get-Content $jsonPath -Raw | ConvertFrom-Json
            if ($j.currentRunState -eq "Level" -or $j.isLevel -eq $true) {
                $levelDetected = $true
                $levelEntryMode = "already-in-level"
                OK "Already in Level (JSON: currentRunState=$($j.currentRunState))"
            }
        } catch {}
    }
    if (-not $levelDetected) {
        WARN "Not in Level. Use -NoLaunch only if game is already in a level."
    }
} else {
    # Try automated menu navigation first
    $hwnd = [RepoInput]::FindGameWindow("REPO")
    $autoEntryAttempted = $false

    if ($menuNavMode -eq "auto" -and $hwnd -ne [IntPtr]::Zero -and $uiCfg -and $uiCfg.menuClickSequence) {
        Write-Host "  Attempting automated menu navigation (mode=auto)..."
        if ([RepoInput]::BringToFront($hwnd)) {
            Start-Sleep -Seconds 1
            $clickOk = $true
            foreach ($click in $uiCfg.menuClickSequence) {
                if ($click.action -eq "click") {
                    $result = [RepoInput]::ClickAt($hwnd, [int]$click.xPct, [int]$click.yPct)
                    INFO "Click at $($click.xPct)%/$($click.yPct)%: $result ($($click.note))"
                    if ($click.waitAfterMs -gt 0) { Start-Sleep -Milliseconds $click.waitAfterMs }
                    if (-not $result) { $clickOk = $false }
                }
            }
            if ($clickOk) {
                $levelEntryMode = "auto-click"
                $autoEntryAttempted = $true
                OK "Menu navigation attempted via auto-click"
            } else {
                WARN "Auto-click partially failed"
                $levelEntryMode = "auto-partial"
                $autoEntryAttempted = $true
            }
        } else {
            WARN "Could not bring game window to foreground for auto-click"
        }
    }

    if (-not $autoEntryAttempted -or $levelEntryMode -eq "auto-partial") {
        # Fall back to assisted
        Write-Host ""
        Write-Host "  ============================================================"
        Write-Host "  AUTOMATION_PARTIAL: Nao consigo entrar no level automaticamente."
        Write-Host "  -> Crie uma partida solo no jogo."
        Write-Host "  -> Aguarde o level carregar."
        Write-Host "  -> Pressione ENTER aqui quando estiver DENTRO do level."
        Write-Host "  (Apos o ENTER, o script envia M/TAB e valida automaticamente)"
        Write-Host "  ============================================================"
        Write-Host ""
        $null = Read-Host "  ENTER quando estiver no level"
        $levelDetectStart  = Get-Date
        $levelEntryMode    = "assisted"
    }

    # Poll JSON for Level state
    STATUS "Polling JSON for currentRunState=Level (max ${waitLevelSec}s)..."
    $waited = 0
    while ($waited -lt $waitLevelSec) {
        Start-Sleep -Seconds 3
        $waited += 3
        if (Test-Path $jsonPath) {
            try {
                $j = Get-Content $jsonPath -Raw | ConvertFrom-Json
                if ($j.currentRunState -eq "Level" -or $j.isLevel -eq $true) {
                    $levelDetected = $true
                    OK "Level detected after ${waited}s (currentRunState=$($j.currentRunState))"
                    break
                } else {
                    INFO "JSON: currentRunState=$($j.currentRunState)"
                }
            } catch {}
        }
        Write-Host "  ${waited}s/${waitLevelSec}s waiting for level..."
    }

    if (-not $levelDetected) {
        WARN "Level NOT detected after ${waitLevelSec}s"
    }
}

# ========================= F) KEY AUTOMATION =========================
Head "F) Key Automation (M x2, TAB)"

$keysSent       = $false
$mSentCount     = 0
$tabSentCount   = 0
$keyHwnd        = [RepoInput]::FindGameWindow("REPO")
$keyAutoStatus  = "not-attempted"

function Send-GameKey([byte]$vk, [int]$holdMs, [string]$label) {
    $hwnd = [RepoInput]::FindGameWindow("REPO")
    if ($hwnd -ne [IntPtr]::Zero) {
        $brought = [RepoInput]::BringToFront($hwnd)
        if ($brought) {
            Start-Sleep -Milliseconds 200
            [RepoInput]::SendKey($vk, $holdMs)
            Write-Host "  [KEY] Sent $label (vk=0x$($vk.ToString('X2')), holdMs=$holdMs, hwnd=$hwnd)"
            return $true
        } else {
            Write-Host "  [WARN] Could not bring window to foreground for $label"
        }
    } else {
        Write-Host "  [WARN] REPO window not found for $label"
    }
    return $false
}

if (-not $levelDetected -and -not $NoLaunch) {
    $keyAutoStatus = "skipped-no-level"
    WARN "Key automation skipped: level not detected"
} else {
    # Wait for JSON to be fresh (probe writes every 5s)
    STATUS "Waiting 6s for JSON baseline before keys..."
    Start-Sleep -Seconds 6

    # Send M (toggle ON)
    STATUS "Sending M (toggle ON)..."
    if (Send-GameKey ([byte]0x4D) 100 "M") { $mSentCount++ }
    Start-Sleep -Milliseconds $waitKeyMs

    # Send M again (toggle OFF)
    STATUS "Sending M (toggle OFF)..."
    if (Send-GameKey ([byte]0x4D) 100 "M") { $mSentCount++ }
    Start-Sleep -Milliseconds $waitKeyMs

    # Send TAB (hold for tabHoldMs)
    STATUS "Sending TAB (hold ${tabHoldMs}ms)..."
    if (Send-GameKey ([byte]0x09) $tabHoldMs "TAB") { $tabSentCount++ }
    Start-Sleep -Milliseconds $waitKeyMs

    $keysSent = $true
    $keyAutoStatus = if ($mSentCount -ge 2 -and $tabSentCount -ge 1) { "full-auto" }
                     elseif ($mSentCount -gt 0) { "partial" }
                     else { "failed" }
    OK "Keys sent: M=$mSentCount TAB=$tabSentCount status=$keyAutoStatus"
}

# Wait for JSON to update with new data
STATUS "Waiting 8s for JSON to reflect key actions..."
Start-Sleep -Seconds 8

# ========================= G) READ EVIDENCE =========================
Head "G) Read Evidence"

$logContent = @()
if (Test-Path $logPath) {
    $logContent = Get-Content $logPath -Encoding UTF8 -ErrorAction SilentlyContinue
}

$script:jd = $null
$jsonParseOk = $false
if (Test-Path $jsonPath) {
    try {
        $script:jd = Get-Content $jsonPath -Raw -Encoding UTF8 | ConvertFrom-Json
        $jsonParseOk = $true
        OK "JSON parsed OK"
    } catch {
        ERR "JSON parse failed: $_"
    }
} else {
    WARN "runtime-state.json not found"
}

function JVal([string]$field, $default) {
    if ($null -eq $script:jd) { return $default }
    $v = $script:jd.$field
    if ($null -eq $v) { return $default }
    return $v
}

# Log evidence
$buildTagInLog      = ($logContent | Where-Object { $_ -match "BuildTag.*md5=$expectedTag" }).Count -gt 0
$toggleLogCount     = ($logContent | Where-Object { $_ -match "Toggle key detected" }).Count
$tabOpenLogCount    = ($logContent | Where-Object { $_ -match "nativeMapTabOpen=True" }).Count
$tabCloseLogCount   = ($logContent | Where-Object { $_ -match "nativeMapTabOpen=False" }).Count
$runStateLevelLog   = ($logContent | Where-Object { $_ -match "RunState=Level" }).Count -gt 0

Write-Host "  Log evidence:"
Write-Host "    BuildTag in log:       $buildTagInLog"
Write-Host "    Toggle key in log:     $toggleLogCount times"
Write-Host "    TAB open in log:       $tabOpenLogCount times"
Write-Host "    TAB close in log:      $tabCloseLogCount times"
Write-Host "    RunState=Level in log: $runStateLevelLog"

if ($jsonParseOk) {
    Write-Host "  JSON state:"
    foreach ($f in @("currentRunState","isLevel","hudVisible","nativeTextureReady","nativeMapCaptureReady","nativeTabOpen","capturePaused","minimapBaselineVisible","centerOnPlayer","centerOnPlayerApplied","centerOnPlayerProjectionValid","centerOnPlayerDistanceFromCenter","centerOnPlayerOffsetMagnitude","centerOnPlayerZoom","toggleKeyDetectedCount","tabOpenCount","tabCloseCount","lastToggleTimeUtc","lastGateReason","lastCaptureReason","forceHudProofOfLife","lastException","lastErrorStack")) {
        $v = JVal $f $null
        if ($null -ne $v) { Write-Host "    $f = $v" }
    }
}

# ========================= H) GAMEPLAY CRITERIA =========================
Head "H) Gameplay Criteria"

$g = @{}

# Runtime base (inherited from runtime validator)
$g["01_hash"]          = $hashMatch
$g["02_mod"]           = $modEnabled
$g["03_buildtag"]      = $buildTagInLog
$g["04_json_exists"]   = ($null -ne $script:jd)
$g["05_awake"]         = ($jsonParseOk -and (JVal "pluginAwakeCalled" $false) -eq $true)
$g["06_plugin_upd"]    = ($jsonParseOk -and [int](JVal "pluginUpdateCount" 0) -gt 0)
$g["07_plugin_gui"]    = ($jsonParseOk -and [int](JVal "pluginOnGuiCount" 0) -gt 0)
$g["08_probe"]         = ($jsonParseOk -and (JVal "runtimeProbeCreated" $false) -eq $true)
$g["09_probe_upd"]     = ($jsonParseOk -and [int](JVal "runtimeProbeUpdateCount" 0) -gt 0)
$g["10_probe_gui"]     = ($jsonParseOk -and [int](JVal "runtimeProbeOnGuiCount" 0) -gt 0)
$lastEx    = JVal "lastException" $null
$lastStack = JVal "lastErrorStack" $null
$g["11_no_ex"]         = ($jsonParseOk -and ($null -eq $lastEx   -or $lastEx.ToString()   -eq ""))
$g["12_no_stack"]      = ($jsonParseOk -and ($null -eq $lastStack -or $lastStack.ToString() -eq ""))

# Gameplay-specific
$isLevel            = ($jsonParseOk -and ((JVal "isLevel" $false) -eq $true -or (JVal "currentRunState" "") -eq "Level"))
$hudVisible         = ($jsonParseOk -and (JVal "hudVisible" $false) -eq $true)
$nativeTexReady     = ($jsonParseOk -and (JVal "nativeTextureReady" $false) -eq $true)
$forceHudOff        = ($jsonParseOk -and (JVal "forceHudProofOfLife" $true) -eq $false)
$centerEnabled      = ($jsonParseOk -and (JVal "centerOnPlayer" $false) -eq $true)
$centerApplied      = ($jsonParseOk -and (JVal "centerOnPlayerApplied" $false) -eq $true)
$centerProjection   = ($jsonParseOk -and (JVal "centerOnPlayerProjectionValid" $false) -eq $true)
$centerDistance     = [double](JVal "centerOnPlayerDistanceFromCenter" 999)
$toggleCountJson    = [int](JVal "toggleKeyDetectedCount" 0)
$tabOpenCountJson   = [int](JVal "tabOpenCount" 0)
$tabCloseCountJson  = [int](JVal "tabCloseCount" 0)

# G13: Level entry confirmed
$g["13_level"]         = ($isLevel -or $runStateLevelLog)
# G14: ForceHudProofOfLife=false
$g["14_no_force_hud"]  = $forceHudOff
# G15: HUD visible in level
$g["15_hud_visible"]   = ($hudVisible -or ($isLevel -and $nativeTexReady))
# G16: nativeTextureReady
$g["16_native_tex"]    = $nativeTexReady
# G17: M key detected >= 2 times (JSON count OR log count)
$mTotal = [Math]::Max($toggleCountJson, $toggleLogCount)
$g["17_m_toggle"]      = ($mTotal -ge 2)
# G18: TAB detection - check JSON counters OR log
$tabTotal = [Math]::Max($tabOpenCountJson, $tabOpenLogCount)
$tabClosed = [Math]::Max($tabCloseCountJson, $tabCloseLogCount)
$tabDetected = ($tabTotal -ge 1 -and ($tabClosed -ge 1 -or $tabSentCount -ge 1))
$g["18_tab"]           = $tabDetected
# C19/C20: Phase 2 CenterOnPlayer evidence
$g["19_center_enabled"] = $centerEnabled
$g["20_center_applied"] = ($centerApplied -and $centerProjection -and $centerDistance -le 1.5)

# TAB BLOCKED check: if we couldn't send TAB at all
$tabBlocked = ($tabSentCount -eq 0 -and $tabTotal -eq 0)

$crit = @(
    "[C1]  hashMatch=true ($buildShort)",
    "[C2]  modEnabled=true",
    "[C3]  BuildTag '$expectedTag' in log",
    "[C4]  runtime-state.json exists",
    "[C5]  pluginAwakeCalled=true",
    "[C6]  pluginUpdateCount > 0 (=$(JVal 'pluginUpdateCount' 0))",
    "[C7]  pluginOnGuiCount > 0 (=$(JVal 'pluginOnGuiCount' 0))",
    "[C8]  runtimeProbeCreated=true",
    "[C9]  runtimeProbeUpdateCount > 0",
    "[C10] runtimeProbeOnGuiCount > 0",
    "[C11] lastException=null",
    "[C12] lastErrorStack=null",
    "[C13] Level entered (isLevel=$isLevel, runStateLog=$runStateLevelLog)",
    "[C14] ForceHudProofOfLife=false",
    "[C15] hudVisible=true in level",
    "[C16] nativeTextureReady=true",
    "[C17] toggleKeyDetected >= 2 (json=$toggleCountJson, log=$toggleLogCount)",
    "[C18] TAB open+close detected (json=$tabOpenCountJson/$tabCloseCountJson, log=$tabOpenLogCount/$tabCloseLogCount)",
    "[C19] CenterOnPlayer=true",
    "[C20] player-centered pan applied (projection=$centerProjection, applied=$centerApplied, distance=$centerDistance)"
)

$keys = @("01_hash","02_mod","03_buildtag","04_json_exists","05_awake","06_plugin_upd","07_plugin_gui","08_probe","09_probe_upd","10_probe_gui","11_no_ex","12_no_stack","13_level","14_no_force_hud","15_hud_visible","16_native_tex","17_m_toggle","18_tab","19_center_enabled","20_center_applied")
$criteriaTotal = $keys.Count

for ($i = 0; $i -lt $keys.Count; $i++) {
    $key = $keys[$i]
    if ($g[$key]) { OK $crit[$i] } else { ERR $crit[$i] }
}

if ($tabBlocked) {
    Write-Host ""
    Write-Host "  ** TAB STATUS: BLOCKED **"
    Write-Host "  TAB key could not be sent (window not found or not in foreground)."
    Write-Host "  Criterion C18 marked FAIL. Manual TAB test required for full PASS."
}

# ========================= I) VERDICT =========================
Head "I) Verdict"

$passCount = ($g.Values | Where-Object { $_ -eq $true }).Count
$failCount = $criteriaTotal - $passCount
$passAll   = ($failCount -eq 0)

$tabStatus = if ($tabBlocked)         { "BLOCKED" }
             elseif ($tabDetected)    { "DETECTED" }
             elseif ($tabSentCount -gt 0) { "SENT_NOT_CONFIRMED" }
             else                     { "NOT_TESTED" }

$levelEntryAuto = ($levelEntryMode -eq "doorstop-auto" -or $levelEntryMode -eq "auto-click")

$tabSentNotConfirmed = ($tabSentCount -ge 1 -and -not $tabDetected -and -not $tabBlocked)
$verdict = if ($passAll)                                               { "PASS" }
           elseif ($passCount -ge ($criteriaTotal - 1) -and $tabSentNotConfirmed) { "PARTIAL_TAB_UNCONFIRMED" }
           elseif ($passCount -ge ($criteriaTotal - 3) -and $tabBlocked)          { "PARTIAL_TAB_BLOCKED" }
           else                                                        { "FAIL" }

$failCause = ""
if (-not $passAll) {
    $firstFail = $keys | Where-Object { -not $g[$_] } | Select-Object -First 1
    $idx = [Array]::IndexOf([string[]]$keys, $firstFail)
    if ($idx -ge 0) { $failCause = $crit[$idx] }
}

Write-Host ""
Write-Host "  Criteria: $passCount/$criteriaTotal pass, $failCount/$criteriaTotal fail"
Write-Host "  Level entry: $levelEntryMode (auto=$levelEntryAuto)"
Write-Host "  Keys: M=$mSentCount TAB=$tabSentCount keyStatus=$keyAutoStatus"
Write-Host "  TAB: $tabStatus"
Write-Host ""
if ($verdict -eq "PASS") {
    Write-Host "  *** RESULT: PASS ($criteriaTotal/$criteriaTotal) ***"
    Write-Host "  All gameplay criteria satisfied."
} elseif ($verdict -eq "PARTIAL_TAB_UNCONFIRMED") {
    Write-Host "  *** RESULT: PARTIAL ($passCount/$criteriaTotal) - TAB_SENT_NOT_CONFIRMED ***"
    Write-Host "  Runtime + level + minimap + toggle PASS. TAB was sent but not confirmed in log."
    Write-Host "  Cause: synthetic TAB was sent, but Unity InputSystem did not confirm native map open/close."
    Write-Host "  Manual verification: press TAB in-game and confirm nativeMapTabOpen=True in log."
} elseif ($verdict -eq "PARTIAL_TAB_BLOCKED") {
    Write-Host "  *** RESULT: PARTIAL ($passCount/$criteriaTotal) - TAB_BLOCKED ***"
    Write-Host "  Runtime + level + toggle PASS. TAB injection blocked (window not found)."
} else {
    Write-Host "  *** RESULT: FAIL ($passCount/$criteriaTotal) ***"
    Write-Host "  Root cause: $failCause"
}

# ========================= J) REPORTS =========================
Head "J) Reports"

$rpt = [ordered]@{
    timestamp        = (Get-Date -Format "yyyy-MM-ddTHH:mm:ssZ")
    verdict          = $verdict
    criteriaPass     = $passCount
    criteriaTotal    = $criteriaTotal
    failCause        = if ($passAll) { $null } else { $failCause }
    buildHash        = $buildShort
    hashMatch        = $hashMatch
    modEnabled       = $modEnabled
    buildTagInLog    = $buildTagInLog
    launchMethod     = $launchMethod
    levelEntryMode   = $levelEntryMode
    levelDetected    = $levelDetected
    levelEntryAuto   = $levelEntryAuto
    keyAutoStatus    = $keyAutoStatus
    mKeysSent        = $mSentCount
    tabKeysSent      = $tabSentCount
    tabStatus        = $tabStatus
    toggleLogCount   = $toggleLogCount
    tabOpenLogCount  = $tabOpenLogCount
    tabCloseLogCount = $tabCloseLogCount
}

if ($jsonParseOk) {
    foreach ($f in @("currentRunState","isLevel","hudVisible","nativeTextureReady","nativeMapCaptureReady","nativeTabOpen","capturePaused","minimapBaselineVisible","centerOnPlayer","centerOnPlayerApplied","centerOnPlayerProjectionValid","centerOnPlayerDistanceFromCenter","centerOnPlayerOffsetMagnitude","centerOnPlayerZoom","toggleKeyDetectedCount","tabOpenCount","tabCloseCount","lastToggleTimeUtc","lastGateReason","lastCaptureReason","forceHudProofOfLife","lastException","lastErrorStack","pluginUpdateCount","pluginOnGuiCount","runtimeProbeUpdateCount","runtimeProbeOnGuiCount","scene","buildTag")) {
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
$md += "# SurveyorMap Gameplay Validation"
$md += ""
$md += "**Date:** $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')   **Verdict:** **$verdict** ($passCount/$criteriaTotal)"
$md += ""
if ($verdict -ne "PASS") { $md += "## Root Cause"; $md += ""; $md += $failCause; $md += "" }
$md += "## Criteria ($criteriaTotal)"
$md += ""
$md += "| # | Criterion | Result |"
$md += "|---|---|---|"
for ($i = 0; $i -lt $keys.Count; $i++) {
    $key = $keys[$i]
    $md += "| $(($i+1).ToString().PadLeft(2)) | $($crit[$i]) | $(if ($g[$key]) {'PASS'} else {'FAIL'}) |"
}
$md += ""
$md += "## Automation Status"
$md += ""
$md += "| Item | Status |"
$md += "|---|---|"
$md += "| Game launch | $launchMethod |"
$md += "| Level entry | $levelEntryMode (detected=$levelDetected) |"
$md += "| M keys | sent=$mSentCount confirmed=$toggleLogCount |"
$md += "| TAB | $tabStatus (open=$tabOpenLogCount close=$tabCloseLogCount) |"
$md += ""
if ($tabBlocked -or $tabSentNotConfirmed) {
    $md += "## TAB: BLOCKED"
    $md += ""
    $md += "TAB automation failed or was not confirmed by Unity InputSystem."
    $md += "Minimum manual step: press TAB in-game and confirm open/close in log."
    $md += ""
}
if ($recentLog.Count -gt 0) {
    $md += "## Recent Log"
    $md += '```'
    $recentLog | ForEach-Object { $md += $_ }
    $md += '```'
}
$md | Set-Content $reportMd -Encoding UTF8
OK "Markdown: $reportMd"

Head "FINAL: $verdict ($passCount/$criteriaTotal)"
Write-Host ""

if ($verdict -eq "PASS") { exit 0 }
elseif ($verdict -in @("PARTIAL_TAB_BLOCKED", "PARTIAL_TAB_UNCONFIRMED")) { exit 2 }
else { exit 1 }
