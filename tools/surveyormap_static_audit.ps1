# surveyormap_static_audit.ps1
# Auditoria estática do Core.cs — retorna PASS/FAIL e exit code.
# Uso: .\tools\surveyormap_static_audit.ps1

$proj    = Split-Path $PSScriptRoot -Parent
$src     = Join-Path $proj "src\Core.cs"
$install = "C:\Users\Hiarly\AppData\Roaming\r2modmanPlus-local\REPO\profiles\REPO - Test\BepInEx\plugins\HiarlyScripter-SurveyorMap\SurveyorMap.dll"
$build   = Join-Path $proj "build\SurveyorMap.dll"

$passCount  = 0
$failCount  = 0
$total = 0
$lines = [System.Collections.Generic.List[string]]::new()

function Check([string]$label, [bool]$result, [string]$detail) {
    $script:total++
    if ($result) {
        $script:passCount++
        $script:lines.Add("  [PASS] $label")
    } else {
        $script:failCount++
        $script:lines.Add("  [FAIL] $label$(if ($detail) { ' -- ' + $detail })")
    }
}

Write-Host ""
Write-Host "======================================================"
Write-Host " SurveyorMap Static Audit"
Write-Host "======================================================"
Write-Host " Source: $src"
Write-Host ""

if (-not (Test-Path $src)) {
    Write-Host "[ERROR] Core.cs not found: $src"
    exit 1
}

$code = Get-Content $src -Raw -Encoding UTF8

Check "Class inherits BaseUnityPlugin"                 ($code -match 'class\s+SurveyorMapPlugin\s*:\s*BaseUnityPlugin') ""
Check "Awake() method exists"                          ($code -match 'private\s+void\s+Awake\s*\(') ""
Check "Update() method exists in plugin"               ($code -match 'private void Update\(\)') ""
Check "OnGUI() method exists in plugin"                ($code -match 'private void OnGUI\(\)') ""
Check "DontDestroyOnLoad(gameObject) in plugin"        ($code -match 'DontDestroyOnLoad\s*\(\s*gameObject\s*\)') ""
Check "transform.parent = null in Awake"               ($code -match 'gameObject\.transform\.parent\s*=\s*null') ""
Check "RuntimeProbeBehaviour class exists"             ($code -match 'class\s+RuntimeProbeBehaviour\s*:\s*MonoBehaviour') ""
Check "RuntimeProbeBehaviour.EnsureCreated() called"   ($code -match 'RuntimeProbeBehaviour\.EnsureCreated\s*\(\s*\)') ""
Check "DontDestroyOnLoad(go) in probe EnsureCreated"   ($code -match 'DontDestroyOnLoad\s*\(\s*go\s*\)') ""
Check "RuntimeProbe Update heartbeat log"              ($code -match 'RuntimeProbe\.Update alive') ""
Check "RuntimeProbe OnGUI first call log"              ($code -match 'RuntimeProbe\.OnGUI first call') ""
Check "JSON write (surveyormap-runtime-state.json)"    ($code -match 'surveyormap-runtime-state\.json') ""
Check "BuildTagShort set in Awake"                     ($code -match 'BuildTagShort\s*=\s*shortHash') ""
Check "AssemblyLocation property exposed"              ($code -match 'AssemblyLocation\s*\{') ""
Check "BuildTimestampUtc property exposed"             ($code -match 'BuildTimestampUtc\s*\{') ""
Check "JSON includes buildMd5Short"                    ($code -match '"buildMd5Short"') ""
Check "JSON includes assemblyLocation"                 ($code -match '"assemblyLocation"') ""
Check "JSON includes timestampUtc"                     ($code -match '"timestampUtc"') ""
Check "JSON includes toggleVisible"                    ($code -match '"toggleVisible"') ""
Check "JSON includes lastErrorStack"                   ($code -match '"lastErrorStack"') ""
Check "Build DLL exists"                               (Test-Path $build) $build
Check "Installed DLL exists in REPO - Test"            (Test-Path $install) $install

foreach ($l in $lines) { Write-Host $l }
Write-Host ""
Write-Host "------------------------------------------------------"
$verdict = if ($failCount -eq 0) { "PASS" } else { "FAIL" }
Write-Host " Result: $verdict  ($passCount/$total passed, $failCount failed)"
Write-Host "======================================================"
Write-Host ""

if ($failCount -gt 0) { exit 1 } else { exit 0 }
