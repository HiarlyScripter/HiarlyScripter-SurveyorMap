# SurveyorMap Gameplay Validation

**Date:** 2026-05-30 18:40:30   **Verdict:** **PASS** (20/20)

## Criteria (20)

| # | Criterion | Result |
|---|---|---|
|  1 | [C1]  hashMatch=true (c0711156) | PASS |
|  2 | [C2]  modEnabled=true | PASS |
|  3 | [C3]  BuildTag 'c0711156' in log | PASS |
|  4 | [C4]  runtime-state.json exists | PASS |
|  5 | [C5]  pluginAwakeCalled=true | PASS |
|  6 | [C6]  pluginUpdateCount > 0 (=53224) | PASS |
|  7 | [C7]  pluginOnGuiCount > 0 (=106548) | PASS |
|  8 | [C8]  runtimeProbeCreated=true | PASS |
|  9 | [C9]  runtimeProbeUpdateCount > 0 | PASS |
| 10 | [C10] runtimeProbeOnGuiCount > 0 | PASS |
| 11 | [C11] lastException=null | PASS |
| 12 | [C12] lastErrorStack=null | PASS |
| 13 | [C13] Level entered (isLevel=True, runStateLog=True) | PASS |
| 14 | [C14] ForceHudProofOfLife=false | PASS |
| 15 | [C15] hudVisible=true in level | PASS |
| 16 | [C16] nativeTextureReady=true | PASS |
| 17 | [C17] toggleKeyDetected >= 2 (json=6, log=12) | PASS |
| 18 | [C18] TAB open+close detected by fresh evidence (status=PASS, method=SendInput, hold=2s, jsonDelta=0/0, logDelta=1/1) | PASS |
| 19 | [C19] CenterOnPlayer=true | PASS |
| 20 | [C20] player-centered pan applied (projection=True, applied=True, distance=0) | PASS |

## Automation Status

| Item | Status |
|---|---|
| Game launch | none |
| Level entry | none (detected=False) |
| M keys | sent=2 confirmed=12 |
| TAB | DETECTED (method=SendInput, hold=2s) |
| Window | focused=True hwnd=5113750 pid=20784 title=R.E.P.O. |

## C18 TAB Diagnostics

| Field | Value |
|---|---|
| C18 status | PASS |
| Method used | SendInput |
| Hold seconds | 2 |
| Window focused | True |
| tabOpenCount/tabCloseCount | 0/0 |
| tabOpenDelta/tabCloseDelta | 0/0 |
| tabOpenLogDelta/tabCloseLogDelta | 1/1 |
| nativeTabOpen | False |
| capturePaused | False |
| capturePaused/resumed log delta | 1/1 |
| Manual fallback used | False |
| Reason |  |

## TAB Attempt Matrix

| Method | Hold | Sent | Focused | JSON delta | Log delta | Capture delta | Confirmed | By |
|---|---:|---|---|---|---|---|---|---|
| keybd_event | 2s | True | True | 0/0 | 0/0 | 0/0 | False |  |
| keybd_event | 4s | True | True | 0/0 | 0/0 | 0/0 | False |  |
| keybd_event | 6s | True | True | 0/0 | 0/0 | 0/0 | False |  |
| SendInput | 2s | True | True | 0/0 | 1/1 | 1/1 | True | native-map-log |

## Recent Log
```
[Info   :SurveyorMap] [SurveyorMap] RuntimeProbe.Update alive. count=51577, frame=51577, t=321,4, scene=Main, activeSelf=True, activeInHierarchy=True
[Info   :SurveyorMap] [SurveyorMap] Plugin.Update absolute alive. frame=52400, t=326,4, scene=Main, activeSelf=True, activeInHierarchy=True, enabled=True, forceProof=False, toggleVisible=True, updateCount=52400
[Info   :SurveyorMap] [SurveyorMap] RuntimeProbe.Update alive. count=52400, frame=52400, t=326,4, scene=Main, activeSelf=True, activeInHierarchy=True
[Info   :SurveyorMap] [SurveyorMap] Plugin.Update absolute alive. frame=53224, t=331,4, scene=Main, activeSelf=True, activeInHierarchy=True, enabled=True, forceProof=False, toggleVisible=True, updateCount=53224
[Info   :SurveyorMap] [SurveyorMap] RuntimeProbe.Update alive. count=53224, frame=53224, t=331,4, scene=Main, activeSelf=True, activeInHierarchy=True
[Info   :SurveyorMap] [SurveyorMap] NativeMapState: nativeMapTabOpen=True, capturePaused=True
[Info   :SurveyorMap] [SurveyorMap] NativeMapState: originalTargetTextureRestored=True, cameraRestored=True, activeParentRestored=False
[Warning:SurveyorMap] [SurveyorMap] NativeMapCapture: ready=False, visiblePixels=False, mapPopulated=True, render=not-rendered, camera=none, texture=none, source=none, mapActive=True, activeParent=True, nativeVisualHidden=False, questionMarkersHidden=False, questionMarkers=0, modules=6, layers=3, reason=native-map-tab-active
[Info   :SurveyorMap] [SurveyorMap] EnemyScan: mode=Off, radius=25,0, totalEnemiesFound=0, directorParents=0, sceneEnemies=0, activeEnemies=0, aliveEnemies=0, validEnemies=0, renderedEnemyMarkers=0, noRendererButAccepted=0, rejected{null=0, inactive=0, invalidScene=0, despawn=0, parentNotSpawned=0, parentMismatch=0, dead=0, noRenderer=0, noTransform=0, outOfRange=0, duplicate=0, projection=0}, clamped=0, unclamped=0, offViewportBeforeClamp=0, markerAtEdge=0, baseMapReady=False, baseMapTextureVisible=False, roomOverlayRooms=0
[Info   :SurveyorMap] [SurveyorMap] HUD visible=False, reason=Level, toggle=True, playerReady=True, enableMinimap=True, allowed=True, nativeTextureReady=False, canvasActive=False
[Info   :SurveyorMap] [SurveyorMap] NativeMapState: nativeMapTabOpen=False, resumingCapture=True
[Info   :SurveyorMap] [SurveyorMap] NativeMapCapture: ready=True, visiblePixels=True, mapPopulated=True, render=render-ok, camera=Dirt Finder Map Camera, texture=200x200 format=ARGB32 depth=32 aa=1 hdr=False mipmaps=False filter=Point, source=native.activeTexture, mapActive=True, activeParent=True, nativeVisualHidden=True, questionMarkersHidden=False, questionMarkers=0, modules=6, layers=3, reason=native-map-rendered
[Info   :SurveyorMap] [SurveyorMap] HUD activation start. canvasActive=False, panelActive=False, rawImage=enabled=True, textureAssigned=True, textureSize=200x200
[Info   :SurveyorMap] [SurveyorMap] HUD activation success. hudActive=True, canvasActive=True, panelActive=True, rawImage=enabled=True, textureAssigned=True, textureSize=200x200
[Info   :SurveyorMap] [SurveyorMap] HUD visible=True, reason=Level, toggle=True, playerReady=True, enableMinimap=True, allowed=True, nativeTextureReady=True, canvasActive=True
```
