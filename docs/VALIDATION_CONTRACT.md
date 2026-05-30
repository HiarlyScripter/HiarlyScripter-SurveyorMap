# VALIDATION_CONTRACT.md
# Critérios objetivos de aceitação por fase. Atualizado 2026-05-30.

---

## Fase 0 — Runtime Baseline (CONCLUÍDA ✅)

**PASS em 2026-05-30. Build: `1f9b9583`.**

Critérios (12/12 PASS):

| # | Critério | Fonte | Status |
|---|---|---|---|
| 1 | hashMatch: build == installed | Hash MD5 | ✅ |
| 2 | modEnabled: SurveyorMap enabled no mods.yml | mods.yml | ✅ |
| 3 | BuildTag atual no LogOutput.log | Log | ✅ |
| 4 | JSON runtime criado e fresh (<30 min) | Arquivo | ✅ |
| 5 | pluginAwakeCalled=true | JSON | ✅ |
| 6 | pluginUpdateCount > 0 | JSON | ✅ |
| 7 | pluginOnGuiCount > 0 | JSON | ✅ |
| 8 | runtimeProbeCreated=true | JSON | ✅ |
| 9 | runtimeProbeUpdateCount > 0 | JSON | ✅ |
| 10 | runtimeProbeOnGuiCount > 0 | JSON | ✅ |
| 11 | lastException=null | JSON | ✅ |
| 12 | lastErrorStack=null | JSON | ✅ |

---

## Fase 1 — Minimap Baseline em Level (PENDENTE)

**Gate para avançar:** todos os critérios abaixo verdadeiros após entrar em level solo.

| # | Critério | Fonte |
|---|---|---|
| 1-12 | Todos os critérios da Fase 0 | (herdados) |
| 13 | currentRunState=Level | JSON |
| 14 | hudVisible=true | JSON |
| 15 | nativeTextureReady=true | JSON |
| 16 | nativeMapCaptureReady=true | JSON |
| 17 | lastCaptureReason=native-map-rendered | JSON |
| 18 | lastException=null (mantido no level) | JSON |

**TAB evidence:** validação manual obrigatória (não automatizável via script):
- nativeTabOpen=true quando TAB abre → log
- capturePaused → log
- nativeTabOpen=false quando TAB solta → log
- captureResumed → log

---

## Fases futuras (só após Fase 1 PASS)

| Fase | Objetivo | Gate |
|---|---|---|
| Fase 2 | Toggle M liga/desliga | Canvas ativa/desativa + toggleKeyDetectedCount > 0 |
| Fase 3 | CenterOnPlayer=true | Player centralizado, sem crash |
| Fase 4 | RevealRooms=true | Overlay de salas aparecendo |
| Fase 5 | ShowEnemies=true | Marcadores de inimigos (abordagem clay-BetterMap) |
| Fase 6 | Visual premium | Sem definição ainda |
| Fase 7 | Package/release local | Todas anteriores PASS |

---

## Regras permanentes

- Não declarar feature funcionando sem evidência objetiva (log ou JSON)
- Não avançar de fase sem todos os critérios satisfeitos
- Não usar teste visual manual como única validação
- Relatório 12/12 = exatamente 12 critérios verificados objetivamente
