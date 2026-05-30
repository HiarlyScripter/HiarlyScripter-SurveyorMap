# SurveyorMap Gameplay Validation

**Date:** 2026-05-30 17:33:52   **Verdict:** **PARTIAL_TAB_UNCONFIRMED** (19/20)

## Root Cause

[C18] TAB open+close detected (json=0/0, log=0/0)

## Criteria (20)

| # | Criterion | Result |
|---|---|---|
|  1 | [C1]  hashMatch=true (c0711156) | PASS |
|  2 | [C2]  modEnabled=true | PASS |
|  3 | [C3]  BuildTag 'c0711156' in log | PASS |
|  4 | [C4]  runtime-state.json exists | PASS |
|  5 | [C5]  pluginAwakeCalled=true | PASS |
|  6 | [C6]  pluginUpdateCount > 0 (=20225) | PASS |
|  7 | [C7]  pluginOnGuiCount > 0 (=40484) | PASS |
|  8 | [C8]  runtimeProbeCreated=true | PASS |
|  9 | [C9]  runtimeProbeUpdateCount > 0 | PASS |
| 10 | [C10] runtimeProbeOnGuiCount > 0 | PASS |
| 11 | [C11] lastException=null | PASS |
| 12 | [C12] lastErrorStack=null | PASS |
| 13 | [C13] Level entered (isLevel=True, runStateLog=True) | PASS |
| 14 | [C14] ForceHudProofOfLife=false | PASS |
| 15 | [C15] hudVisible=true in level | PASS |
| 16 | [C16] nativeTextureReady=true | PASS |
| 17 | [C17] toggleKeyDetected >= 2 (json=2, log=4) | PASS |
| 18 | [C18] TAB open+close detected (json=0/0, log=0/0) | FAIL |
| 19 | [C19] CenterOnPlayer=true | PASS |
| 20 | [C20] player-centered pan applied (projection=True, applied=True, distance=0) | PASS |

## Automation Status

| Item | Status |
|---|---|
| Game launch | none |
| Level entry | none (detected=False) |
| M keys | sent=2 confirmed=4 |
| TAB | SENT_NOT_CONFIRMED (open=0 close=0) |

## TAB: BLOCKED

TAB automation failed or was not confirmed by Unity InputSystem.
Minimum manual step: press TAB in-game and confirm open/close in log.

## Recent Log
```
[Info   :SurveyorMap] [SurveyorMap] HUD toggle applied. visibleBefore=True, visibleByToggle=False, hudActive=False
[Info   :SurveyorMap] [SurveyorMap] VisibleGate: playerApiReady=True, playerLocal=True, toggle=False, enableMinimap=True
[Info   :SurveyorMap] [SurveyorMap] Toggle key detected in RuntimeLoop: M
[Info   :SurveyorMap] [SurveyorMap] HUD toggle applied. visibleBefore=False, visibleByToggle=True, hudActive=False
[Info   :SurveyorMap] [SurveyorMap] VisibleGate: playerApiReady=True, playerLocal=True, toggle=True, enableMinimap=True
[Info   :SurveyorMap] [SurveyorMap] EnemyScan: mode=Off, radius=25,0, totalEnemiesFound=0, directorParents=0, sceneEnemies=0, activeEnemies=0, aliveEnemies=0, validEnemies=0, renderedEnemyMarkers=0, noRendererButAccepted=0, rejected{null=0, inactive=0, invalidScene=0, despawn=0, parentNotSpawned=0, parentMismatch=0, dead=0, noRenderer=0, noTransform=0, outOfRange=0, duplicate=0, projection=0}, clamped=0, unclamped=0, offViewportBeforeClamp=0, markerAtEdge=0, baseMapReady=False, baseMapTextureVisible=False, roomOverlayRooms=0
[Info   :SurveyorMap] [SurveyorMap] HUD activation start. canvasActive=False, panelActive=False, rawImage=enabled=True, textureAssigned=True, textureSize=200x200
[Info   :SurveyorMap] [SurveyorMap] HUD activation success. hudActive=True, canvasActive=True, panelActive=True, rawImage=enabled=True, textureAssigned=True, textureSize=200x200
[Info   :SurveyorMap] [SurveyorMap] HUD visible=True, reason=Level, toggle=True, playerReady=True, enableMinimap=True, allowed=True, nativeTextureReady=True, canvasActive=True
[Info   :SurveyorMap] [SurveyorMap] Plugin.Update absolute alive. frame=18582, t=121,5, scene=Main, activeSelf=True, activeInHierarchy=True, enabled=True, forceProof=False, toggleVisible=True, updateCount=18582
[Info   :SurveyorMap] [SurveyorMap] RuntimeProbe.Update alive. count=18582, frame=18582, t=121,5, scene=Main, activeSelf=True, activeInHierarchy=True
[Info   :SurveyorMap] [SurveyorMap] Plugin.Update absolute alive. frame=19403, t=126,5, scene=Main, activeSelf=True, activeInHierarchy=True, enabled=True, forceProof=False, toggleVisible=True, updateCount=19403
[Info   :SurveyorMap] [SurveyorMap] RuntimeProbe.Update alive. count=19403, frame=19403, t=126,5, scene=Main, activeSelf=True, activeInHierarchy=True
[Info   :SurveyorMap] [SurveyorMap] Plugin.Update absolute alive. frame=20225, t=131,5, scene=Main, activeSelf=True, activeInHierarchy=True, enabled=True, forceProof=False, toggleVisible=True, updateCount=20225
[Info   :SurveyorMap] [SurveyorMap] RuntimeProbe.Update alive. count=20225, frame=20225, t=131,5, scene=Main, activeSelf=True, activeInHierarchy=True
```
