# AGENT_STATE.md
# Source of truth - updated 2026-05-30 after Codex Phase 2 attempt.

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
| Codex Exec worktree | `C:\Users\Hiarly\.codex\PROJETOS\REPO\HiarlyScripter-SurveyorMap-Exec` |
| Source | `src\Core.cs` |
| Build DLL | `build\SurveyorMap.dll` |
| Test profile | `C:\Users\Hiarly\AppData\Roaming\r2modmanPlus-local\REPO\profiles\REPO - Test` |
| Installed DLL | `...\REPO - Test\BepInEx\plugins\HiarlyScripter-SurveyorMap\SurveyorMap.dll` |
| Runtime validation | `tools\last-runtime-validation.*` |
| Gameplay validation | `tools\last-gameplay-validation.*` |
| Full validation | `tools\last-full-validation.*` |

---

## Current Build

| Field | Value |
|---|---|
| Build/hash | `c0711156` |
| Static Audit | PASS 22/22 |
| Build Release | PASS, 0 errors / 0 warnings |
| Installed DLL hash | `c0711156` |
| Runtime Probe | PASS 12/12 |
| Gameplay Validation | PARTIAL 19/20 |
| Full validation | BLOCKED by TAB confirmation |

---

## Confirmed Functional State

| Item | Status |
|---|---|
| BepInEx loads SurveyorMap | PASS, BuildTag `c0711156` found in log |
| Plugin.Awake() runs | PASS |
| Plugin.Update() runs | PASS |
| Plugin.OnGUI() runs | PASS |
| RuntimeProbe.Update() runs | PASS |
| RuntimeProbe.OnGUI() runs | PASS |
| Runtime JSON exists and is fresh | PASS |
| currentRunState | `Level` |
| hudVisible in level | `true` |
| nativeTextureReady in level | `true` |
| nativeMapCaptureReady in level | `true` |
| minimapBaselineVisible in level | `true` |
| lastCaptureReason | `native-map-rendered` |
| lastGateReason | `gameplay-active` |
| ForceHudProofOfLife | `false` |
| CenterOnPlayer | `true` |
| CenterOnPlayer applied | `true` |
| CenterOnPlayer projection valid | `true` |
| CenterOnPlayer distance from center | `0` |
| M toggle | PASS, validated by log/JSON |
| TAB open/close | BLOCKED, synthetic TAB sent but not confirmed by MapToolController log |
| lastException | `null` |
| lastErrorStack | `null` |

---

## Active Configuration

```ini
ForceHudProofOfLife = false
CenterOnPlayer = true
RevealRooms = false
RevealMode = Off
ShowEnemies = false
EnemyDetectionMode = Off
SafeMode = true
CaptureFPS = 5
RenderTextureSize = 256
```

---

## Phase State

Phase 1 baseline minimap remains complete with PASS.

Phase 2 implementation evidence:
- `CenterOnPlayer=true` migrated/applied.
- Minimap remained visible in `Level`.
- Player-centered pan applied with `centerOnPlayerDistanceFromCenter=0`.
- Runtime stayed clean: `lastException=null`, `lastErrorStack=null`.
- M toggle still works.

Blocking item:
- C18 TAB open/close did not confirm through synthetic key injection.
- Local game API shows native map uses Unity InputSystem binding `<Keyboard>/tab`.
- Win32 synthetic TAB attempts did not toggle `MapToolController.Active`; M still reached the plugin through legacy Unity input.

---

## Next Step

Next recommended action: have Claude or the user confirm native TAB open/close with physical input, or improve validation automation so Unity InputSystem receives the TAB event. Do not start RevealRooms, ShowEnemies, visual polish, package, Thunderstore, or GitHub work until Phase 2 is fully PASS.

---

## Permanent Rules

- Do not publish to Thunderstore.
- Do not push to GitHub.
- Do not touch the Default profile.
- Do not touch other mods.
- Do not copy code from reference mods.
- Do not declare Phase 2 fully PASS until TAB open/close is objectively confirmed.
