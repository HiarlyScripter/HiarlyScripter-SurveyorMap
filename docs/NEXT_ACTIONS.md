# NEXT_ACTIONS.md
# Próximas ações — atualizado 2026-05-30 pós-hardening.

---

## Estado atual

- Runtime baseline: **PASS** (build `1f9b9583`)
- Config baseline: **aplicada** (ForceHudProofOfLife=false, SafeMode=true, etc.)
- Validador: **corrigido** (12 critérios, freshness check, JSON enxuto)
- Fase 1 (minimap em level): **pendente**

---

## Próximo objetivo: Fase 1 — Minimap baseline em level

### Comandos obrigatórios
```powershell
cd "C:\Users\Hiarly\.claude\PROJETOS\REPO\HiarlyScripter-SurveyorMap"
.\tools\surveyormap_static_audit.ps1
.\tools\surveyormap_full_validate.ps1 -LaunchGame -WaitSeconds 90 -VerboseReport
```

### O que o usuário precisa fazer
1. Abrir r2modman → REPO - Test → Start modded
2. Aguardar menu carregar
3. Criar partida solo
4. Entrar no level e aguardar ~10s
5. Opcionalmente: pressionar M para testar toggle
6. Opcionalmente: pressionar TAB para testar mapa nativo
7. Fechar o jogo ou aguardar validação

### Critérios de PASS da Fase 1
Ver `docs/VALIDATION_CONTRACT.md` — Fase 1 (critérios 13-18).

---

## Escopo permitido agora

- Validar minimap baseline visual em level
- Ajustar config se necessário (não reativar features)
- Corrigir bugs de captura se nativeTextureReady=false

## Fora de escopo

- CenterOnPlayer, RevealRooms, ShowEnemies
- Visual premium
- Package/Thunderstore/GitHub

---

## Guia de diagnóstico se Fase 1 FAIL

| Sintoma | Causa provável | Ação |
|---|---|---|
| currentRunState != Level | Gate de level não abre | Verificar SemiFunc.RunIsLevel() + GameDirector state |
| hudVisible=false | Toggle off ou gate fechado | Verificar playerApiReady, enableMinimap, nativeTextureReady |
| nativeTextureReady=false | Camera não encontrada ou activeTexture null | Ver log: `Dirt Finder Map Camera missing` ou `native activeTexture not ready` |
| lastCaptureReason != native-map-rendered | Outro estado da captura | Ler campo lastCaptureReason no JSON |
