# CURRENT_RISK_REGISTER.md
# Registro de Riscos Ativos — SurveyorMap
# Atualizado em: 2026-05-30

---

## RISCO 1 — Coroutine/RuntimeLoop morre após scene load [CRÍTICO - ATIVO]

**Descrição:** O `RuntimeLoop` é uma coroutine iniciada no `Awake()` do plugin. Coroutines iniciadas em MonoBehaviours do BepInEx (que vivem em DontDestroyOnLoad) sobrevivem a trocas de cena — mas o `SurveyorMapController` adicionado via `AddComponent` é filho do mesmo GameObject e pode ser destruído ou ter sua coroutine invalidada dependendo de como o Unity gerencia a cena.

**Evidência:** Log mostra `RuntimeLoop started` no startup mas nenhuma linha do SurveyorMap depois de `Changed level to: Level - Museum`.

**Mitigação planejada:**
- Verificar se o plugin GameObject usa `DontDestroyOnLoad`
- Garantir que `EnsureControllerAlive` recria controller se ele foi destruído
- Adicionar `Update()` no plugin como fallback que chama `EnsureControllerAlive()` e `RuntimeUpdate()` independente da coroutine
- Logar quando coroutine for reiniciada

---

## RISCO 2 — Gate nunca retorna true após scene load [CRÍTICO - ATIVO]

**Descrição:** `IsHudGateOpenForCapture` pode depender de APIs (`RunIsLevel`, `PlayerAvatarLocal`, `levelObjects`) que só ficam disponíveis depois de um delay após o load. Se o gate for checado muito cedo e falhar, e não for rechecado com frequência suficiente, o HUD nunca aparece.

**Evidência:** Os warnings de startup (`RunIsLobby unavailable`, `RunIsLevel unavailable`) indicam que as APIs demoram para inicializar. O gate pode estar bloqueando por causa disso e nunca reavaliar.

**Mitigação planejada:**
- Garantir que `RuntimeUpdateCore` roda a cada frame mesmo quando game API está indisponível
- Adicionar log de gate-block com motivo exato (qual condição específica falhou)
- Rate-limit os warnings de "unavailable" (rebaixar para Debug no startup)

---

## RISCO 3 — Input M não sendo processado [ATIVO]

**Descrição:** O toggle key `M` é checado no `RuntimeLoop`. Se o RuntimeLoop morreu, o input nunca é processado.

**Dependência:** Este risco é consequência direta do Risco 1. Corrigir o RuntimeLoop provavelmente resolve este também.

**Mitigação planejada:** Adicionar verificação de input no `Update()` do plugin como fallback.

---

## RISCO 4 — TAB pode voltar a quebrar [ATIVO - MONITORAR]

**Descrição:** A correção do TAB depende de `IsNativeMapTabOpen()` via reflexão em `MapToolController.instance.Active`. Qualquer mudança na câmera (`camera.targetTexture`, `camera.enabled`) sem restauração adequada pode causar regressão.

**Regras de segurança:**
- `RenderCameraOnce` deve sempre restaurar `targetTexture` e `enabled` no `finally`
- `ReleaseOwnedRenderTexture` deve usar `FindObjectsOfType<Camera>(true)` (não `Camera.allCameras`)
- Nunca chamar `EnsureNativeMapReadyForCapture` ou `HideNativeVisuals` quando TAB está aberto
- Não mexer na lógica de TAB sem criar checkpoint antes

---

## RISCO 5 — Warnings de startup próprios do SurveyorMap [BAIXO - COSMÉTICO]

**Descrição:** Os warnings `RunIsLobby unavailable`, `RunIsShop unavailable`, `MenuLevel unavailable`, `RunIsLevel unavailable`, `LevelGenDone unavailable` aparecem no startup porque as APIs do jogo não estão prontas naquele momento.

**Impacto:** Não causam bug funcional. São apenas ruído no log que pode dificultar o diagnóstico de erros reais.

**Mitigação planejada:** Rebaixar para `LogHelper.Debug()` ou usar `RateLimitedWarning` com chave por tipo.

---

## RISCO 6 — Warnings de saves/mods ausentes no perfil REPO - Test [EXTERNO - NÃO CORRIGIR]

**Descrição:** O perfil REPO - Test é limpo e não tem todos os mods/saves do perfil Default. Warnings como:
- `dictionaryOfDictionaries does not contain key 'playerUpgradeManaRegeneration'`
- `Item 'Gun Hunter' not found in the itemDictionary`

vêm de saves criados com outros mods que não estão presentes.

**Ação:** Ignorar. Não tentar corrigir dentro do SurveyorMap. São warnings do jogo/outros mods.

---

## RISCO 7 — Map Expand ou outros mods como causa falsa [ATENÇÃO]

**Descrição:** Sem evidência no log, não atribuir falhas do SurveyorMap a outros mods do perfil REPO - Test (QuickPocket, REPOConfig, NoSaveDelete, MenuLib).

**Regra:** Qualquer atribuição de causa deve ter evidência no log ou no comportamento observável.

---

## RISCO 8 — Features desligadas não significam produto final [ESTRATÉGICO]

**Descrição:** O estado atual com `RevealRooms=false`, `ShowEnemies=false`, `CenterOnPlayer=false` é temporário para estabilização do baseline. Não transformar esse estado em padrão permanente nem em produto publicável.

**Ordem de reativação:** baseline funcional → CenterOnPlayer → RevealRooms → ShowEnemies → visual premium → publicação.
