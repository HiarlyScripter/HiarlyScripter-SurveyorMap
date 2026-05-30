# VALIDATION_CONTRACT.md
# Objective acceptance criteria by phase. Updated 2026-05-30.

---

## Phase 0 - Runtime Baseline (COMPLETE / PASS)

Current build/hash: `c0711156`.

| # | Criterion | Source | Status |
|---|---|---|---|
| 1 | hashMatch: build == installed | Hash/log evidence | PASS |
| 2 | modEnabled: SurveyorMap enabled | Runtime evidence | PASS |
| 3 | BuildTag `c0711156` in LogOutput.log | Log | PASS |
| 4 | runtime-state.json exists and is fresh | File/JSON | PASS |
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

Phase 1 remains valid under build `c0711156`.

| Criterion | Source | Status |
|---|---|---|
| currentRunState=Level | JSON/log | PASS |
| ForceHudProofOfLife=false | JSON/config evidence | PASS |
| hudVisible=true in level | JSON/log | PASS |
| nativeTextureReady=true | JSON/log | PASS |
| nativeMapCaptureReady=true | JSON/log | PASS |
| minimapBaselineVisible=true | JSON/log | PASS |
| M toggle detected | JSON/log | PASS |
| lastException=null | JSON | PASS |
| lastErrorStack=null | JSON | PASS |

---

## Phase 2 - CenterOnPlayer=true (PARTIAL / BLOCKED)

Gameplay criteria: 19/20 PASS.

| # | Criterion | Source | Status |
|---|---|---|---|
| 1-17 | Runtime, level, minimap, and M toggle inherited criteria | JSON/log | PASS |
| 18 | TAB open and close detected | MapToolController log/JSON | BLOCKED |
| 19 | CenterOnPlayer=true | JSON/config evidence | PASS |
| 20 | Player-centered pan applied | JSON/log | PASS |

Current Phase 2 evidence:
- `centerOnPlayer=true`.
- `centerOnPlayerApplied=true`.
- `centerOnPlayerProjectionValid=true`.
- `centerOnPlayerDistanceFromCenter=0`.
- `centerOnPlayerZoom=3`.
- `currentRunState=Level`.
- `hudVisible=true`.
- `nativeTextureReady=true`.
- `minimapBaselineVisible=true`.
- `forceHudProofOfLife=false`.
- `lastException=null`.
- `lastErrorStack=null`.

Blocking detail:
- Synthetic TAB was sent by automation.
- `MapToolController.Active` did not produce native TAB open/close log entries.
- Local API inspection confirms the native map binding is Unity InputSystem `<Keyboard>/tab`.
- Phase 2 is not full PASS until native TAB open/close is objectively confirmed.

---

## Future Phases

| Phase | Objective | Gate |
|---|---|---|
| Phase 3 | RevealRooms=true | Only after Phase 2 full PASS |
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
