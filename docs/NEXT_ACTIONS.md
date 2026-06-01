# Next Actions — SurveyorMap v2

**Atualizado:** 2026-06-01  
**Status:** aguardando validação gameplay das novas features

---

## Validação Imediata Necessária

### Gameplay — Enemy cleanup (Pendência 3)

**O que verificar:**
- Entrar em uma run, esperar inimigos spawnar
- Verificar markers aparecem com formas/cores diferentes por dificuldade
- Matar inimigos → markers devem desaparecer (dentro de ~2s pelo sweep)
- Inimigos despawnados → markers removidos
- Reiniciar run → marcadores do level anterior não persistem

**Log esperado:**
```
[SurveyorMap] Patched EnemyHealth.DeathRPC
[SurveyorMap] Enemy marker added: id=... cat=Easy/Medium/Hard/Elite host=...
[SurveyorMap] Sweep removed N stale markers. Active=M
```

### Gameplay — Edit mode F8 (Pendência 5)

**O que verificar:**
- F8 ativa/desativa edit mode
- Drag funciona (minimap move)
- Resize pelo canto (muda tamanho)
- Scroll sobre minimap muda zoom
- Info overlay exibe X/Y/W/H/Z
- F8 de saída salva o config
- F8 não funciona durante TAB aberto

### Gameplay — RevealRooms Off (Pendência 1)

**O que verificar:**
- TAB nativo deve mostrar salas com "?" e fundo preto como vanilla
- Se RevealRoomsMode=Off (default): nenhuma sala revelada via SetExplored()
- Log NÃO deve conter `RevealRooms NativeGlobal:`

---

## Pendências por Status

### IMPLEMENTADO — Pendente confirmação gameplay

| Pendência | Feature |
|---|---|
| 3 | Cleanup de marcadores mortos/despawnados/inativos |
| 4 | Tamanho dos ícones menor e configurável (`EnemyMarkerSize`) |
| 2 | Ícones diferenciados por dificuldade (forma + cor) |
| 5 | Edit mode in-game (F8) |

### BLOQUEADO

| Pendência | Feature | Razão |
|---|---|---|
| 1 | RevealRooms MinimapOnly | Sem API que permita revelar na câmera sem chamar `SetExplored()`. Risco alto de overlay fake. Documentado como BLOQUEADO. |

---

## RevealRooms MinimapOnly — Análise para Próxima Rodada

**Problema:** `RoomVolume.SetExplored()` é global — afeta TAB nativo.

**Caminhos possíveis a auditar:**
1. Existe `MapModule.ShowRoom()` ou equivalente que opere só na câmera de mapa?
2. `RoomVolume.Explored` pode ser setado temporariamente e revertido (não é seguro em multiplayer)
3. Existe um shader parameter ou layer mask na câmera de mapa que revele salas sem mutar estado?
4. Criar câmera separada apontada para o mesmo mundo, mas com culling diferente?

**Critério de aceite:** SOMENTE pode ser considerado PASS se prova visual confirmar que TAB continua com salas pretas/? após `ClearAll()` reset no GenerateDone.

**Não implementar sem auditoria de API prévia.**

---

## Configs Atuais

```
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
RevealRoomsMode = Vanilla      # Off (default/safe) | NativeGlobal (opt-in, afeta TAB)
ShowEnemies = true
EnemyMarkerSize = 1.0      # 0.1–3.0
EnemyMarkerShapeMode = DifficultyShape  # DifficultyShape | Circle

[EditMode]
EditModeEnabled = true
EditModeKey = F8
```
