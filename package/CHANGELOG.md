# Changelog - SurveyorMap

Este arquivo possui versao em Portugues e Ingles. A versao em Portugues vem primeiro; a English version is below.

This file includes Portuguese and English versions. Portuguese comes first; English version is below.

## Portugues

### v1.0.0 local - 2026-06-01 (patch 3 — UX inimigos + reset editmode)

**Compatibilidade:** R.E.P.O. + BepInEx `5.4.2100`

- **Cor separada da forma**: cor = nivel de perigo/dificuldade; forma = tipo/familia do inimigo.
- Nova paleta de cores por dificuldade: Easy = verde-gelo `#DFFFE8`; Medium = azul/ciano `#3DA5FF`; Hard = roxo/violeta `#9B5CFF`; Elite/Boss = vermelho/coral `#FF3B30`. Removido amarelo/dourado como cor principal.
- Formas por tipo/nome/prefab: Circulo (comum/basico), Triangulo (cacador/agressivo), Losango (especial/suporte), Estrela (boss/elite).
- Filtro pre-spawn em `AddMarker`: inimigos mortos, inativos, despawnados ou com `CurrentState=Despawn` sao recusados antes de criar marcador.
- Nova config `ShowEnemiesInUnexploredRooms = false`: oculta marcadores em salas nao exploradas por padrao. `true` = mostrar todos. Fail-safe: mostrar se a sala nao puder ser determinada.
- Tecla `R` no modo de edicao reseta para os valores padrao (PosX=24, PosY=120, W=260, H=260, Zoom=2.25, Opacity=0.85). Funciona apenas com F8 ativo.
- Overlay do modo edicao atualizado com `[R=reset]`.

### v1.0.0 local - 2026-06-01 (patch 2 — marcadores e edit mode)

**Compatibilidade:** R.E.P.O. + BepInEx `5.4.2100`

- Cleanup de marcadores por registry: sweep a cada 2s, ClearAll no carregamento do nivel, patch manual em `EnemyHealth.DeathRPC`/`DeathImpulseRPC`.
- Marcadores com formas (circulo/losango/triangulo/estrela) e cores por dificuldade.
- Configs `EnemyMarkerSize` e `EnemyMarkerShapeMode`.
- Modo de edicao F8: mover, redimensionar pelo canto, zoom com scroll, `Shift+wheel`, `+/-`, salvar ao sair.
- `RevealRoomsMode = Vanilla` como padrao seguro. `NativeGlobal` e opt-in e tambem afeta o TAB.
- `autoAdd = false` antes de `Map.Instance.AddCustom` para evitar duplicatas.

### v1.0.0 local - 2026-06-01

**Compatibilidade:** R.E.P.O. + BepInEx `5.4.2100`

- Rebuild v2 com arquitetura limpa baseada no espelho do mapa nativo.
- Minimap persistente via camera nativa `activeTexture`.
- Comportamento TAB-safe: o minimapa some enquanto o TAB nativo esta aberto.
- Toggle `M` para mostrar/esconder o minimapa.
- Marcadores de inimigos por `MapCustom`, com cleanup por despawn.
- `RevealRoomsMode` substitui o booleano antigo: `Vanilla` (padrao, sem SetExplored), `NativeGlobal` (afeta TAB), `MinimapOnly` BLOQUEADO.

### v1.0.0 local - 2026-05-31

- Rebuild inicial da arquitetura v2.
- Remocao de RuntimeLoop, RuntimeProbeBehaviour, CenterOnPlayer, overlays falsos, RenderTexture proprio e proof HUD.
- Baseline de minimap nativo, M toggle, TAB-safe, RevealRooms global e ShowEnemies inicial.

## English

### v1.0.0 local - 2026-06-01 (patch 3 — enemy UX + edit mode reset)

**Compatibility:** R.E.P.O. + BepInEx `5.4.2100`

- **Colour separated from shape**: colour = danger/difficulty level; shape = enemy type/family.
- New colour palette by difficulty: Easy = ice-white `#DFFFE8`; Medium = blue/cyan `#3DA5FF`; Hard = purple/violet `#9B5CFF`; Elite/Boss = red/coral `#FF3B30`. Removed yellow/gold as primary marker colour.
- Shapes by enemy type/name/prefab: Circle (common/basic), Triangle (hunter/aggressive), Diamond (special/support), Star (boss/elite).
- Pre-spawn filter in `AddMarker`: dead, inactive, despawned or `CurrentState=Despawn` enemies are rejected before creating a marker.
- New config `ShowEnemiesInUnexploredRooms = false`: hides markers in unexplored rooms by default. `true` = show all. Fail-safe: show if room cannot be determined.
- `R` key in edit mode resets to default values (PosX=24, PosY=120, W=260, H=260, Zoom=2.25, Opacity=0.85). Only works when F8 edit mode is active.
- Edit mode overlay updated to include `[R=reset]`.

### v1.0.0 local - 2026-06-01 (patch 2 — markers and edit mode)

**Compatibility:** R.E.P.O. + BepInEx `5.4.2100`

- Registry-based marker cleanup: 2s sweep, ClearAll on level load, manual patch of `EnemyHealth.DeathRPC`/`DeathImpulseRPC`.
- Marker shapes (circle/diamond/triangle/star) and colours by difficulty.
- `EnemyMarkerSize` and `EnemyMarkerShapeMode` configs.
- F8 edit mode: move, resize from corner, scroll zoom, `Shift+wheel`, `+/-`, save on exit.
- `RevealRoomsMode = Vanilla` as safe default. `NativeGlobal` is opt-in and also affects TAB.
- `autoAdd = false` before `Map.Instance.AddCustom` to prevent duplicates.

### v1.0.0 local - 2026-06-01

**Compatibility:** R.E.P.O. + BepInEx `5.4.2100`

- v2 rebuild with a clean native-map mirror architecture.
- Persistent minimap through the native map camera `activeTexture`.
- TAB-safe behavior: the minimap hides while the native TAB map is open.
- `M` toggle to show/hide the minimap.
- Enemy markers through `MapCustom`, with cleanup on despawn.
- `RevealRoomsMode` replaces the old boolean: `Vanilla` (default, no SetExplored), `NativeGlobal` (also affects TAB), `MinimapOnly` BLOCKED.

### v1.0.0 local - 2026-05-31

- Initial v2 architecture rebuild.
- Removed RuntimeLoop, RuntimeProbeBehaviour, CenterOnPlayer, fake overlays, owned RenderTexture, and proof HUD.
- Baseline native minimap, M toggle, TAB-safe behavior, global RevealRooms, and initial ShowEnemies.
