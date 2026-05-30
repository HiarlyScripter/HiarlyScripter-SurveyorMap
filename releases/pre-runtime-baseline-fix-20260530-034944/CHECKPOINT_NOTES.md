# Checkpoint: pre-runtime-baseline-fix
# Data: 2026-05-30 03:49
# Motivo: antes de corrigir causa raiz do minimap não aparecer no level

## Estado pré-fix
- Core.cs hash (MD5): A5CF5F5E1934477AB76B82B65655A37E
- Problema: IsGameplayState() retorna false → PlayerApiReady=false → Allowed=false → HUD nunca abre
- TAB nativo: funcionando

## Causa raiz identificada
- GetRunState() linha ~3674: bool gameplayState = IsGameplayState()
- IsGameplayState() checa GameDirector.currentState == 2
- Se esse valor é errado para a versão atual do jogo, o gate fecha permanentemente
- playerApiReady = false → Allowed = false → HUD = invisible

## Fix planejado
- Remover gameplayState do cálculo de PlayerApiReady
- As checagens RunIsLevel, !InLobby, !InShop, !MenuLevel já garantem contexto de level
