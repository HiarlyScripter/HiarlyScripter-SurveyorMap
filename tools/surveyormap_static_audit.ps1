# surveyormap_static_audit.ps1
# Auditoria estatica SurveyorMap v2 - arquitetura NativeMapMirror limpa.
# Uso: .\tools\surveyormap_static_audit.ps1

$proj    = Split-Path $PSScriptRoot -Parent
$srcDir  = Join-Path $proj "src"
$install = "C:\Users\Hiarly\AppData\Roaming\r2modmanPlus-local\REPO\profiles\REPO - Test\BepInEx\plugins\HiarlyScripter-SurveyorMap\SurveyorMap.dll"
$build   = Join-Path $proj "build\SurveyorMap.dll"

$passCount = 0
$failCount = 0
$total     = 0
$lines     = [System.Collections.Generic.List[string]]::new()

function Check {
    param([string]$label, [bool]$result, [string]$detail = "")
    $script:total++
    if ($result) {
        $script:passCount++
        $script:lines.Add("  [PASS] $label")
    } else {
        $script:failCount++
        $detail2 = if ($detail) { " -- $detail" } else { "" }
        $script:lines.Add("  [FAIL] $label$detail2")
    }
}

function SrcContains {
    param([string]$pattern)
    $files = Get-ChildItem $srcDir -Filter "*.cs" -File -ErrorAction SilentlyContinue
    foreach ($f in $files) {
        $c = Get-Content $f.FullName -Raw -Encoding UTF8 -ErrorAction SilentlyContinue
        if ($c -and $c -cmatch $pattern) { return $true }
    }
    return $false
}

function SrcFile {
    param([string]$name)
    return Test-Path (Join-Path $srcDir $name)
}

Write-Host ""
Write-Host "======================================================"
Write-Host " SurveyorMap Static Audit v2"
Write-Host "======================================================"
Write-Host " SrcDir: $srcDir"
Write-Host ""

# === Plugin identity ===
Check "SurveyorMapPlugin inherits BaseUnityPlugin" (SrcContains 'class\s+SurveyorMapPlugin\s*:\s*BaseUnityPlugin') ""
Check "GUID com.hiarlyscripter.surveyormap"        (SrcContains 'com\.hiarlyscripter\.surveyormap') ""
Check "PluginVersion 1.0.0"                         (SrcContains '"1\.0\.0"') ""

# === Lifecycle ===
Check "Awake() exists"                              (SrcContains 'private\s+void\s+Awake\s*\(') ""
Check "Update() exists"                             (SrcContains 'private\s+void\s+Update\s*\(') ""
Check "OnGUI() exists"                              (SrcContains 'private\s+void\s+OnGUI\s*\(') ""
Check "DontDestroyOnLoad gameObject"                (SrcContains 'DontDestroyOnLoad\s*\(\s*gameObject\s*\)') ""
Check "transform.parent = null in Awake"            (SrcContains 'transform\.parent\s*=\s*null') ""
Check "HideFlags.HideAndDontSave"                   (SrcContains 'HideFlags\.HideAndDontSave') ""

# === BuildTag MD5 log ===
Check "MD5 BuildTag logged in Awake"                (SrcContains 'BuildTag\s+md5=') ""

# === New architecture classes ===
Check "NativeMapMirror.cs exists"                   (SrcFile "NativeMapMirror.cs") ""
Check "NativeMapMirror class defined"               (SrcContains 'class\s+NativeMapMirror') ""
Check "Dirt Finder Map Camera string"               (SrcContains '"Dirt Finder Map Camera"') ""
Check "camera.activeTexture used"                   (SrcContains '\.activeTexture') ""
Check "Map.Instance.ActiveSet call"                 (SrcContains 'Map\.Instance\.ActiveSet\s*\(') ""

$noRender = -not (SrcContains '_mapCamera\.Render\s*\(')
Check "No camera.Render() call"                     $noRender "camera.Render found - forbidden"

$noRT = -not (SrcContains 'new\s+RenderTexture\s*\(')
Check "No new RenderTexture"                        $noRT "new RenderTexture found - forbidden"

$noTarget = -not (SrcContains '\.targetTexture\s*=')
Check "No targetTexture assignment"                 $noTarget "targetTexture hijack found - forbidden"

