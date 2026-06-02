# Next Actions — SurveyorMap v2

**Atualizado:** 2026-06-02
**Status:** build 8bdba81b APROVADA — proximo: log cleanup

---

## Proximo Passo Imediato — Log Cleanup

### Problema

O LogOutput/BepInEx esta poluido para release. A cada 2s o sweep emite linhas como:

```
[Debug  :SurveyorMap] [SurveyorMap] Sweep: active=8
[Debug  :SurveyorMap] [SurveyorMap] Sweep removed 0 stale markers. Active=8
[Debug  :SurveyorMap] [SurveyorMap] Room explored (new): Room Volume - Truck ...
[Debug  :SurveyorMap] [SurveyorMap] Map.AddCustom OK: host=...
[Debug  :SurveyorMap] [SurveyorMap] Enemy marker: id=... diff=... threat=... ...
```

Em uma run de 8 inimigos isso gera centenas de linhas de debug por minuto.

### Solucao planejada

1. Adicionar config `DebugLogging = false` (secao `[Debug]`).
2. Todos os `LogDebug` repetitivos ficam condicionais a `DebugLogging = true`.
3. `LogWarning` e `LogError` continuam sempre ativos.
4. Log de classificacao de marcador continua existindo, mas condicionado a `DebugLogging`.
5. BuildTag MD5 e logs de erro/aviso permanecem incondicionais.

### Restricoes obrigatorias

- NAO alterar `NativeMapMirror.cs`.
- NAO alterar `RevealRoomsService.cs`.
- NAO mexer em minimap / TAB / M / F8 / R.
- NAO alterar sistema visual (cor, forma, keywords de elevacao).
- NAO alterar `EnemyMarkerSize` default (0.95).
- NAO alterar `ShowEnemiesInUnexploredRooms` (ja deprecated/no-op).
- NAO publicar. NAO push. NAO mexer no Default profile.

### Entrega esperada

- 0 erros / 0 warnings na build.
- PASS 45/45 static audit.
- Hashes build = install = package = ZIP.
- Commit limpo.
- Com `DebugLogging = false` (default): log silencioso em run normal.
- Com `DebugLogging = true`: mesmo verbose anterior para diagnostico.

---

## Config Completa (estado atual — apos log cleanup)

```ini
[Minimap]
EnableMinimap = true
ToggleKey = M
Width = 260
Height = 260
PosX = 24
PosY = 120
Opacity = 0.85
Zoom = 2.25

[Features]
RevealRoomsMode = Vanilla
ShowEnemies = true
EnemyMarkerSize = 0.95
ShowEnemiesInUnexploredRooms = false  # DEPRECIADO — sem efeito em v1.0

[Debug]
DebugLogging = false  # true = verbose (classificacao, sweep, room, addcustom)

[EditMode]
EditModeEnabled = true
EditModeKey = F8
```

---

## Pendencias de Longo Prazo

### BLOQUEADO — RevealRooms MinimapOnly

**Status:** BLOQUEADO — nenhuma API encontrada que revele na camera sem chamar `RoomVolume.SetExplored()`.

### BACKLOG — ShowEnemiesInUnexploredRooms v1.1

Feature depreciada em v1.0. A API nativa (SetExplored) e chamada repetidamente no quarto do caminhao mas nao nos quartos do nivel em modo Vanilla.

Possiveis caminhos para v1.1:
- Patch em `RoomVolume.OnTriggerEnter` para rastrear entrada do jogador em cada sala
- Layer mask ou shader parameter na camera de mapa

**Nao implementar sem prova visual de que TAB continua vanilla.**

---

## Arquivos Stale — Nao Commitar

- `tools/last-runtime-validation.json` — resultado de build antiga
- `tools/last-runtime-validation.md` — idem
- `tools/archive/*.log` — logs de sessoes anteriores
- `tools/archive/*.png` — screenshots de sessoes anteriores
- `tools/surveyormap_runtime_validate.ps1` — fix cosmetico pendente
