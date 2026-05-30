# COMPACT_RESUME_SURVEYORMAP.md
# Checkpoint de Continuidade — SurveyorMap
# Criado em: 2026-05-30 03:29

---

## Estado Atual do Fluxo

- **Codex** não implementa mais. Usado apenas como referência histórica/handoff.
- **Claude Code** é o agente principal de implementação.
- **ChatGPT** atua como arquiteto estratégico — revisa logs, prompts e riscos.
- **Projeto ativo:** `C:\Users\Hiarly\.claude\PROJETOS\REPO\HiarlyScripter-SurveyorMap`
  (não mais em `.codex`)
- **Perfil de teste principal:** `REPO - Test`
  (não usar perfil Default como alvo de instalação)

---

## Objetivo Final do Mod

SurveyorMap deve se tornar um **minimap/ESP avançado** para R.E.P.O., incluindo:

- HUD no canto inferior esquerdo (260×260)
- Minimap player-centered (jogador sempre no centro)
- Mapa/reveal de salas com overlay de geometria
- Enemy ESP — marcadores com formas geométricas e cores por nível de perigo
- Visual premium (bordas, opacidade, zoom configurável)
- Sem quebrar o mapa TAB nativo
- Sem copiar código dos mods referência (dig-Minimap, clay-BetterMap)

---

## Estado Funcional Confirmado (pré-compact)

| Item | Status |
|---|---|
| TAB nativo voltou a funcionar | ✅ Confirmado no log e visualmente |
| Pressionar TAB não deixa mais mapa preto | ✅ Confirmado |
| Soltar TAB também está OK | ✅ Confirmado |
| BuildTag presente no log (`md5=b0a5a93c`) | ✅ |
| Build compilando 0 errors / 0 warnings | ✅ |
| DLL instalada no perfil REPO - Test | ✅ hash=B0A5A93C7DDF7DCDC5DF30D15CA7B803 |
| SurveyorMap carrega e inicializa | ✅ Confirmado no log |
| HUD canvas criado (active=False na inicialização) | ✅ |
| RuntimeLoop started + scheduled | ✅ |

---

## Problema Atual Principal

**O minimap do SurveyorMap NÃO aparece no level e NÃO responde ao M.**

Evidência no log (LogOutput.log do REPO - Test):

```
[Info] SurveyorMap v1.0.0 loading.
[Info] BuildTag: 2026-05-29 21:10:41 | md5=b0a5a93c | ...SurveyorMap.dll
[Info] HUD canvas created. ... active=False
[Info] RuntimeLoop started.
[Info] RuntimeLoop scheduled.
[Info] SurveyorMap initialized.
[Info] Changed level to: Level - Museum   ← jogo entrou no level
[Info] Changed level to: Level - Manor    ← reload por morte
```

**Após esses loads, NENHUMA linha do SurveyorMap aparece:**
- Sem `RunState=Level`
- Sem `HUD visible=True`
- Sem `NativeMapCapture`
- Sem `Toggle key detected`
- Sem `NativeMapState`

**Causa provável:** bug de lifecycle — coroutine/controller/gate não reavalia depois de scene load ou reload. O RuntimeLoop possivelmente morre na troca de cena (coroutines no BepInEx plugin são destruídas junto com o plugin GameObject se o jogo usa DontDestroyOnLoad mas o contexto da cena muda) ou o gate (`IsHudGateOpenForCapture`) nunca retorna `true` após o load.

---

## Estado de Configs (Baseline NativeMapMirror)

```
CenterOnPlayer        = false
RevealRooms           = false
RevealMode            = Off
ShowEnemies           = false
EnemyDetectionMode    = Off
ShowNativeQuestionMarks = true
LayoutDefaultsVersion = 7
```

Esses valores estão corretos e não devem ser alterados até o baseline funcionar.

---

## Decisão Estratégica

Antes de reativar features, **corrigir o baseline** para o minimap aparecer no level.

**Ordem obrigatória de implementação após compact:**

1. Corrigir lifecycle/runtime → minimap aparece no level
2. Preservar TAB nativo funcionando
3. Reduzir warnings/spam próprios do SurveyorMap (rebaixar para Debug/rate-limited)
4. Só depois: reativar `CenterOnPlayer=true`
5. Só depois: reativar `RevealRooms=true`
6. Só depois: reativar `ShowEnemies=true`
7. Só depois: visual premium

---

## Restrições Permanentes

- Não recomeçar do zero
- Não reescrever código de forma ampla sem necessidade
- Não mexer em outros mods (QuickPocket, REPOConfig, etc.)
- Não copiar código de dig-Minimap ou clay-BetterMap
- Mods referência: usar apenas como bússola/auditoria, nunca como fonte de código
- Não publicar no Thunderstore
- Não fazer push no GitHub
- Sempre criar checkpoint antes de mudança crítica
- Sempre compilar e instalar no REPO - Test
- Sempre comparar hash build vs instalado
- Não declarar sucesso sem log ou teste visual confirmado
