# Handoff Current — SurveyorMap v2

**Atualizado:** 2026-06-02
**Por:** Claude (checkpoint pos-validacao gameplay — antes de log cleanup)
**Validado pelo usuario:** build 8bdba81b APROVADA manualmente

---

## Estado do Build

| Item | Valor |
|---|---|
| Branch | `codex-exec` |
| HEAD commit | `4d42c8a` |
| Build hash | `8bdba81b` |
| Install hash (REPO - Test) | `8bdba81b` |
| Package DLL hash | `8bdba81b` |
| ZIP DLL interna | `8bdba81b` |
| Static Audit | PASS 45/45 |
| Tag de checkpoint | `checkpoint/validated-gameplay-before-log-cleanup-20260602` |

---

## Funcionalidades Aprovadas Manualmente (build 8bdba81b)

| Funcionalidade | Status |
|---|---|
| Minimap persistente HUD | APROVADO |
| TAB (mapa nativo) | APROVADO — sem regressao |
| M toggle | APROVADO |
| F8 edit mode | APROVADO |
| R reset (edit mode) | APROVADO |
| Inimigos aparecem no mapa | APROVADO |
| EnemyMarkerSize = 0.95 via REPOConfig | APROVADO |
| Cleanup / despawn em ~2s | APROVADO |
| Sistema visual: cor + forma = ameaca | APROVADO |
| Sem amarelo/dourado nos markers | CONFIRMADO |

---

## Sistema Visual Final (commit 4d42c8a)

**Modelo: Forma e Cor = Ameaca (redundantes)**

| Ameaca | Forma | Cor | Hex |
|---|---|---|---|
| Low / Easy | Circulo | Verde-gelo | `#DFFFE8` |
| Medium | Quadrado | Azul escuro | `#1E6BFF` |
| High | Triangulo | Lila | `#C084FC` |
| Elite / Critical | Estrela | Vermelho/coral | `#FF3B30` |

Keywords elevam tier apenas (nunca escolhem cor separada):
- `veryheavy` → pelo menos High
- `trudge`, `slow walker`, `boss`, `elite` → Elite
- `hunt`, `huntsman`, `bang`, `rush`, `charge` → pelo menos High

`ShowEnemiesInUnexploredRooms` — DEPRECIADO, sem efeito em v1.0.

---

## Problema Restante (a resolver na proxima rodada)

**Log cleanup:** O LogOutput/BepInEx esta poluido demais para release.

Causa: `SweepDeadMarkers()` roda a cada 2s e emite `LogDebug` para cada sweep e marcador ativo. Em uma run com 8 inimigos = ~4 linhas a cada 2s = spam continuo durante toda a run.

Solucao planejada:
1. Adicionar `DebugLogging = false` como config (secao `[Debug]`).
2. Mover logs repetitivos (`Sweep:`, `Room explored`, `Map.AddCustom OK`) para `LogDebug` condicional — so emitir se `DebugLogging = true`.
3. Manter `LogWarning` e `LogError` sempre ativos (nao sao spam).
4. Manter log de classificacao de marcador (`Enemy marker: id=...`) como `LogDebug` condicional.

**Restricoes para log cleanup:**
- Nao alterar `NativeMapMirror.cs`.
- Nao alterar `RevealRoomsService.cs`.
- Nao mexer em minimap/TAB/M/F8/R.
- Nao alterar sistema visual (cor, forma, keywords).
- Nao alterar `EnemyMarkerSize` default (0.95).
- Nao alterar `ShowEnemiesInUnexploredRooms` (ja deprecated).

---

## Historico de Commits desta Fase

| Commit | Descricao |
|---|---|
| `56987f3` | fix: restore enemy marker visibility (RoomVolumeExploredPatch + fail-open) |
| `2daa069` | fix: enemy marker size/config/shape/room-tracking improvements |
| `3a8794a` | feat: enemy visual system v2 — Shape=ThreatTier, Color=Family |
| `4d42c8a` | fix: enemy visual alignment — colour = threat tier (redundant with shape) |

---

## O Que NAO Foi Alterado

- `NativeMapMirror.cs` — INTOCADO
- `RevealRoomsService.cs` — INTOCADO
- `SurveyorMapDiagnostics.cs` — INTOCADO
- `Core.cs` — INTOCADO
- TAB / M toggle / F8 edit mode / R reset — INTOCADOS
- `RevealRoomsMode = Vanilla` por padrao — preservado

---

## Working Tree (nao commitado — nao mexer)

| Arquivo | Tipo | Acao |
|---|---|---|
| `docs/HANDOFF_CURRENT.md` | Este doc | Parte do checkpoint commit |
| `docs/NEXT_ACTIONS.md` | Proximas acoes | Parte do checkpoint commit |
| `docs/AGENT_STATE.md` | Estado do agente | Parte do checkpoint commit |
| `tools/last-runtime-validation.json` | Build antiga | NAO commitar |
| `tools/last-runtime-validation.md` | Idem | NAO commitar |
| `tools/surveyormap_runtime_validate.ps1` | Fix cosmetico | NAO commitar |

Untracked (NAO commitar): `package/SurveyorMap.dll`, `tools/archive/*.log`, `tools/archive/*.png`

---

## Pendencias

| Item | Status |
|---|---|
| Log cleanup (DebugLogging config) | PROXIMO — aguardando prompt |
| RevealRooms MinimapOnly | BLOQUEADO permanentemente |
| ShowEnemiesInUnexploredRooms v1.1 | Backlog — depende de API confiavel |
