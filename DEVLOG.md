# DEVLOG - SurveyorMap

## 2026-05-22

- Created v1 implementation plan and scaffold.
- Implemented original client-side HUD minimap architecture.
- Added configs, marker pooling, separate player/enemy updates, SafeMode, and rate-limited logs.

## 2026-11-06 — Revalidação para o update do jogo (Steam buildid 26407089)

- Jogo atualizou em 06/11/2026 (só Assembly-CSharp mudou; Unity 6000.3.17 já vinha de 29/10).
- Build contra a DLL nova: 0 erros. Warning CS0618 corrigido (`FindObjectsOfType` -> `FindObjectsByType<RoomVolume>(FindObjectsSortMode.None)` em RevealRoomsService).
- Todas as APIs/reflexões/patches Harmony conferidos na DLL nova (existem). Semântica ainda NÃO validada in-game.
- Static audit: 45/45 após correção (caminho da DLL instalada no audit corrigido para o layout do r2modman).
- DLL nova (md5 bfd376b6) instalada no perfil `REPO - Test`. Status: UNTESTED até aprovação in-game.
- Docs `docs/CURRENT_STATE_FOR_AGENTS.md` e afins estão desatualizados (fase de maio); fonte confiável: package/CHANGELOG.md e tools/last-*-validation.*.

## 2026-11-06 — v1.0.2

- Aprovado in-game por Hiarly (DLL md5 bfd376b6, código idêntico exceto PluginVersion). Bump para 1.0.2: manifest, thunderstore.toml, `PluginVersion`, README (badge build 26407089), CHANGELOG PT/EN, audit e runtime validator.
- Build final md5 c9acee77 (só difere pela string de versão). ZIP: dist/HiarlyScripter-SurveyorMap-1.0.2.zip. Cópia para o perfil Test falhou (DLL em uso pelo jogo); reinstalar com o jogo fechado.

- 2026-11-06: v1.0.2 publicada no Thunderstore (https://thunderstore.io/package/download/HiarlyScripter/SurveyorMap/1.0.2/). Nexus/Discord: não publicados.
