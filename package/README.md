# SurveyorMap

Este arquivo possui versao em Portugues e Ingles. A versao em Portugues vem primeiro; a English version is below.

This file includes Portuguese and English versions. Portuguese comes first; English version is below.

## Portugues

SurveyorMap adiciona um minimapa persistente e configuravel ao HUD do R.E.P.O. durante a gameplay. Ele espelha a camera nativa do mapa do jogo (`activeTexture`), mantendo geometria, cores e marcadores nativos sempre que possivel.

### Principais recursos

- Minimap persistente no canto inferior esquerdo durante gameplay.
- Visual nativo do mapa, sem overlay falso como base principal.
- TAB-safe: ao abrir o mapa nativo com TAB, o minimapa do HUD some automaticamente.
- Toggle com `M` para ligar/desligar o minimapa.
- Marcadores de inimigos via `MapCustom`, com formas e cores por dificuldade.
- Cleanup de marcadores em morte, despawn, inatividade e varredura periodica.
- Modo de edicao in-game com `F8`.
- `RevealRoomsMode` seguro por padrao: `Vanilla` nao chama `RoomVolume.SetExplored()`.

### Instalacao

Instale o pacote no perfil desejado do r2modman. Para teste local, o DLL fica em:

`BepInEx/plugins/HiarlyScripter-SurveyorMap/SurveyorMap.dll`

Este pacote local ainda deve ser tratado como build de teste ate validacao final em gameplay e LogOutput fresco.

### Configuracao

O arquivo de config fica em:

`BepInEx/config/com.hiarlyscripter.surveyormap.cfg`

| Secao | Chave | Padrao | Efeito |
|---|---:|---:|---|
| Minimap | EnableMinimap | true | Ativa/desativa o HUD do minimapa. |
| Minimap | ToggleKey | M | Tecla para mostrar/esconder o minimapa. |
| Minimap | Width | 260 | Largura do minimapa em pixels. |
| Minimap | Height | 260 | Altura do minimapa em pixels. |
| Minimap | PosX | 24 | Distancia da borda esquerda. |
| Minimap | PosY | 120 | Distancia da borda inferior. |
| Minimap | Opacity | 0.85 | Opacidade do minimapa. |
| Minimap | Zoom | 2.25 | Zoom/orthographic size da camera nativa enquanto o minimapa esta visivel. |
| Features | RevealRoomsMode | Vanilla | `Vanilla` preserva TAB/minimap vanilla. `NativeGlobal` revela via `SetExplored()` e afeta TAB + minimap. |
| Features | ShowEnemies | true | Mostra marcadores de inimigos no mapa. |
| Features | EnemyMarkerSize | 1.0 | Multiplicador de tamanho dos marcadores. |
| Features | EnemyMarkerShapeMode | DifficultyShape | `DifficultyShape` usa forma/cor por dificuldade. `Circle` usa circulos coloridos. |
| EditMode | EditModeEnabled | true | Ativa o modo de edicao in-game. |
| EditMode | EditModeKey | F8 | Tecla para entrar/sair do modo de edicao. |

### Controles

- `M`: mostra/esconde o minimapa.
- `F8`: entra/sai do modo de edicao.
- Arrastar o minimapa no edit mode: move a posicao.
- Arrastar o canto inferior direito no edit mode: redimensiona.
- Mouse wheel no edit mode: ajusta o zoom.
- `Shift + mouse wheel` no edit mode: redimensiona.
- `+ / -` no edit mode: aumenta/diminui o tamanho.

### RevealRoomsMode

- `Vanilla` (padrao): nao chama `RoomVolume.SetExplored()`. TAB e minimap seguem o comportamento vanilla, com salas nao exploradas pretas/com `?`.
- `NativeGlobal`: chama `RoomVolume.SetExplored()` e revela salas no minimap e no TAB nativo. E intencionalmente global.
- `MinimapOnly`: nao implementado/bloqueado. Nao ha caminho seguro confirmado para revelar apenas o minimap sem mutar o estado nativo das salas.

### Marcadores de inimigos

- Marcadores sao gerados por sprites procedurais proprios.
- Em `DifficultyShape`, dificuldade 1 usa circulo verde, dificuldade 2 usa losango amarelo, dificuldade 3 usa triangulo laranja, e categorias especiais/fallback podem usar estrela vermelha.
- `EnemyMarkerSize` controla o tamanho.
- O cleanup remove marcadores em despawn, morte, inatividade e por varredura periodica.
- A transparencia por andar diferente fica a cargo do sistema nativo `MapCustomEntity`.

### Multiplayer

