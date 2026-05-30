# SurveyorMap Full Validation Report

**Date:** 2026-05-30  
**Build/hash:** `b46919ef`  
**Verdict:** **PASS**  
**Overall:** **PASS**

This report consolidates the already collected validation evidence. No build,
install, DLL copy, r2modman action, package update, push, or publish was
performed during this documentation sync.

## Summary

| Phase | Result | Evidence |
|---|---|---|
| Static Audit | PASS | 22/22 checks passed |
| Build Release | PASS | 0 errors / 0 warnings |
| Runtime Probe | PASS | 12/12 criteria in `tools/last-runtime-validation.*` |
| Gameplay Baseline | PASS | 18/18 criteria in `tools/last-gameplay-validation.*` |
| Overall | PASS | All required phases PASS |

## Consolidated Runtime Evidence

| Field | Value |
|---|---|
| buildTag | `b46919ef` |
| hashMatch | `true` |
| modEnabled | `true` |
| buildTagInLog | `true` |
| currentRunState | `Level` |
| hudVisible | `true` |
| nativeTextureReady | `true` |
| nativeMapCaptureReady | `true` |
| minimapBaselineVisible | `true` |
| lastCaptureReason | `native-map-rendered` |
| lastGateReason | `gameplay-active` |
| ForceHudProofOfLife | `false` |
| toggleKeyDetectedCount | `4` in JSON, `8` in log |
| TAB open/close | `1/1` in log |
| lastException | `null` |
| lastErrorStack | `null` |

## Source Reports

- Runtime report: `tools/last-runtime-validation.md`
- Runtime JSON: `tools/last-runtime-validation.json`
- Gameplay report: `tools/last-gameplay-validation.md`
- Gameplay JSON: `tools/last-gameplay-validation.json`

## Notes

- Runtime Probe PASS is supported by 12/12 criteria.
- Gameplay Baseline PASS is supported by 18/18 criteria.
- M toggle was validated by log/JSON evidence.
- TAB open/close was validated by log evidence.
- Minimap baseline was validated while the run state was `Level`.
- RevealRooms and ShowEnemies remain out of scope.
- Next phase is Phase 2: `CenterOnPlayer=true`.

