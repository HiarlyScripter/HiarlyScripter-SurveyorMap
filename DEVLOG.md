# DEVLOG - SurveyorMap

## 2026-05-22

- Created v1 implementation plan and scaffold.
- Implemented original client-side HUD minimap architecture.
- Added configs, marker pooling, separate player/enemy updates, SafeMode, and rate-limited logs.

## 2026-11-06 — Revalidação para o update do jogo (Steam buildid 26407089)

- Jogo atualizou em 06/11/2026 (só Assembly-CSharp mudou; Unity 6000.3.17 já vinha de 29/10).
- Build contra a DLL nova: 0 erros. Warning CS0618 corrigido (`FindObjectsOfType` -> `FindObjectsByType<RoomVolume>(FindObjectsSortMode.None)` em RevealRoomsService).
- Todas as APIs/reflexões/patches Harmony conferidos na DLL nova (existem). Semântica ainda NÃO validada in-game.
- Static audit: 44/44 (caminho da DLL instalada no audit corrigido para o layout do r2modman).
- DLL nova (md5 bfd376b6) instalada no perfil `REPO - Test`. Status: UNTESTED até aprovação in-game.
- Docs `docs/CURRENT_STATE_FOR_AGENTS.md` e afins estão desatualizados (fase de maio); fonte confiável: package/CHANGELOG.md e tools/last-*-validation.*.