SurveyorMap e client-side/local. Somente o jogador que quer o HUD precisa instalar. O mod nao envia RPCs proprios e nao exige instalacao por outros jogadores.

### Limitacoes conhecidas

- `MinimapOnly` para RevealRooms nao esta implementado.
- `NativeGlobal` altera o estado nativo do mapa e tambem revela salas no TAB.
- O modo de edicao deve ser usado com cuidado durante gameplay; ele foi feito para ajuste local do HUD.
- Esta build local nao deve ser considerada publicada ate a validacao final e revisao do pacote.

## English

SurveyorMap adds a persistent configurable HUD minimap to R.E.P.O. gameplay. It mirrors the game's native map camera (`activeTexture`), preserving native geometry, colors, and markers whenever possible.

### Main features

- Persistent bottom-left minimap during gameplay.
- Native map look, with no fake overlay as the primary map.
- TAB-safe: when the native TAB map opens, the HUD minimap hides automatically.
- `M` toggle for showing/hiding the minimap.
- Enemy markers through native `MapCustom`, with shape and color by difficulty.
- Marker cleanup on death, despawn, inactivity, and periodic sweep.
- In-game edit mode with `F8`.
- Safe default `RevealRoomsMode`: `Vanilla` does not call `RoomVolume.SetExplored()`.

### Installation

Install the package into the desired r2modman profile. For local testing, the DLL is placed at:

`BepInEx/plugins/HiarlyScripter-SurveyorMap/SurveyorMap.dll`

This local package should still be treated as a test build until final gameplay validation and a fresh LogOutput are confirmed.

### Configuration

The config file is stored at:

`BepInEx/config/com.hiarlyscripter.surveyormap.cfg`

| Section | Key | Default | Effect |
|---|---:|---:|---|
| Minimap | EnableMinimap | true | Enables/disables the minimap HUD. |
| Minimap | ToggleKey | M | Key to show/hide the minimap. |
| Minimap | Width | 260 | Minimap width in pixels. |
| Minimap | Height | 260 | Minimap height in pixels. |
| Minimap | PosX | 24 | Distance from the left edge. |
| Minimap | PosY | 120 | Distance from the bottom edge. |
| Minimap | Opacity | 0.85 | Minimap opacity. |
| Minimap | Zoom | 2.25 | Native map camera zoom/orthographic size while the minimap is visible. |
| Features | RevealRoomsMode | Vanilla | `Vanilla` preserves vanilla TAB/minimap behavior. `NativeGlobal` reveals through `SetExplored()` and affects TAB + minimap. |
| Features | ShowEnemies | true | Shows enemy markers on the map. |
| Features | EnemyMarkerSize | 1.0 | Enemy marker size multiplier. |
| Features | EnemyMarkerShapeMode | DifficultyShape | `DifficultyShape` uses shape/color by difficulty. `Circle` uses colored circles. |
| EditMode | EditModeEnabled | true | Enables the in-game edit mode. |
| EditMode | EditModeKey | F8 | Key to enter/exit edit mode. |

### Controls

- `M`: show/hide the minimap.
- `F8`: enter/exit edit mode.
- Drag the minimap in edit mode: move position.
- Drag the bottom-right corner in edit mode: resize.
- Mouse wheel in edit mode: adjust zoom.
- `Shift + mouse wheel` in edit mode: resize.
- `+ / -` in edit mode: increase/decrease size.

### RevealRoomsMode

- `Vanilla` (default): does not call `RoomVolume.SetExplored()`. TAB and minimap follow vanilla behavior, with unexplored rooms black/marked with `?`.
- `NativeGlobal`: calls `RoomVolume.SetExplored()` and reveals rooms on both minimap and native TAB. This is intentionally global.
- `MinimapOnly`: not implemented/blocked. No safe confirmed path exists to reveal only the minimap without mutating native room state.

### Enemy markers

- Markers are generated from original procedural sprites.
- In `DifficultyShape`, difficulty 1 uses a green circle, difficulty 2 a yellow diamond, difficulty 3 an orange triangle, and special/fallback categories may use a red star.
- `EnemyMarkerSize` controls size.
- Cleanup removes markers on despawn, death, inactivity, and periodic sweep.
- Cross-floor transparency is handled by the native `MapCustomEntity` system.

### Multiplayer

SurveyorMap is client-side/local. Only the player who wants the HUD needs to install it. The mod does not send custom RPCs and does not require other players to install it.

### Known limitations

- `MinimapOnly` RevealRooms is not implemented.
- `NativeGlobal` changes native map state and also reveals rooms on TAB.
- Edit mode should be used carefully during gameplay; it is intended for local HUD adjustment.
- This local build should not be considered published until final validation and package review.
