# CURRENT_STATE_FOR_AGENTS.md
# Estado atual do projeto para agentes — fonte de verdade desta rodada.

---

## Estado funcional confirmado

| Item | Status |
|---|---|
| r2modman lista SurveyorMap no REPO - Test | ✅ Confirmado |
| TAB nativo funciona | ✅ Confirmado |
| TAB não fica preto | ✅ Confirmado |
| BepInEx carrega SurveyorMap | ✅ Confirmado (BuildTag aparece no log) |

---

## Estado quebrado

| Item | Status |
|---|---|
| SurveyorMap aparece visualmente | ❌ Não aparece |
| Minimap no level | ❌ Não aparece |
| M liga/desliga | ❌ Não faz nada |
| ProofOfLife box apareceu | ❌ Não apareceu (build `4cc93384`) |
| Plugin.Update absolute alive no log | ❌ Nunca apareceu |
| OnGUI absolute proof reached no log | ❌ Nunca apareceu |
| PASS runtime confirmado | ❌ Não obtido ainda |

---

## Configs baseline (não alterar até baseline funcionar)

```
CenterOnPlayer        = false  ← NÃO REATIVAR
RevealRooms           = false  ← NÃO REATIVAR
RevealMode            = Off    ← NÃO REATIVAR
ShowEnemies           = false  ← NÃO REATIVAR
EnemyDetectionMode    = Off    ← NÃO REATIVAR
ForceHudProofOfLife   = true   ← diagnóstico ativo
```

---

## Próxima fase obrigatória

Antes de qualquer feature nova:

1. **Validação runtime automatizada** — executar `tools\surveyormap_runtime_validate.ps1`
2. **Static audit** — `tools\surveyormap_static_audit.ps1` (já rodou 21/21 PASS)
3. **Smoke test** — `tools\surveyormap_smoke_test.ps1` após o jogo rodar
4. **Obter PASS/FAIL** — baseado em `LogOutput.log` + `runtime-state.json`
5. **Só depois:** avançar para CenterOnPlayer → RevealRooms → ShowEnemies → visual premium

---

## Proibido (sem exceção)

- Publicar no Thunderstore
- Push no GitHub
- Mexer em outros mods
- Mexer no perfil Default
- Copiar código de dig-Minimap ou clay-BetterMap
- Dois agentes editando simultaneamente
- Declarar sucesso sem evidência de log/PASS

---

## Checkpoints disponíveis (em releases/)

| Nome | O que contém |
|---|---|
| `pre-compact-runtime-baseline-state-20260530-032902` | Estado estável pré-compact da sessão anterior |
| `pre-runtime-baseline-fix-20260530-034944` | Antes do fix de gameplayState gate |
| `pre-runtime-loop-hotfix-20260530-044316` | Antes do Plugin.Update fallback |
| `pre-reference-architecture-runtime-fix-20260530-080150` | Antes da mudança para dig-style activeTexture |
| `pre-absolute-proof-of-life-20260530-092337` | Antes do proof absoluto sem gate |
| `pre-runtime-smoke-test-infra-20260530-094730` | Antes de DontDestroyOnLoad fix + RuntimeProbe |
