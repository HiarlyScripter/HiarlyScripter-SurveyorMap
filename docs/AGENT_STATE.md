# SurveyorMap - Agent State

## Status: RELEASE CANDIDATE APPROVED - v1.0.0

Data de aprovacao: 2026-06-03

---

## Build final aprovada

| Item | Valor |
|---|---|
| Hash (md5 primeiros 8) | 468672de |
| Commit | b77f4a9 |
| Tag local | v1.0.0-release-candidate |
| Branch | codex-exec |
| Build: 0 erros / 0 warnings | CONFIRMADO |

---

## Auditoria Codex - resultado final

- Auditoria APROVADO 45/45
- Log release-clean com DebugLogging=false: uma unica linha
- Camera freeze F8 validada (usuario confirmou)
- ShowEnemiesInUnexploredRooms: removida de codigo, config, docs
- Risco de plagio/originalidade: BAIXO (implementacao clean-room)

---

## Package / ZIP

Estrutura final do ZIP:
  manifest.json
  README.md
  CHANGELOG.md
  icon.png
  LICENSE
  plugins/HiarlyScripter-SurveyorMap/SurveyorMap.dll

ZIP path: releases/HiarlyScripter-SurveyorMap-v1.0.0-local.zip
DLL interna: 468672de

---

## Funcionalidades v1.0.0

- Minimapa persistente nativo (camera activeTexture)
- TAB-safe (some ao abrir TAB)
- M toggle
- F8 edit mode: drag, resize, scroll=tamanho, +/-=zoom, R=reset
- EnableZoomHotkeysOutsideEdit: +/- zoom fora do F8
- FreezeCameraInEditMode: InputManager.DisableAiming() por frame + CameraAimEditModePatch
- UnlockCursorInEditMode: cursor desbloqueado no F8
- Marcadores de inimigos com shape+cor por tier de ameaca
- RevealRoomsMode: Vanilla (padrao) / NativeGlobal (opt-in)
- DebugLogging: false por padrao (log limpo em release)

---

## Proxima etapa

Publicacao no Thunderstore / GitHub Release somente com autorizacao explicita do usuario.
Nao publicar, nao fazer push, nao mexer no Default profile sem instrucao direta.

---

## Pastas

Canonica:
  C:\Users\Hiarly\.claude\PROJETOS\REPO\HiarlyScripter-SurveyorMap

Codex arquivada (historico apenas):
  C:\Users\Hiarly\.codex\PROJETOS\REPO\HiarlyScripter-SurveyorMap-Exec_DISABLED_*