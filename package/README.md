# SurveyorMap

---

## Português

O SurveyorMap adiciona um minimapa nativo persistente no HUD inferior esquerdo durante o gameplay do R.E.P.O. Ele espelha a saída real da câmera de mapa do jogo — mesma geometria, mesmas cores, mesma fidelidade que o TAB — sem overlays falsos.

### Funcionalidades

- **Minimapa persistente** — HUD inferior esquerdo, visível apenas durante o gameplay. Some nos menus e lobby.
- **Fidelidade nativa** — lê a câmera de mapa real do jogo (`activeTexture`). Mostra a geometria real das salas, marcadores nativos e fidelidade das paredes.
- **Tecla M** — pressione `M` para mostrar/ocultar o minimapa sem afetar o TAB nativo.
- **TAB seguro** — ao abrir o TAB, o minimapa persistente some automaticamente. O TAB não é afetado.
- **Marcadores de inimigos** — mostra posições de inimigos como marcadores no mapa. Inimigos mortos/despawnados são removidos automaticamente em até ~2s. A **cor** indica o nível de perigo (dificuldade) e a **forma geométrica** indica o tipo/família do inimigo.
- **ShowEnemiesInUnexploredRooms** — `false` por padrão: inimigos em salas não exploradas ficam ocultos (mais vanilla). `true`: todos os marcadores aparecem independente da exploração.
- **Modo de edição F8** — pressione `F8` para entrar no modo de edição: arraste para reposicionar, segure o canto inferior direito para redimensionar, scroll para zoom, `+`/`-` para ajustar tamanho, `Shift+scroll` para redimensionar, `R` para resetar para os valores padrão. Config salva automaticamente ao sair.
- **RevealRooms (opt-in)** — defina `RevealRoomsMode = NativeGlobal` para revelar todas as salas ao entrar no nível. **Atenção:** este modo também afeta o TAB nativo. O padrão é `Vanilla` (TAB permanece original).

### Quem precisa instalar

Apenas o jogador que quer o HUD. O SurveyorMap não envia RPCs, não altera a rede e não requer instalação pelos outros jogadores.

### Paleta de cores dos marcadores de inimigos

A **cor** representa o nível de perigo/dificuldade:

| Dificuldade | Cor | Hex |
|---|---|---|
| Easy (nível 1) | Verde-gelo quase branco | `#DFFFE8` |
| Medium (nível 2) | Azul / ciano | `#3DA5FF` |
| Hard (nível 3) | Roxo / violeta | `#9B5CFF` |
| Elite / Boss (nível 4+) | Vermelho / coral | `#FF3B30` |

A **forma** representa o tipo/família do inimigo:

| Família | Forma | Exemplos de palavras-chave |
|---|---|---|
| Comum / básico | Círculo | fallback padrão |
| Caçador / agressivo / melee | Triângulo | hunt, rush, bang, attack |
| Especial / suporte / estranho | Losango | shadow, ghost, support, eye |
| Boss / elite / extremo | Estrela | boss, elite, giant, king |

### Configuração

| Seção | Chave | Padrão | Efeito |
|---|---|---|---|
| Minimap | EnableMinimap | true | Liga/desliga o HUD do minimapa. |
| Minimap | ToggleKey | M | Tecla para alternar o minimapa. |
| Minimap | Width | 260 | Largura do minimapa em pixels. |
| Minimap | Height | 260 | Altura do minimapa em pixels. |
| Minimap | PosX | 24 | Offset horizontal a partir da borda esquerda. |
| Minimap | PosY | 120 | Offset vertical a partir da borda inferior. |
| Minimap | Opacity | 0.85 | Opacidade (0 = invisível, 1 = opaco). |
| Minimap | Zoom | 2.25 | Fator de zoom ortográfico da câmera de mapa. |
| Features | RevealRoomsMode | Vanilla | Vanilla = padrão (TAB original). NativeGlobal = revela via SetExplored, também afeta TAB. |
| Features | ShowEnemies | true | Mostrar marcadores de inimigos no mapa. |
| Features | EnemyMarkerSize | 1.0 | Multiplicador de escala dos marcadores (0.1–3.0). |
| Features | ShowEnemiesInUnexploredRooms | false | false = ocultar inimigos em salas não exploradas. true = mostrar todos. |
| EditMode | EditModeEnabled | true | Liga o modo de edição in-game (F8). |
| EditMode | EditModeKey | F8 | Tecla para entrar/sair do modo de edição. |

### Notas

