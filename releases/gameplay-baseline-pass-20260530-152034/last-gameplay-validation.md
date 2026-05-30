# SurveyorMap Gameplay Validation

**Date:** 2026-05-30 15:20:07   **Verdict:** **PASS** (18/18)

## Criteria (18)

| # | Criterion | Result |
|---|---|---|
|  1 | [C1]  hashMatch=true (b46919ef) | PASS |
|  2 | [C2]  modEnabled=true | PASS |
|  3 | [C3]  BuildTag 'b46919ef' in log | PASS |
|  4 | [C4]  runtime-state.json exists | PASS |
|  5 | [C5]  pluginAwakeCalled=true | PASS |
|  6 | [C6]  pluginUpdateCount > 0 (=23731) | PASS |
|  7 | [C7]  pluginOnGuiCount > 0 (=47656) | PASS |
|  8 | [C8]  runtimeProbeCreated=true | PASS |
|  9 | [C9]  runtimeProbeUpdateCount > 0 | PASS |
| 10 | [C10] runtimeProbeOnGuiCount > 0 | PASS |
| 11 | [C11] lastException=null | PASS |
| 12 | [C12] lastErrorStack=null | PASS |
| 13 | [C13] Level entered (isLevel=True, runStateLog=True) | PASS |
| 14 | [C14] ForceHudProofOfLife=false | PASS |
| 15 | [C15] hudVisible=true in level | PASS |
| 16 | [C16] nativeTextureReady=true | PASS |
| 17 | [C17] toggleKeyDetected >= 2 (json=4, log=8) | PASS |
| 18 | [C18] TAB open+close detected (json=0/0, log=1/1) | PASS |

## Automation Status

| Item | Status |
|---|---|
| Game launch | none |
| Level entry | none (detected=False) |
| M keys | sent=2 confirmed=8 |
| TAB | DETECTED (open=1 close=1) |

## Recent Log
```
[Info   :SurveyorMap] [SurveyorMap] HUD visible=False, reason=toggle-off, toggle=False, playerReady=True, enableMinimap=True, allowed=True, nativeTextureReady=False, canvasActive=False
[Info   :SurveyorMap] [SurveyorMap] HUD toggle applied. visibleBefore=True, visibleByToggle=False, hudActive=False
[Info   :SurveyorMap] [SurveyorMap] VisibleGate: playerApiReady=True, playerLocal=True, toggle=False, enableMinimap=True
[Info   :SurveyorMap] [SurveyorMap] Toggle key detected in RuntimeLoop: M
[Info   :SurveyorMap] [SurveyorMap] HUD toggle applied. visibleBefore=False, visibleByToggle=True, hudActive=False
[Info   :SurveyorMap] [SurveyorMap] VisibleGate: playerApiReady=True, playerLocal=True, toggle=True, enableMinimap=True
[Info   :SurveyorMap] [SurveyorMap] HUD activation start. canvasActive=False, panelActive=False, rawImage=enabled=True, textureAssigned=True, textureSize=200x200
[Info   :SurveyorMap] [SurveyorMap] HUD activation success. hudActive=True, canvasActive=True, panelActive=True, rawImage=enabled=True, textureAssigned=True, textureSize=200x200
[Info   :SurveyorMap] [SurveyorMap] HUD visible=True, reason=Level, toggle=True, playerReady=True, enableMinimap=True, allowed=True, nativeTextureReady=True, canvasActive=True
[Info   :SurveyorMap] [SurveyorMap] Plugin.Update absolute alive. frame=22590, t=239,6, scene=Main, activeSelf=True, activeInHierarchy=True, enabled=True, forceProof=False, toggleVisible=True, updateCount=22590
[Info   :SurveyorMap] [SurveyorMap] RuntimeProbe.Update alive. count=22590, frame=22590, t=239,6, scene=Main, activeSelf=True, activeInHierarchy=True
[Info   :SurveyorMap] [SurveyorMap] Plugin.Update absolute alive. frame=23158, t=244,6, scene=Main, activeSelf=True, activeInHierarchy=True, enabled=True, forceProof=False, toggleVisible=True, updateCount=23158
[Info   :SurveyorMap] [SurveyorMap] RuntimeProbe.Update alive. count=23158, frame=23158, t=244,6, scene=Main, activeSelf=True, activeInHierarchy=True
[Info   :SurveyorMap] [SurveyorMap] Plugin.Update absolute alive. frame=23731, t=249,6, scene=Main, activeSelf=True, activeInHierarchy=True, enabled=True, forceProof=False, toggleVisible=True, updateCount=23731
[Info   :SurveyorMap] [SurveyorMap] RuntimeProbe.Update alive. count=23731, frame=23731, t=249,6, scene=Main, activeSelf=True, activeInHierarchy=True
```
