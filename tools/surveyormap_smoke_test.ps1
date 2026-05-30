# surveyormap_smoke_test.ps1
# Le LogOutput.log e runtime-state.json e emite PASS/FAIL.
# Use apos iniciar o jogo manualmente.

param(
    [string]$Profile = "REPO - Test",
    [string]$ExpectedMd5 = ""   # ex: "4cc93384" (opcional, curto)
)

$pluginDir = "C:\Users\Hiarly\AppData\Roaming\r2modmanPlus-local\REPO\profiles\$Profile\BepInEx\plugins\HiarlyScripter-SurveyorMap"
$logPath   = "C:\Users\Hiarly\AppData\Roaming\r2modmanPlus-local\REPO\profiles\$Profile\BepInEx\LogOutput.log"
$jsonPath  = "$pluginDir\diagnostics\surveyormap-runtime-state.json"
$buildDll  = "C:\Users\Hiarly\.claude\PROJETOS\REPO\HiarlyScripter-SurveyorMap\build\SurveyorMap.dll"
$instDll   = "$pluginDir\SurveyorMap.dll"

$pass = 0; $fail = 0

function Check($label, $ok, $detail = "") {
    if ($ok) {
        Write-Host "  PASS  $label" -ForegroundColor Green
        $script:pass++
    } else {
        $msg = "  FAIL  $label"
        if ($detail) { $msg += "  [$detail]" }
        Write-Host $msg -ForegroundColor Red
        $script:fail++
    }
}

Write-Host "`n=== SurveyorMap Smoke Test ===" -ForegroundColor Cyan
Write-Host "  Profile : $Profile"
Write-Host "  Log     : $logPath"
Write-Host "  JSON    : $jsonPath`n"

# ── DLL hash ──────────────────────────────────────────────────────────────────
$buildHash = if (Test-Path $buildDll) { (Get-FileHash -Algorithm MD5 $buildDll).Hash } else { "" }
$instHash  = if (Test-Path $instDll)  { (Get-FileHash -Algorithm MD5 $instDll).Hash  } else { "" }
Check "DLL build exists"     ($buildHash -ne "")   "not found: $buildDll"
Check "DLL installed exists" ($instHash  -ne "")   "not found: $instDll"
Check "DLL hashes match"     ($buildHash -ne "" -and $buildHash -eq $instHash) "build=$buildHash inst=$instHash"

# ── LogOutput.log ─────────────────────────────────────────────────────────────
if (-not (Test-Path $logPath)) {
    Check "LogOutput.log exists" $false "file not found"
} else {
    $log = Get-Content $logPath -Raw

    $buildShort = $buildHash.Substring(0,8).ToLower()
    Check "BuildTag in log (md5=$buildShort)"  ($log -match "md5=$buildShort")  "BuildTag not found"
    Check "SurveyorMap initialized"            ($log -match "SurveyorMap initialized")
    Check "RuntimeLoop scheduled"              ($log -match "RuntimeLoop scheduled")
    Check "RuntimeProbe created"               ($log -match "RuntimeProbe created")
    Check "Plugin.Update absolute alive"       ($log -match "Plugin\.Update absolute alive")
    Check "RuntimeProbe.Update alive"          ($log -match "RuntimeProbe\.Update alive")
    Check "RuntimeProbe.OnGUI first call"      ($log -match "RuntimeProbe\.OnGUI first call")
    $hasException = ($log -match "\[Error\s*:SurveyorMap\]") -and ($log -match "exception")
    Check "No SurveyorMap exceptions"          (-not $hasException) "errors found in log"
}

# ── runtime-state.json ────────────────────────────────────────────────────────
if (-not (Test-Path $jsonPath)) {
    Check "runtime-state.json created" $false "file not found: $jsonPath"
} else {
    $j = Get-Content $jsonPath -Raw
    Check "pluginAwakeCalled=true"       ($j -match '"pluginAwakeCalled":\s*true')
    Check "runtimeProbeCreated=true"     ($j -match '"runtimeProbeCreated":\s*true')
    Check "pluginUpdateCount > 0"        ($j -match '"pluginUpdateCount":\s*[1-9]')
    Check "runtimeProbeUpdateCount > 0"  ($j -match '"runtimeProbeUpdateCount":\s*[1-9]')
    Check "runtimeProbeOnGuiCount > 0"   ($j -match '"runtimeProbeOnGuiCount":\s*[1-9]')
    Check "lastException null"           ($j -match '"lastException":\s*null')
}

# ── Screenshot ────────────────────────────────────────────────────────────────
$ssPath = "$pluginDir\diagnostics\surveyormap-proof-menu.png"
Check "Screenshot exists" (Test-Path $ssPath) "not found: $ssPath"

# ── Result ────────────────────────────────────────────────────────────────────
Write-Host "`n=== Result ===" -ForegroundColor Cyan
Write-Host "  PASS: $pass"
Write-Host "  FAIL: $fail"
if ($fail -eq 0) {
    Write-Host "`n  SMOKE TEST: PASS" -ForegroundColor Green
} else {
    Write-Host "`n  SMOKE TEST: FAIL ($fail checks failed)" -ForegroundColor Red
    Write-Host "  See FAIL lines above for details." -ForegroundColor Yellow
}