- O minimapa é puramente client-side e não afeta outros jogadores.
- `RevealRoomsMode = NativeGlobal` chama `RoomVolume.SetExplored()`, o que também revela salas no TAB nativo — comportamento opt-in documentado, não é o padrão.
- RevealRooms apenas no minimapa (sem afetar o TAB): **BLOQUEADO** — nenhuma implementação segura encontrada sem mutar `RoomVolume.Explored`.

---

## English

SurveyorMap adds a persistent native-looking minimap to the bottom-left HUD during R.E.P.O. gameplay. It mirrors the real native map camera output — the same geometry, colors, and fidelity as the TAB map — no fake overlays.

### Features

- **Persistent minimap** — bottom-left HUD, visible only during gameplay. Disappears in menus and lobby.
- **Native map fidelity** — reads the game's own map camera (`activeTexture`). Shows real room geometry, native markers, and wall fidelity.
- **M toggle** — press `M` to show/hide the minimap without affecting the native TAB map.
- **TAB-safe** — when you open the TAB map, the persistent minimap hides automatically. TAB is unaffected.
- **Enemy markers** — shows enemy positions on the map. Dead/despawned enemies are cleaned up automatically within ~2s. **Colour** indicates the danger level (difficulty), **shape** indicates enemy type/family.
- **ShowEnemiesInUnexploredRooms** — `false` by default: enemies in unexplored rooms are hidden (more vanilla). `true`: all markers visible regardless of exploration.
- **F8 Edit Mode** — press `F8` to enter edit mode: drag to reposition, grab the bottom-right corner to resize, scroll to zoom, `+`/`-` to adjust size, `Shift+scroll` to resize, `R` to reset to defaults. Config is saved automatically on exit.
- **RevealRooms (opt-in)** — set `RevealRoomsMode = NativeGlobal` to reveal all rooms on level load. **Note:** this also affects the native TAB map. Default is `Vanilla` (TAB stays original).

### Who needs to install it

Only the player who wants the HUD. SurveyorMap does not send RPCs, does not alter networking, and does not require other players to install it.

### Enemy marker colour palette

**Colour** = danger level / difficulty:

| Difficulty | Colour | Hex |
|---|---|---|
| Easy (level 1) | Ice-white green | `#DFFFE8` |
| Medium (level 2) | Blue / cyan | `#3DA5FF` |
| Hard (level 3) | Purple / violet | `#9B5CFF` |
| Elite / Boss (level 4+) | Red / coral | `#FF3B30` |

**Shape** = enemy type / family:

| Family | Shape | Keyword examples |
|---|---|---|
| Common / basic | Circle | default fallback |
| Hunter / aggressive / melee | Triangle | hunt, rush, bang, attack |
| Special / support / strange | Diamond | shadow, ghost, support, eye |
| Boss / elite / extreme | Star | boss, elite, giant, king |

### Configuration

| Section | Key | Default | Effect |
|---|---|---|---|
| Minimap | EnableMinimap | true | Enable/disable the minimap HUD. |
| Minimap | ToggleKey | M | Key to toggle the minimap. |
| Minimap | Width | 260 | Minimap width in pixels. |
| Minimap | Height | 260 | Minimap height in pixels. |
| Minimap | PosX | 24 | Horizontal offset from the left edge. |
| Minimap | PosY | 120 | Vertical offset from the bottom edge. |
| Minimap | Opacity | 0.85 | Minimap opacity (0 = invisible, 1 = opaque). |
| Minimap | Zoom | 2.25 | Native map camera orthographic zoom. |
| Features | RevealRoomsMode | Vanilla | Vanilla = default (no SetExplored, TAB stays original). NativeGlobal = reveals via SetExplored, also affects TAB. |
| Features | ShowEnemies | true | Show enemy markers on the map. |
| Features | EnemyMarkerSize | 1.0 | Scale multiplier for enemy markers (0.1–3.0). |
| Features | ShowEnemiesInUnexploredRooms | false | false = hide markers in unexplored rooms. true = show all. |
| EditMode | EditModeEnabled | true | Enable in-game edit mode (F8). |
| EditMode | EditModeKey | F8 | Key to enter/exit edit mode. |

### Notes

- The minimap is purely client-side and does not affect other players.
- `RevealRoomsMode = NativeGlobal` calls `RoomVolume.SetExplored()` which also reveals rooms in the native TAB map — intentional opt-in, not the default.
- MinimapOnly reveal (TAB stays vanilla): **BLOCKED** — no safe implementation found without mutating `RoomVolume.Explored`.
