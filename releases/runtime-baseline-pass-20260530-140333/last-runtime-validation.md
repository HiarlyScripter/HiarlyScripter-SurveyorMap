# SurveyorMap Runtime Validation

**Date:** 2026-05-30 14:03:19
**Verdict:** **PASS**

## Criteria

| Check | Result |
|---|---|
| Hash match | PASS |
| Mod enabled | PASS |
| BuildTag in log | PASS |
| Plugin.Update alive | PASS |
| RuntimeProbe.Update alive | PASS |
| RuntimeProbe.OnGUI first | PASS |
| runtime-state.json | PASS |
| pluginAwakeCalled | PASS |
| runtimeProbeCreated | PASS |
| probeUpdateCount > 0 | PASS |
| probeOnGuiCount > 0 | PASS |
| lastException null | PASS |

## Recent SurveyorMap Log Lines

```
[Info   :SurveyorMap] [SurveyorMap] Run state: RunState=Menu, inferredLevel=False, RunIsLobby=False, RunIsShop=False, MenuLevel=True, RunIsLevel=False, spectating=False, PlayerAvatarLocal=False, levelObjects=True, levelGenDone=True, levelGenerated=False, modulesSpawned=0, playerApiReady=False, allowed=False, reason=menu-level
[Info   :SurveyorMap] [SurveyorMap] Player API ready=False, RunState=Menu, PlayerAvatarLocal=False, levelObjects=True
[Info   :SurveyorMap] [SurveyorMap] VisibleGate: playerApiReady=False, playerLocal=False, toggle=True, enableMinimap=True
[Info   :SurveyorMap] [SurveyorMap] Run state: RunState=Menu, inferredLevel=False, RunIsLobby=False, RunIsShop=False, MenuLevel=True, RunIsLevel=False, spectating=False, PlayerAvatarLocal=False, levelObjects=True, levelGenDone=False, levelGenerated=False, modulesSpawned=0, playerApiReady=False, allowed=False, reason=menu-level
[Info   :SurveyorMap] [SurveyorMap] Run state: RunState=Menu, inferredLevel=False, RunIsLobby=False, RunIsShop=False, MenuLevel=True, RunIsLevel=False, spectating=False, PlayerAvatarLocal=True, levelObjects=True, levelGenDone=False, levelGenerated=False, modulesSpawned=0, playerApiReady=True, allowed=False, reason=menu-level
[Info   :SurveyorMap] [SurveyorMap] Player API ready=True, RunState=Menu, PlayerAvatarLocal=True, levelObjects=True
[Info   :SurveyorMap] [SurveyorMap] VisibleGate: playerApiReady=True, playerLocal=True, toggle=True, enableMinimap=True
[Info   :SurveyorMap] [SurveyorMap] Run state: RunState=Menu, inferredLevel=False, RunIsLobby=False, RunIsShop=False, MenuLevel=True, RunIsLevel=False, spectating=False, PlayerAvatarLocal=True, levelObjects=True, levelGenDone=True, levelGenerated=True, modulesSpawned=0, playerApiReady=True, allowed=False, reason=menu-level
[Info   :SurveyorMap] [SurveyorMap] Plugin.Update absolute alive. frame=2117, t=15,9, scene=Main, activeSelf=True, activeInHierarchy=True, enabled=True, forceProof=True, toggleVisible=True, updateCount=2117
[Info   :SurveyorMap] [SurveyorMap] RuntimeProbe.Update alive. count=2117, frame=2117, t=15,9, scene=Main, activeSelf=True, activeInHierarchy=True
[Info   :SurveyorMap] [SurveyorMap] Plugin.Update absolute alive. frame=2942, t=20,9, scene=Main, activeSelf=True, activeInHierarchy=True, enabled=True, forceProof=True, toggleVisible=True, updateCount=2942
[Info   :SurveyorMap] [SurveyorMap] RuntimeProbe.Update alive. count=2942, frame=2942, t=20,9, scene=Main, activeSelf=True, activeInHierarchy=True
[Info   :SurveyorMap] [SurveyorMap] Plugin.Update absolute alive. frame=3767, t=25,9, scene=Main, activeSelf=True, activeInHierarchy=True, enabled=True, forceProof=True, toggleVisible=True, updateCount=3767
[Info   :SurveyorMap] [SurveyorMap] RuntimeProbe.Update alive. count=3767, frame=3767, t=25,9, scene=Main, activeSelf=True, activeInHierarchy=True
[Info   :SurveyorMap] [SurveyorMap] Plugin.Update absolute alive. frame=4592, t=30,9, scene=Main, activeSelf=True, activeInHierarchy=True, enabled=True, forceProof=True, toggleVisible=True, updateCount=4592
[Info   :SurveyorMap] [SurveyorMap] RuntimeProbe.Update alive. count=4592, frame=4592, t=30,9, scene=Main, activeSelf=True, activeInHierarchy=True
[Info   :SurveyorMap] [SurveyorMap] Plugin.Update absolute alive. frame=5416, t=35,9, scene=Main, activeSelf=True, activeInHierarchy=True, enabled=True, forceProof=True, toggleVisible=True, updateCount=5416
[Info   :SurveyorMap] [SurveyorMap] RuntimeProbe.Update alive. count=5416, frame=5416, t=35,9, scene=Main, activeSelf=True, activeInHierarchy=True
[Info   :SurveyorMap] [SurveyorMap] Plugin.Update absolute alive. frame=6241, t=40,9, scene=Main, activeSelf=True, activeInHierarchy=True, enabled=True, forceProof=True, toggleVisible=True, updateCount=6241
[Info   :SurveyorMap] [SurveyorMap] RuntimeProbe.Update alive. count=6241, frame=6241, t=40,9, scene=Main, activeSelf=True, activeInHierarchy=True
```

## Build hash: 1f9b9583

## Next Step

PASS. Set ForceHudProofOfLife=false and test minimap visual baseline.
