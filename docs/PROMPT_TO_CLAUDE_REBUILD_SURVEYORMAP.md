# Prompt To Claude - SurveyorMap Full Implementation

Copy this whole prompt into a new Claude chat.

---

You are the main executor for rebuilding the R.E.P.O. mod `SurveyorMap`.

Your first action is to read this handoff:

`C:\Users\Hiarly\.codex\PROJETOS\REPO\HiarlyScripter-SurveyorMap-Exec\docs\CLAUDE_FULL_IMPLEMENTATION_HANDOFF.md`

Then also read:

`C:\Users\Hiarly\.codex\PROJETOS\REPO\HiarlyScripter-SurveyorMap-Exec\docs\REFERENCE_REBUILD_BLUEPRINT.md`

The handoff is the operational source of truth. The blueprint is supporting context.

## Mission

Rebuild SurveyorMap cleanly and deliver a local working mod:

- native persistent minimap in the bottom-left;
- native TAB intact;
- `M` toggles the SurveyorMap minimap;
- RevealRooms through native `RoomVolume.SetExplored()`;
- ShowEnemies through native `MapCustom` markers;
- build/install/validate in `REPO - Test`;
- generate a local r2modman/Thunderstore-style ZIP under `releases\`;
- do not publish to Thunderstore;
- do not push to GitHub.

This is an execution mission, not a planning-only pass.

Do not stop after minimap, RevealRooms, or ShowEnemies if the step is passing. Continue until DLL, validation evidence, screenshots/crops, and local ZIP are delivered. Stop only for a critical blocker.

## Project

Use:

`C:\Users\Hiarly\.codex\PROJETOS\REPO\HiarlyScripter-SurveyorMap-Exec`

Do not use `.claude` as the project source.

Do not edit the r2modman `Default` profile.

Use only:

`C:\Users\Hiarly\AppData\Roaming\r2modmanPlus-local\REPO\profiles\REPO - Test`

for install/validation.

## Safety Start

Before editing logic, run and record:

```powershell
git status
git branch --show-current
git log --oneline --decorate -10
```

If there are uncommitted changes you did not make, do not overwrite them.

Create a local checkpoint tag or branch before the rebuild. Do not push.

Suggested checkpoint:

`checkpoint/claude-before-clean-rebuild-YYYYMMDD-HHMMSS`

Make additional local checkpoints after:

- minimap baseline;
- RevealRooms;
- ShowEnemies;
- final DLL/ZIP.

Do not push any checkpoint.

## Legal Rule

The reference mods `dig-Minimap` and `clay-BetterMap` were audited, but no clear LICENSE file was found in the local packages.

Use clean-room implementation:

- do not copy literal code;
- replicate behavior and architecture;
- use verified game APIs;
- write original SurveyorMap code.

If you independently locate a clear permissive license, record it and add attribution before adapting anything. Otherwise continue clean-room.

## Rebuild Direction

Treat current `src\Core.cs` as diagnostic history. It became too large and fragile.

You may rewrite `src\Core.cs` cleanly or split code into smaller `.cs` files under `src\`. The SDK-style csproj should compile additional `.cs` files automatically.

Keep:

- plugin name `SurveyorMap`;
- GUID `com.hiarlyscripter.surveyormap`;
- package identity where truthful;
- BepInEx/Harmony/netstandard2.1.

Do not preserve:

- RuntimeLoop watchdog;
- RuntimeProbeBehaviour;
- ForceHudProofOfLife;
- cyan proof HUD;
- fake room overlay;
- CenterOnPlayer as default;
- SurveyorRoomMapCache;
- overlay by bounds/colliders;
- RenderCameraOnce;
- owned RenderTexture baseline;
- HideNativeVisuals;
- global filters that disable MapCustom/MapCustomEntity;
- projected UI enemy overlay as the primary ShowEnemies path.

## Implementation Order

1. Rebuild source cleanly.
2. Implement native minimap mirror:
   - find `"Dirt Finder Map Camera"`;
   - use `camera.activeTexture`;
   - keep `Map.Instance.ActiveSet(true)` during valid gameplay;
   - draw bottom-left about 260x260 with `OnGUI`/`GUI.DrawTexture`;
   - no fake cyan map.
3. Implement `M` toggle.
4. Implement TAB-safe behavior:
   - detect `MapToolController.Active`;
   - hide persistent minimap while TAB is active;
   - restore/default camera zoom while TAB is active;
   - never black out TAB.
5. Implement RevealRooms:
   - Harmony postfix on `LevelGenerator.GenerateDone`;
   - call `RoomVolume.SetExplored()` with no arguments;
   - log counts;
   - no custom room overlay.
6. Implement ShowEnemies:
   - Harmony postfix on `EnemyParent.SpawnRPC`;
   - Harmony postfix on `EnemyParent.DespawnRPC`;
   - add/update native `MapCustom`;
   - cleanup `MapCustomEntity`;
   - filter dead/despawned/inactive enemies.
7. Update validators for the new architecture.
8. Build Release.
9. Install DLL to `REPO - Test`.
10. Validate runtime/gameplay, including TAB via SendInput/scancode.
11. Capture screenshots/crops:
    - native TAB;
    - persistent minimap;
    - RevealRooms;
    - ShowEnemies;
    - M toggle off/on.
12. Update package README/changelog/manifest truthfully.
13. Create local ZIP under `releases\`.

## Technical Non-Negotiables

- Do not use fake cyan overlay as the map.
- Do not use aggressive CenterOnPlayer zoom/pan by default.
- Do not create owned RenderTexture for baseline.
- Do not call `camera.Render()` for baseline.
- Do not assign or hijack native camera `targetTexture`.
- Do not hide or disable the native map camera.
- Do not deactivate `Map.ActiveParent`.
- Do not disable `MapCustom` or `MapCustomEntity` globally.
- Do not reconstruct rooms by bounds/colliders.
- Do not rely on stale logs.
- Do not claim visual PASS without screenshots/crops.
- Do not publish.
- Do not push.
- Do not edit `Default`.
- Do not edit other mods.

## Expected Commands

Build:

```powershell
$env:REPO_MANAGED_DIR = "E:\SteamLibrary\steamapps\common\REPO\REPO_Data\Managed"
$env:REPO_BEPINEX_CORE_DIR = "C:\Users\Hiarly\AppData\Roaming\r2modmanPlus-local\REPO\profiles\REPO - Test\BepInEx\core"
dotnet build .\src -c Release
```

Install:

```powershell
$installDir = "C:\Users\Hiarly\AppData\Roaming\r2modmanPlus-local\REPO\profiles\REPO - Test\BepInEx\plugins\HiarlyScripter-SurveyorMap"
New-Item -ItemType Directory -Force -Path $installDir | Out-Null
Copy-Item .\build\SurveyorMap.dll "$installDir\SurveyorMap.dll" -Force
```

Validate after updating validators:

```powershell
.\tools\surveyormap_static_audit.ps1
.\tools\surveyormap_runtime_validate.ps1 -LaunchGame -WaitSeconds 90 -VerboseReport
.\tools\surveyormap_gameplay_validate.ps1 -WaitSeconds 90 -VerboseReport
.\tools\surveyormap_full_validate.ps1 -WaitSeconds 90 -VerboseReport
```

Package local ZIP:

```powershell
Copy-Item .\build\SurveyorMap.dll .\package\plugins\HiarlyScripter-SurveyorMap\SurveyorMap.dll -Force
New-Item -ItemType Directory -Force -Path .\releases | Out-Null
$zip = ".\releases\HiarlyScripter-SurveyorMap-v1.0.0-local.zip"
if (Test-Path $zip) { Remove-Item $zip -Force }
Compress-Archive -Path .\package\* -DestinationPath $zip -Force
```

Do not run `tcli publish`.

## Final PASS Requirements

All are required:

- build 0 errors / 0 warnings;
- DLL installed in `REPO - Test`;
- build hash equals installed hash;
- plugin loads;
- minimap appears bottom-left;
- minimap looks like native TAB map;
- `M` toggle works;
- TAB automation uses SendInput/scancode;
- TAB opens/closes and does not go black;
- RevealRooms visible on TAB and persistent minimap;
- ShowEnemies visible on TAB and persistent minimap;
- dead/despawned/inactive enemies clean up;
- screenshots/crops exist;
- local ZIP exists under `releases\`.

## Final Report

Report:

1. `STATUS`: PASS / BLOCKED / INCONCLUSIVE
2. branch and checkpoints
3. files changed
4. build result
5. installed DLL path and hash comparison
6. validation results
7. screenshot/crop paths
8. ZIP path
9. remaining risks

No publish. No push. No Default profile.
