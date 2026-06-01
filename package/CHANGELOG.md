# Changelog - SurveyorMap

Este arquivo possui versao em Portugues e Ingles. A versao em Portugues vem primeiro; a English version is below.

This file includes Portuguese and English versions. Portuguese comes first; English version is below.

## Portugues

### v1.0.0 local - 2026-06-01

**Compatibilidade:** R.E.P.O. + BepInEx `5.4.2100`

- Rebuild v2 com arquitetura limpa baseada no espelho do mapa nativo.
- Minimap persistente via camera nativa `activeTexture`.
- Comportamento TAB-safe: o minimapa some enquanto o TAB nativo esta aberto.
- Toggle `M` para mostrar/esconder o minimapa.
- Marcadores de inimigos por `MapCustom`, com cleanup por registry, morte, despawn, inatividade e varredura periodica.
- Marcadores com formas e cores por dificuldade.
- Configs `EnemyMarkerSize` e `EnemyMarkerShapeMode`.
- Modo de edicao com `F8`: mover, redimensionar, ajustar zoom, `Shift + wheel`, `+ / -`, e salvar config ao sair.
- `RevealRoomsMode` substitui o booleano antigo:
  - `Vanilla` e o padrao e nao chama `RoomVolume.SetExplored()`;
  - `NativeGlobal` revela via `RoomVolume.SetExplored()` e afeta TAB + minimap;
  - `MinimapOnly` permanece bloqueado/nao implementado.
- ZIP local atualizado em `releases/HiarlyScripter-SurveyorMap-v1.0.0-local.zip`.

### v1.0.0 local - 2026-05-31

- Rebuild inicial da arquitetura v2.
- Remocao de RuntimeLoop, RuntimeProbeBehaviour, CenterOnPlayer, overlays falsos, RenderTexture proprio e proof HUD.
- Baseline de minimap nativo, M toggle, TAB-safe, RevealRooms global e ShowEnemies inicial.

## English

### v1.0.0 local - 2026-06-01

**Compatibility:** R.E.P.O. + BepInEx `5.4.2100`

- v2 rebuild with a clean native-map mirror architecture.
- Persistent minimap through the native map camera `activeTexture`.
- TAB-safe behavior: the minimap hides while the native TAB map is open.
- `M` toggle to show/hide the minimap.
- Enemy markers through `MapCustom`, with registry cleanup on death, despawn, inactivity, and periodic sweep.
- Enemy marker shapes and colors by difficulty.
- `EnemyMarkerSize` and `EnemyMarkerShapeMode` configs.
- `F8` edit mode: move, resize, adjust zoom, `Shift + wheel`, `+ / -`, and save config on exit.
- `RevealRoomsMode` replaces the old boolean:
  - `Vanilla` is the default and does not call `RoomVolume.SetExplored()`;
  - `NativeGlobal` reveals through `RoomVolume.SetExplored()` and affects TAB + minimap;
  - `MinimapOnly` remains blocked/not implemented.
- Local ZIP refreshed at `releases/HiarlyScripter-SurveyorMap-v1.0.0-local.zip`.

### v1.0.0 local - 2026-05-31

- Initial v2 architecture rebuild.
- Removed RuntimeLoop, RuntimeProbeBehaviour, CenterOnPlayer, fake overlays, owned RenderTexture, and proof HUD.
- Baseline native minimap, M toggle, TAB-safe behavior, global RevealRooms, and initial ShowEnemies.
