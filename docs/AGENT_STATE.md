# Agent State

Updated 2026-05-30 after Codex TAB automation diagnostic round.

## Current Status

| Item | Status |
|---|---|
| Branch | `codex-exec` |
| Current build/hash | `c0711156` |
| Static Audit | PASS 22/22 |
| Build Release | PASS 0 errors / 0 warnings (from prior Phase 2 validation, not rebuilt this round) |
| Runtime Probe | PASS 12/12 |
| Gameplay Validation | PASS 20/20 |
| Full validation | PASS |
| Phase 2 CenterOnPlayer | PASS |

## C18 TAB Result

C18 is now objectively validated by automation.

- Winning method: `SendInput`.
- Winning hold: `2s`.
- Window focus: `true`.
- Window title: `R.E.P.O.`.
- `keybd_event` attempts: 2s, 4s, 6s all sent but unconfirmed.
- `SendInput` attempt: 2s sent and confirmed.
- Fresh log delta: `nativeMapTabOpen=True/False` = `1/1`.
- Fresh capture delta: `capturePaused/resumingCapture` = `1/1`.
- Manual TAB fallback was not used.

## Phase 2 Evidence

- `CenterOnPlayer=true`.
- `centerOnPlayerApplied=true`.
- `centerOnPlayerProjectionValid=true`.
- `centerOnPlayerDistanceFromCenter=0`.
- `currentRunState=Level`.
- `hudVisible=true`.
- `nativeTextureReady=true`.
- `nativeMapCaptureReady=true`.
- `minimapBaselineVisible=true`.
- `ForceHudProofOfLife=false`.
- M toggle validates by log/JSON.
- TAB open/close validates by fresh log evidence through `SendInput`.
- `lastException=null`.
- `lastErrorStack=null`.

## Scope Notes

- `src\Core.cs` was not changed in this TAB diagnostic round.
- No build was run for the final diagnostic change.
- No DLL was installed in this TAB diagnostic round.
- r2modman UI was not touched.
- `mods.yml` was not edited.
- `RevealRooms=false` and `ShowEnemies=false` remain out of scope.

## Next Step

Claude can treat Phase 2 as PASS and resume planning the next phase. Do not start RevealRooms or ShowEnemies unless explicitly requested in a new round.
