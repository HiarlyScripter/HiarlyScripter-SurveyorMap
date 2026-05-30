# SurveyorMap Runtime Validation

**Date:** 2026-05-30 18:35:25   **Verdict:** **PASS** (12/12)

## 12 Criteria

| # | Criterion | Result |
|---|---|---|
| C1  | hashMatch: build == installed (c0711156) | PASS |
| C2  | modEnabled: SurveyorMap enabled | PASS |
| C3  | BuildTag 'c0711156' in LogOutput.log | PASS |
| C4  | runtime-state.json exists and fresh | PASS |
| C5  | pluginAwakeCalled=true | PASS |
| C6  | pluginUpdateCount > 0 | PASS |
| C7  | pluginOnGuiCount > 0 | PASS |
| C8  | runtimeProbeCreated=true | PASS |
| C9  | runtimeProbeUpdateCount > 0 | PASS |
| C10 | runtimeProbeOnGuiCount > 0 | PASS |
| C11 | lastException=null | PASS |
| C12 | lastErrorStack=null | PASS |

## Recent SurveyorMap Log
```
[Info   :SurveyorMap] [SurveyorMap] Player API ready=False, RunState=Menu, PlayerAvatarLocal=False, levelObjects=True
[Info   :SurveyorMap] [SurveyorMap] VisibleGate: playerApiReady=False, playerLocal=False, toggle=True, enableMinimap=True
[Info   :SurveyorMap] [SurveyorMap] Run state: RunState=Menu, inferredLevel=False, RunIsLobby=False, RunIsShop=False, MenuLevel=True, RunIsLevel=False, spectating=False, PlayerAvatarLocal=True, levelObjects=True, levelGenDone=False, levelGenerated=False, modulesSpawned=0, playerApiReady=True, allowed=False, reason=menu-level
[Info   :SurveyorMap] [SurveyorMap] Player API ready=True, RunState=Menu, PlayerAvatarLocal=True, levelObjects=True
[Info   :SurveyorMap] [SurveyorMap] VisibleGate: playerApiReady=True, playerLocal=True, toggle=True, enableMinimap=True
[Info   :SurveyorMap] [SurveyorMap] Run state: RunState=Menu, inferredLevel=False, RunIsLobby=False, RunIsShop=False, MenuLevel=True, RunIsLevel=False, spectating=False, PlayerAvatarLocal=True, levelObjects=True, levelGenDone=True, levelGenerated=True, modulesSpawned=0, playerApiReady=True, allowed=False, reason=menu-level
[Info   :SurveyorMap] [SurveyorMap] Plugin.Update absolute alive. frame=2067, t=16,0, scene=Main, activeSelf=True, activeInHierarchy=True, enabled=True, forceProof=False, toggleVisible=True, updateCount=2067
[Info   :SurveyorMap] [SurveyorMap] RuntimeProbe.Update alive. count=2067, frame=2067, t=16,0, scene=Main, activeSelf=True, activeInHierarchy=True
[Info   :SurveyorMap] [SurveyorMap] Plugin.Update absolute alive. frame=2883, t=21,0, scene=Main, activeSelf=True, activeInHierarchy=True, enabled=True, forceProof=False, toggleVisible=True, updateCount=2883
[Info   :SurveyorMap] [SurveyorMap] RuntimeProbe.Update alive. count=2883, frame=2883, t=21,0, scene=Main, activeSelf=True, activeInHierarchy=True
[Info   :SurveyorMap] [SurveyorMap] Plugin.Update absolute alive. frame=3708, t=26,1, scene=Main, activeSelf=True, activeInHierarchy=True, enabled=True, forceProof=False, toggleVisible=True, updateCount=3708
[Info   :SurveyorMap] [SurveyorMap] RuntimeProbe.Update alive. count=3708, frame=3708, t=26,1, scene=Main, activeSelf=True, activeInHierarchy=True
```

## Next Step

PASS 12/12. Advance to Fase 1 level validation if applicable.
