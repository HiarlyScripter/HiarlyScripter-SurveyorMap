# NEXT_PROMPT_AFTER_COMPACT.md
# Prompt pronto para colar logo após o /compact

---

Cole este prompt diretamente após executar /compact:

---

/repo-mod

Use o padrão Arquiteto Sênior — Execução Controlada.

Leia primeiro o arquivo de resumo de continuidade:
C:\Users\Hiarly\.claude\PROJETOS\REPO\HiarlyScripter-SurveyorMap\releases\pre-compact-runtime-baseline-state-20260530-032902\COMPACT_RESUME_SURVEYORMAP.md

Depois leia também:
C:\Users\Hiarly\.claude\PROJETOS\REPO\HiarlyScripter-SurveyorMap\releases\pre-compact-runtime-baseline-state-20260530-032902\CURRENT_PATHS_AND_COMMANDS.md

Contexto resumido (já está no arquivo acima, mas repito para orientação rápida):

Projeto ativo: C:\Users\Hiarly\.claude\PROJETOS\REPO\HiarlyScripter-SurveyorMap
Perfil de teste: C:\Users\Hiarly\AppData\Roaming\r2modmanPlus-local\REPO\profiles\REPO - Test

Estado atual:
- TAB nativo funciona. Não quebra mais.
- SurveyorMap carrega, HUD criado, RuntimeLoop scheduled.
- MAS: minimap NÃO aparece no level. M não responde.
- Log para após "Changed level to: Level - Museum" sem nenhum output do SurveyorMap.

Problema a corrigir:
Bug de lifecycle/runtime — o SurveyorMap não volta a processar após scene load.
Provavelmente: coroutine morreu na troca de cena, ou gate nunca retorna true após o load.

Modo desta rodada: HOTFIX CIRÚRGICO — corrigir lifecycle/runtime/gate.

Escopo permitido:
- Corrigir RuntimeLoop (garantir que sobrevive a scene loads)
- Corrigir EnsureControllerAlive (recriar controller/HUD se destruído)
- Corrigir gate (IsHudGateOpenForCapture deve retornar true em level)
- Rebaixar warnings de startup para Debug/rate-limited
- Compilar e instalar no perfil REPO - Test
- Verificar hash

Fora de escopo:
- Não reativar CenterOnPlayer
- Não reativar RevealRooms
- Não reativar ShowEnemies
- Não implementar features novas
- Não mexer em outros mods
- Não mexer no perfil Default
- Não publicar

Critério de sucesso:
- Log mostra RunState=Level depois do load
- Log mostra HUD visible=True
- Log mostra NativeMapCapture: ready=True
- M liga/desliga o minimap
- TAB continua funcionando
- 0 erros, 0 warnings no build
