# NEXT_ACTIONS.md
# Next actions - updated 2026-05-30 after full validation PASS.

---

## Current State

- Phase 1 baseline minimap: **COMPLETE / PASS**.
- Build/hash: `b46919ef`.
- Static Audit: **PASS 22/22**.
- Build Release: **PASS 0 errors / 0 warnings**.
- Runtime Probe: **PASS 12/12**.
- Gameplay Baseline: **PASS 18/18**.
- Overall: **PASS**.
- `ForceHudProofOfLife=false`.
- Minimap baseline was validated while `currentRunState=Level`.
- M toggle was validated by log/JSON.
- TAB open/close was validated by log.

---

## Next Objective

Phase 2 - `CenterOnPlayer=true`.

Only begin Phase 2 after the acting agent reads:
- `docs\START_HERE_FOR_AI.md`
- `docs\AI_GIT_PROTOCOL.md`
- `docs\HANDOFF_CURRENT.md`
- `docs\VALIDATION_CONTRACT.md`
- `tools\last-full-validation.md`

Before editing, run:

```powershell
.\tools\ai_start_work.ps1
```

At the end of the round, run:

```powershell
.\tools\ai_finish_work.ps1 -Message "<checkpoint message>"
```

---

## Scope Allowed For Next Implementation Round

- Enable and validate `CenterOnPlayer=true`.
- Keep the existing Phase 1 minimap baseline intact.
- Preserve objective log/JSON validation for every claim.

---

## Out Of Scope

- `RevealRooms=true`.
- `ShowEnemies=true`.
- Premium visual polish.
- Package, Thunderstore, GitHub release, or publishing work.
- r2modman profile changes unless a future explicit validation round requires them.
- Any unrelated mod.

---

## Phase 2 PASS Expectations

The exact Phase 2 criteria live in `docs\VALIDATION_CONTRACT.md`. At minimum:
- Phase 1 remains PASS.
- `CenterOnPlayer=true` is active.
- Player-centered minimap behavior is validated without crash.
- Runtime still reports `lastException=null` and `lastErrorStack=null`.
- Validation evidence is captured in log/JSON.

