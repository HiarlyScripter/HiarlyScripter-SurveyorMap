# AGENT_STATE.md
# Fonte de verdade — atualizado 2026-05-30 após PASS runtime baseline.

---

## Identidade do projeto

| Campo | Valor |
|---|---|
| Nome | SurveyorMap |
| Autor | HiarlyScripter |
| GUID | com.hiarlyscripter.surveyormap |
| Versão | 1.0.0 |

---

## Caminhos críticos

| Item | Caminho |
|---|---|
| Raiz do projeto | `C:\Users\Hiarly\.claude\PROJETOS\REPO\HiarlyScripter-SurveyorMap` |
| Source | `...\src\Core.cs` |
| Build DLL | `...\build\SurveyorMap.dll` |
| Perfil de teste | `C:\Users\Hiarly\AppData\Roaming\r2modmanPlus-local\REPO\profiles\REPO - Test` |
| DLL instalada | `...\REPO - Test\BepInEx\plugins\HiarlyScripter-SurveyorMap\SurveyorMap.dll` |
| Config ativa | `...\REPO - Test\BepInEx\config\com.hiarlyscripter.surveyormap.cfg` |
| LogOutput.log | `...\REPO - Test\BepInEx\LogOutput.log` |
| JSON diagnóstico | `...\REPO - Test\BepInEx\plugins\HiarlyScripter-SurveyorMap\diagnostics\surveyormap-runtime-state.json` |

---

## Build atual

| Campo | Valor |
|---|---|
| Hash curto | `1f9b9583` (2026-05-30) |
| Build DLL | `...\build\SurveyorMap.dll` |
| Instalada | `...\REPO - Test\...\SurveyorMap.dll` (hash igual) |
| Package DLL | `...\package\plugins\...\SurveyorMap.dll` (sincronizada em 2026-05-30) |

---

## Estado funcional CONFIRMADO

| Item | Status |
|---|---|
| BepInEx carrega SurveyorMap | ✅ Confirmado (BuildTag no log) |
| Plugin.Awake() roda | ✅ Confirmado |
| Plugin.OnEnable() roda | ✅ Confirmado |
| Plugin.Update() roda | ✅ Confirmado (pluginUpdateCount=6241+) |
| Plugin.OnGUI() roda | ✅ Confirmado (pluginOnGuiCount=12480+) |
| RuntimeProbe.Update() roda | ✅ Confirmado (runtimeProbeUpdateCount=6241+) |
| RuntimeProbe.OnGUI() roda | ✅ Confirmado (runtimeProbeOnGuiCount=12480+) |
| JSON runtime escrito | ✅ Confirmado |
| lastException=null | ✅ Confirmado |
| scene=DontDestroyOnLoad | ✅ Confirmado |
| hideFlags=HideAndDontSave | ✅ Confirmado |
| SurveyorMap enabled no mods.yml | ✅ Confirmado |
| TAB nativo funciona | ✅ Confirmado (observação anterior) |

---

## Estado pendente (próxima rodada)

| Item | Status |
|---|---|
| Minimap aparece no level | ⏳ Não validado ainda |
| NativeMapCapture ready no level | ⏳ Não validado |
| M detectado no level | ⏳ Não validado |
| TAB open/close no level com evidência de log | ⏳ Não validado |
| nativeTextureReady=true no level | ⏳ Não validado |

---

## Config baseline atual (aplicada 2026-05-30)

```
ForceHudProofOfLife   = false  ← diagnóstico desativado
CenterOnPlayer        = false
RevealRooms           = false
RevealMode            = Off
ShowEnemies           = false
EnemyDetectionMode    = Off
SafeMode              = true
CaptureFPS            = 5
RenderTextureSize     = 256
```

---

## Fixes que resolveram o runtime baseline

| Fix | O que resolveu |
|---|---|
| `hideFlags = HideFlags.HideAndDontSave` | Plugin destruído antes do primeiro frame — DontDestroyOnLoad sozinho não bastava |
| `Log = Logger` antes de qualquer `LogHelper.Info` | NullReferenceException em Awake() matava o plugin |
| `CultureInfo.InvariantCulture` no JSON writer | Vírgula PT-BR no número quebrava parsing do JSON |
| `mods.yml` SurveyorMap enabled=true | Plugin estava desabilitado no perfil |

---

## Checkpoint estável disponível

`releases/runtime-baseline-pass-20260530-140333`
(Core.cs + DLL + relatório PASS do runtime baseline)

---

## Proibido (sem exceção)

- Publicar no Thunderstore
- Push no GitHub
- Mexer em outros mods ou no perfil Default
- Copiar código de dig-Minimap ou clay-BetterMap
- Declarar feature funcionando sem evidência objetiva de log/JSON
