# VALIDATION_CONTRACT.md
# Objective acceptance criteria by phase. Updated 2026-05-30.

---

## Overall Full Validation - COMPLETE / PASS

Build/hash: `b46919ef`.

| Area | Required Result | Current Status |
|---|---|---|
| Static Audit | PASS 22/22 | PASS |
| Build Release | PASS 0 errors / 0 warnings | PASS |
| Runtime Probe | PASS 12/12 | PASS |
| Gameplay Baseline | PASS 18/18 | PASS |
| Overall | All areas PASS | PASS |

---

## Phase 0 - Runtime Baseline (COMPLETE / PASS)

Build/hash: `b46919ef`.

Criteria (12/12 PASS):

| # | Criterion | Source | Status |
|---|---|---|---|
| 1 | hashMatch: build == installed | Hash/log evidence | PASS |
| 2 | modEnabled: SurveyorMap enabled | Runtime evidence | PASS |
| 3 | BuildTag `b46919ef` in LogOutput.log | Log | PASS |
| 4 | runtime-state.json exists and is fresh for pass | File/JSON | PASS |
| 5 | pluginAwakeCalled=true | JSON | PASS |
| 6 | pluginUpdateCount > 0 | JSON | PASS |
| 7 | pluginOnGuiCount > 0 | JSON | PASS |
| 8 | runtimeProbeCreated=true | JSON | PASS |
| 9 | runtimeProbeUpdateCount > 0 | JSON | PASS |
| 10 | runtimeProbeOnGuiCount > 0 | JSON | PASS |
| 11 | lastException=null | JSON | PASS |
| 12 | lastErrorStack=null | JSON | PASS |

---

## Phase 1 - Minimap Baseline In Level (COMPLETE / PASS)

Build/hash: `b46919ef`.

Gameplay Baseline PASS 18/18:

| # | Criterion | Source | Status |
|---|---|---|---|
| 1-12 | All Phase 0 criteria remain PASS | Runtime/gameplay reports | PASS |
| 13 | currentRunState=Level | JSON/log | PASS |
| 14 | ForceHudProofOfLife=false | JSON/config evidence | PASS |
| 15 | hudVisible=true in level | JSON/log | PASS |
| 16 | nativeTextureReady=true | JSON/log | PASS |
| 17 | M toggle detected | JSON/log | PASS |
| 18 | TAB open and close detected | Log | PASS |

Additional Phase 1 evidence:
- `nativeMapCaptureReady=true`.
- `minimapBaselineVisible=true`.
- `lastCaptureReason=native-map-rendered`.
- `lastGateReason=gameplay-active`.
- TAB open/close log count is `1/1`.
- `lastException=null`.
- `lastErrorStack=null`.

---

## Phase 2 - CenterOnPlayer=true (NEXT)

Gate to start:
- Overall Full Validation is PASS.
- Phase 1 remains PASS.
- Acting agent has run `tools\ai_start_work.ps1`.

Expected PASS evidence:
- `CenterOnPlayer=true` is active.
- Player-centered minimap behavior is validated in level.
- No crash or runtime exception.
- `lastException=null`.
- `lastErrorStack=null`.
- Evidence is captured in log/JSON.

---

## Future Phases

| Phase | Objective | Gate |
|---|---|---|
| Phase 3 | RevealRooms=true | Only after CenterOnPlayer PASS |
| Phase 4 | ShowEnemies=true | Only after RevealRooms PASS |
| Phase 5 | Premium visual polish | Only after gameplay features PASS |
| Phase 6 | Package/release local | Only after all required feature phases PASS |

For now, `RevealRooms=false` and `ShowEnemies=false` remain out of scope.

---

## Permanent Rules

- Do not declare a feature working without objective log or JSON evidence.
- Do not advance phase without all required criteria satisfied.
- Do not use visual/manual observation as the only validation.
- Do not publish, push, or package unless explicitly requested in a future round.

