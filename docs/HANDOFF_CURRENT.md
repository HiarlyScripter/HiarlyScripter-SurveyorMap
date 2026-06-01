# Handoff Current — SurveyorMap v2

**Atualizado:** 2026-06-01  
**Por:** Claude (sessão de evolução controlada)  
**Validado pelo usuário:** pendente gameplay manual

---

## Estado Atual

| Item | Status |
|---|---|
| Branch | `codex-exec` |
| Commit base | `be3e7f9` (clean rebuild baseline) |
| Build hash | `f516283d` |
| Static Audit | PASS 45/45 |
| Build Release | PASS 0 erros / 0 warnings |
| Runtime Validator (8 critérios) | pendente (requer gameplay) |
| Gameplay — minimap | herda baseline (não regredido, requer confirmação) |
| Gameplay — M toggle | herda baseline |
| Gameplay — TAB safe | herda baseline |
| Gameplay — RevealRooms | OFF por padrão (TAB vanilla preservado) |
| Gameplay — ShowEnemies cleanup | implementado, requer validação |
| Gameplay — markers diferenciados | implementado, requer validação |
| Edit mode F8 | implementado, requer validação |

## O Que Foi Feito Nesta Sessão

### Pendência 3 — Cleanup de inimigos mortos (CONCLUÍDA)
- Registry `Dictionary<int, MarkerEntry>` por `EnemyParent.instanceID`
- `AddMarker`: checa duplicata antes de criar
- `RemoveMarker`: cleanup por id no registry
- `SweepDeadMarkers()`: chamado a cada 2s no `Update()` — remove entradas stale
- `ClearAll()`: chamado em `GenerateDone` para limpar marcadores do level anterior
- Patch manual de `EnemyHealth.DeathRPC` e `DeathImpulseRPC` via reflection (não crasheia se método não existir)
- `EnemyHealth` cacheado no `MarkerEntry` para evitar alocações no sweep
- `ActiveMarkerCount` reportado direto do registry (não mais contador manual que derivava)

### Pendência 4 — Tamanho e forma dos marcadores (CONCLUÍDA)
- 4 formas procedurais: círculo (D1), losango (D2), triângulo (D3), estrela (D4+)
- 4 cores: verde/amarelo/laranja/vermelho por dificuldade
- `EnemyMarkerSize` config (multiplicador, default 1.0)
- `EnemyMarkerShapeMode` config (DifficultyShape | Circle)
- Escala aplicada via `MapCustomEntity.transform.localScale`
- Sprites cacheadas por categoria (não recriadas)

### Pendência 1 — RevealRooms (PARCIALMENTE CONCLUÍDA)
- `RevealRooms: bool` substituído por `RevealRoomsMode: string`
- Default: `"Off"` — TAB vanilla preservado, sem `SetExplored()` chamado
- `"NativeGlobal"` = comportamento anterior (opt-in) — também revela no TAB
- `MinimapOnly` = **BLOQUEADO** — sem implementação segura encontrada
- README e CHANGELOG documentam status honesto

### Pendência 5 — Edit mode in-game (CONCLUÍDA)
- F8 entra/sai do edit mode (apenas fora do TAB)
- Arrastar: reposiciona minimap
- Canto inferior direito: resize
- Scroll wheel: zoom
- Info overlay: X/Y/W/H/Z
- `Config.Save()` automático ao sair
- Configs: `EditModeEnabled`, `EditModeKey`

## Arquitetura Atual (6 arquivos src/)

- `src/Core.cs` — entrypoint, toggle M, sweep, edit mode, patch death RPCs
- `src/SurveyorMapConfig.cs` — todos os bindings de config
- `src/NativeMapMirror.cs` — camera nativa + activeTexture + TAB detection
- `src/RevealRoomsService.cs` — modo NativeGlobal apenas, ClearAll no level load
- `src/EnemyMapMarkerService.cs` — registry + shapes + sweep + cleanup
- `src/SurveyorMapDiagnostics.cs` — JSON runtime para validators

## Pendências Remanescentes

| # | Pendência | Status |
|---|---|---|
| 1 | RevealRooms MinimapOnly | BLOQUEADO — requer nova auditoria de API |
| 2 | Ícones diferenciados | IMPLEMENTADO (pendente confirmação gameplay) |
| 3 | Cleanup marcadores mortos | IMPLEMENTADO (pendente confirmação gameplay) |
| 4 | Tamanho ícones configurável | IMPLEMENTADO (pendente confirmação gameplay) |
| 5 | Edit mode in-game | IMPLEMENTADO (pendente confirmação gameplay) |

## Próximos Passos

1. Usuário roda gameplay manual para validar cleanup, markers diferenciados, edit mode
2. Capturar screenshots: minimap ativo, TAB vanilla, markers diferenciados, cleanup pós-morte, edit mode
3. Rodar `.\tools\surveyormap_runtime_validate.ps1 -NoLaunch` após gameplay
4. Se PASS 8/8, criar ZIP local e commit final
5. RevealRooms MinimapOnly fica documentado como BLOQUEADO para próxima auditoria de API
