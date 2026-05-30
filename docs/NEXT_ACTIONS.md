# NEXT_ACTIONS.md
# Next actions - updated 2026-05-30 after Codex Phase 2 attempt.

---

## Current State

- Branch: `codex-exec`.
- Build/hash: `c0711156`.
- Static Audit: **PASS 22/22**.
- Build Release: **PASS 0 errors / 0 warnings**.
- Installed DLL hash: **MATCH** (`c0711156`).
- Runtime Probe: **PASS 12/12**.
- Gameplay Validation: **PARTIAL 19/20**.
- Overall: **BLOCKED** on TAB confirmation.
- `ForceHudProofOfLife=false`.
- `CenterOnPlayer=true`.
- `centerOnPlayerApplied=true`.
- `centerOnPlayerProjectionValid=true`.
- `centerOnPlayerDistanceFromCenter=0`.
- HUD/minimap remains visible in `Level`.
- M toggle remains validated by log/JSON.
- TAB synthetic key was sent, but native `MapToolController.Active` did not log open/close.

---

## Immediate Next Objective

Resolve Phase 2 C18 only:

- Confirm native TAB open/close with physical keyboard input while the game is in level, or
- Improve the automation path so Unity InputSystem receives `<Keyboard>/tab`, then rerun gameplay validation.

Do not expand feature scope until Phase 2 is full PASS.

---

## Commands To Resume

Before editing or validating:

```powershell
.\tools\ai_start_work.ps1
```

Useful checks:

```powershell
.\tools\surveyormap_runtime_validate.ps1 -NoLaunch -VerboseReport
.\tools\surveyormap_gameplay_validate.ps1 -NoLaunch -WaitSeconds 120 -VerboseReport
```

At the end of the next round:

```powershell
.\tools\ai_finish_work.ps1 -Message "<checkpoint message>"
```

---

## Out Of Scope

- `RevealRooms=true`.
- `ShowEnemies=true`.
- Premium visual polish.
- Package, Thunderstore, GitHub release, or publishing work.
- Default r2modman profile.
- Other mods.
- Reference mod code copying.