# === Config ===
Check "SurveyorMapConfig.cs exists"                 (SrcFile "SurveyorMapConfig.cs") ""
Check "EnableMinimap config entry"                  (SrcContains '"EnableMinimap"') ""
Check "RevealRooms config entry"                    (SrcContains '"RevealRooms"') ""
Check "ShowEnemies config entry"                    (SrcContains '"ShowEnemies"') ""

# === RevealRooms ===
Check "RevealRoomsService.cs exists"                (SrcFile "RevealRoomsService.cs") ""

$patchLG = SrcContains 'HarmonyPatch\s*\(\s*typeof\s*\(\s*LevelGenerator\s*\)'
Check "HarmonyPatch LevelGenerator"                 $patchLG ""

Check "GenerateDone patch"                          (SrcContains '"GenerateDone"') ""

$setExp = SrcContains 'room\.SetExplored\s*\('
Check "RoomVolume.SetExplored call"                 $setExp ""

$noSetExpTrue = -not (SrcContains 'SetExplored\s*\(\s*true\s*\)')
Check "No SetExplored(true) wrong signature"        $noSetExpTrue "SetExplored(true) found - sig is no args"

# === ShowEnemies ===
Check "EnemyMapMarkerService.cs exists"             (SrcFile "EnemyMapMarkerService.cs") ""

$patchSp = SrcContains 'HarmonyPatch\s*\(\s*typeof\s*\(\s*EnemyParent\s*\)'
Check "HarmonyPatch EnemyParent"                    $patchSp ""

Check "SpawnRPC patch"                              (SrcContains '"SpawnRPC"') ""
Check "DespawnRPC patch"                            (SrcContains '"DespawnRPC"') ""

$addMC = SrcContains 'AddComponent\s*<\s*MapCustom\s*>'
Check "MapCustom component added"                   $addMC ""

Check "MapCustomEntity cleanup"                     (SrcContains 'MapCustomEntity') ""

$noMCDisable = -not (SrcContains '\.enabled\s*=\s*false.*MapCustom')
Check "No global MapCustom disable"                 $noMCDisable "Global MapCustom.enabled=false found - forbidden"

# === Diagnostics ===
Check "SurveyorMapDiagnostics.cs exists"            (SrcFile "SurveyorMapDiagnostics.cs") ""
Check "WriteDiagJson method"                        (SrcContains 'WriteDiagJson\s*\(') ""
Check "surveyormap-runtime-state.json path"         (SrcContains 'surveyormap-runtime-state\.json') ""

# === Safety: no forbidden architecture ===
$noProbe = -not (SrcContains 'RuntimeProbeBehaviour')
Check "No RuntimeProbeBehaviour"                    $noProbe "Old probe class found - must be removed"

$noForce = -not (SrcContains 'ForceHudProofOfLife')
Check "No ForceHudProofOfLife"                      $noForce "Old proof HUD found - must be removed"

$noCache = -not (SrcContains 'SurveyorRoomMapCache')
Check "No SurveyorRoomMapCache"                     $noCache "Old room cache found - must be removed"

$noHide = -not (SrcContains 'HideNativeVisuals')
Check "No HideNativeVisuals"                        $noHide "Old hide call found - must be removed"

$noLoop = -not (SrcContains 'RuntimeLoop')
Check "No RuntimeLoop"                              $noLoop "Old runtime loop found - must be removed"

# === Build artifacts ===
Check "Build DLL exists"                            (Test-Path $build) $build
Check "Installed DLL in REPO - Test"                (Test-Path $install) $install

if ((Test-Path $build) -and (Test-Path $install)) {
    $h1 = (Get-FileHash $build   -Algorithm MD5).Hash
    $h2 = (Get-FileHash $install -Algorithm MD5).Hash
    $match = ($h1 -eq $h2)
    $detail3 = "build=$($h1.Substring(0,8)) install=$($h2.Substring(0,8))"
    Check "Build hash == Install hash" $match $detail3
}

foreach ($l in $lines) { Write-Host $l }
Write-Host ""
Write-Host "------------------------------------------------------"
$verdict = if ($failCount -eq 0) { "PASS" } else { "FAIL" }
$msg = " Result: $verdict  ($passCount/$total passed, $failCount failed)"
Write-Host $msg
Write-Host "======================================================"
Write-Host ""

if ($failCount -gt 0) { exit 1 } else { exit 0 }
