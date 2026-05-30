# Next Actions

Updated 2026-05-30 after C18 TAB automation diagnostic.

## Current Result

Phase 2 - `CenterOnPlayer=true` is PASS.

- Build/hash: `c0711156`.
- Runtime Probe: PASS 12/12.
- Gameplay Validation: PASS 20/20.
- Overall: PASS.
- `CenterOnPlayer=true`.
- `ForceHudProofOfLife=false`.
- HUD/minimap visible in `Level`.
- M toggle validated by log/JSON.
- TAB open/close validated by automation.

## C18 Automation Finding

- `keybd_event` does not satisfy C18 even with focus and TAB holds of 2s, 4s, and 6s.
- `SendInput` with scancode input satisfies C18 with TAB hold of 2s.
- Fresh evidence:
  - `nativeMapTabOpen=True/False` log delta = `1/1`.
  - `capturePaused/resumingCapture` log delta = `1/1`.
- Manual fallback was implemented but not needed.

## Recommended Next Action

Claude can resume after reading:

- `docs\HANDOFF_BACK_TO_CLAUDE.md`
- `tools\last-gameplay-validation.md`
- `tools\last-full-validation.md`

Recommended next phase: plan the next explicitly requested phase only. `RevealRooms` and `ShowEnemies` remain out of scope until requested.

## Guardrails

- Do not change `src\Core.cs` for TAB automation unless a future task explicitly requests mod logic changes.
- Do not publish.
- Do not push.
- Do not package.
- Do not touch r2modman UI.
- Do not edit `mods.yml`.
