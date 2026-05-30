# AGENT_STATE.md
# Source of truth - updated 2026-05-30 after Gameplay Baseline PASS.

---

## Project Identity

| Field | Value |
|---|---|
| Name | SurveyorMap |
| Author | HiarlyScripter |
| GUID | `com.hiarlyscripter.surveyormap` |
| Version | `1.0.0` |

---

## Critical Paths

| Item | Path |
|---|---|
| Claude project root | `C:\Users\Hiarly\.claude\PROJETOS\REPO\HiarlyScripter-SurveyorMap` |
| Codex Exec worktree | `C:\Users\Hiarly\.codex\PROJETOS\REPO\HiarlyScripter-SurveyorMap-Exec` |
| Source | `src\Core.cs` |
| Runtime validation | `tools\last-runtime-validation.*` |
| Gameplay validation | `tools\last-gameplay-validation.*` |
| Full validation | `tools\last-full-validation.*` |

---

## Current Build

| Field | Value |
|---|---|
| Build/hash | `b46919ef` |
| Full validation | PASS |
| Static Audit | PASS 22/22 |
| Build Release | PASS 0 errors / 0 warnings |
| Runtime Probe | PASS 12/12 |
| Gameplay Baseline | PASS 18/18 |

---

## Confirmed Functional State

| Item | Status |
|---|---|
| BepInEx loads SurveyorMap | PASS, BuildTag found in log |
| Plugin.Awake() runs | PASS |
| Plugin.Update() runs | PASS |
| Plugin.OnGUI() runs | PASS |
| RuntimeProbe.Update() runs | PASS |
| RuntimeProbe.OnGUI() runs | PASS |
| Runtime JSON exists and is fresh for the pass | PASS |
| currentRunState | `Level` |
| hudVisible in level | `true` |
| nativeTextureReady in level | `true` |
| nativeMapCaptureReady in level | `true` |
| minimapBaselineVisible in level | `true` |
| lastCaptureReason | `native-map-rendered` |
| lastGateReason | `gameplay-active` |
| ForceHudProofOfLife | `false` |
| M toggle | Validated by log/JSON |
| TAB open/close | Validated by log, open/close `1/1` |
| lastException | `null` |
| lastErrorStack | `null` |

---

## Baseline Configuration

```ini
ForceHudProofOfLife = false
CenterOnPlayer = false
RevealRooms = false
RevealMode = Off
ShowEnemies = false
EnemyDetectionMode = Off
SafeMode = true
CaptureFPS = 5
RenderTextureSize = 256
```

---

## Completed Phase

Phase 1 baseline minimap is complete with PASS.

Evidence:
- Static Audit PASS 22/22.
- Build Release PASS 0 errors / 0 warnings.
- Runtime Probe PASS 12/12.
- Gameplay Baseline PASS 18/18.
- Minimap baseline validated in `Level`.
- M validated by log/JSON.
- TAB open/close validated by log.

---

## Next Phase

Next phase: Phase 2 - `CenterOnPlayer=true`.

Keep out of scope for now:
- `RevealRooms=true`
- `ShowEnemies=true`
- Premium visual polish
- Package/Thunderstore/GitHub publication work

---

## Permanent Rules

- Do not publish to Thunderstore.
- Do not push to GitHub.
- Do not touch other mods.
- Do not declare a feature working without objective log/JSON evidence.
- Do not edit `src\Core.cs` during documentation-only sync rounds.

