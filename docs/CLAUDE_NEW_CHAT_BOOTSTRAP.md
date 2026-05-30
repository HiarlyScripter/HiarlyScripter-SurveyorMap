# CLAUDE_NEW_CHAT_BOOTSTRAP.md
# Documento de bootstrap para novo chat — leia antes de qualquer ação.

---

## Identidade do projeto

- **Projeto:** SurveyorMap — mod original para R.E.P.O. com BepInEx/Harmony/Unity
- **Autor:** HiarlyScripter
- **Agentes:**
  - **Claude Code** = executor principal de código
  - **Codex** = auditor read-only apenas (não implementa)
  - **ChatGPT** = arquiteto/orquestrador estratégico
- **REGRA:** Não confiar na memória do chat anterior. Usar arquivos do projeto como fonte de verdade.

---

## Caminhos críticos

| Item | Caminho |
|---|---|
| Projeto ativo | `C:\Users\Hiarly\.claude\PROJETOS\REPO\HiarlyScripter-SurveyorMap` |
| Source principal | `...\src\Core.cs` |
| DLL build | `...\build\SurveyorMap.dll` |
| Perfil de teste | `C:\Users\Hiarly\AppData\Roaming\r2modmanPlus-local\REPO\profiles\REPO - Test` |
| DLL instalada | `...\REPO - Test\BepInEx\plugins\HiarlyScripter-SurveyorMap\SurveyorMap.dll` |
| Log de teste | `...\REPO - Test\BepInEx\LogOutput.log` |
| JSON diagnóstico | `...\REPO - Test\BepInEx\plugins\HiarlyScripter-SurveyorMap\diagnostics\surveyormap-runtime-state.json` |
| Script audit | `...\tools\surveyormap_static_audit.ps1` |
| Script smoke | `...\tools\surveyormap_smoke_test.ps1` |
| Script validate | `...\tools\surveyormap_runtime_validate.ps1` |

---

## Estado atual confirmado

### Funcionando
- r2modman lista o SurveyorMap no perfil REPO - Test ✅
- TAB nativo funciona ✅
- TAB não fica preto ✅

### Quebrado
- O minimap do SurveyorMap NÃO aparece visualmente (menu, level, M)
- O ProofOfLife box (build `4cc93384`) não apareceu no log nem visualmente
- `Plugin.Update absolute alive` nunca apareceu no log mesmo sem gate
- `OnGUI absolute proof reached` nunca apareceu no log
- Nenhum PASS runtime confiável foi obtido ainda

---

## Diagnóstico atual

### Hipótese principal (forte)
O plugin's `gameObject` era destruído na primeira transição de cena (`Leave to Main Menu`)
porque não havia `DontDestroyOnLoad` explícito. O `Awake` e a primeira iteração síncrona
do RuntimeLoop rodavam, mas depois o objeto era destruído → `Update()`, `OnGUI()` e
coroutine nunca chamados.

### Correção aplicada (última build `4cc93384`)
- `DontDestroyOnLoad(gameObject)` + `gameObject.transform.parent = null` no Awake
- `RuntimeProbeBehaviour` — MonoBehaviour em GameObject separado com DontDestroyOnLoad explícito
- JSON runtime escrito pelo probe
- Scripts de validação em `tools/`
- Auditoria estática: 21/21 PASS

### Ainda pendente
- Validação runtime objetiva (PASS/FAIL automático)
- Teste visual real não executado com a build `4cc93384`

---

## Build atual

| Campo | Valor |
|---|---|
| MD5 curto | `4cc93384` |
| MD5 completo build | `4CC93384400CD88E7EE63A94B30D59FC` |
| Arquivo build | `...\build\SurveyorMap.dll` |
| Arquivo instalado | `...\REPO - Test\BepInEx\plugins\...\SurveyorMap.dll` |
| Hashes iguais | Sim (confirmado na última sessão) |

---

## Regras permanentes

- Não publicar no Thunderstore
- Não push no GitHub
- Não mexer em outros mods
- Não mexer no perfil Default
- Não copiar código de dig-Minimap ou clay-BetterMap
- Dois agentes não editam ao mesmo tempo
- Criar checkpoint antes de qualquer mudança crítica
- Não declarar sucesso sem evidência de log/teste
