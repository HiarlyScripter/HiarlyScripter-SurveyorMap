# SurveyorMap

---

## Português

O SurveyorMap adiciona um minimapa nativo persistente no HUD inferior esquerdo durante o gameplay do R.E.P.O. Ele espelha a saída real da câmera de mapa do jogo — mesma geometria, mesmas cores, mesma fidelidade que o TAB — sem overlays falsos.

### Funcionalidades

- **Minimapa persistente** — HUD inferior esquerdo, visível apenas durante o gameplay. Some nos menus e lobby.
- **Fidelidade nativa** — lê a câmera de mapa real do jogo (`activeTexture`). Mostra a geometria real das salas, marcadores nativos e fidelidade das paredes.
- **Tecla M** — pressione `M` para mostrar/ocultar o minimapa sem afetar o TAB nativo.
- **TAB seguro** — ao abrir o TAB, o minimapa persistente some automaticamente. O TAB não é afetado.
- **Marcadores de inimigos** — mostra posições de inimigos como marcadores no mapa. Inimigos mortos/despawnados são removidos automaticamente em até ~2s. A **forma** indica o nível de ameaça e a **cor** indica o tipo/família do inimigo.
- **Modo de edição F8** — pressione `F8` para entrar no modo de edição: arraste para reposicionar, segure o canto inferior direito para redimensionar, scroll para zoom, `+`/`-` para ajustar tamanho, `Shift+scroll` para redimensionar, `R` para resetar para os valores padrão. Config salva automaticamente ao sair.
- **RevealRooms (opt-in)** — defina `RevealRoomsMode = NativeGlobal` para revelar todas as salas ao entrar no nível. **Atenção:** este modo também afeta o TAB nativo. O padrão é `Vanilla` (TAB permanece original).

### Quem precisa instalar

Apenas o jogador que quer o HUD. O SurveyorMap não envia RPCs, não altera a rede e não requer instalação pelos outros jogadores.

### Sistema visual dos marcadores de inimigos

#### Forma = nível de ameaça

A **forma** comunica o perigo imediato do inimigo, derivado de `difficulty` + elevação por palavras-chave do nome/tipo:

| Ameaça | Forma | Exemplos |
|---|---|---|
| Baixa (Easy) | Circulo | Head Grabber, Valuable Thrower |
| Media (Medium) | Quadrado | Oogly, inimigos genericos |
| Alta (High) | Triangulo | Hunter/Huntsman, Beamer (veryheavy) |
| Critica (Elite) | Estrela | Trudge/Slow Walker, Birthday Boy, Elsa, chefes |

Inimigos classificados como `veryheavy` sao elevados automaticamente para Ameaca Alta (triangulo) mesmo que o `difficulty` base seja Medio.
Palavras-chave como `trudge`, `slow walker`, `boss`, `elite` elevam para Ameaca Critica (estrela).

#### Cor = tipo/familia do inimigo

A **cor** diferencia o comportamento/familia do inimigo:

| Familia | Cor | Hex | Exemplos |
|---|---|---|---|
| Comum / generico | Azul escuro | `#1E6BFF` | fallback para desconhecidos |
| Pequeno / critter | Verde-gelo | `#DFFFE8` | Head Grabber, Valuable Thrower |
| Especial / sobrenatural | Lila | `#C084FC` | Beamer (Clown), Oogly, Elsa, Birthday Boy |
| Bruto / cacador | Vermelho | `#FF3B30` | Hunter/Huntsman, Trudge/Slow Walker |

### Configuracao

| Secao | Chave | Padrao | Efeito |
|---|---|---|---|
| Minimap | EnableMinimap | true | Liga/desliga o HUD do minimapa. |
| Minimap | ToggleKey | M | Tecla para alternar o minimapa. |
| Minimap | Width | 260 | Largura do minimapa em pixels. |
| Minimap | Height | 260 | Altura do minimapa em pixels. |
| Minimap | PosX | 24 | Offset horizontal a partir da borda esquerda. |
| Minimap | PosY | 120 | Offset vertical a partir da borda inferior. |
| Minimap | Opacity | 0.85 | Opacidade (0 = invisivel, 1 = opaco). |
| Minimap | Zoom | 2.25 | Fator de zoom ortografico da camera de mapa. |
| Features | RevealRoomsMode | Vanilla | Vanilla = padrao (TAB original). NativeGlobal = revela via SetExplored, tambem afeta TAB. |
| Features | ShowEnemies | true | Mostrar marcadores de inimigos no mapa. |
| Features | EnemyMarkerSize | 0.95 | Escala dos marcadores (0.30-2.00). Aplica em ~2s sem reiniciar. |
| EditMode | EditModeEnabled | true | Liga o modo de edicao in-game (F8). |
| EditMode | EditModeKey | F8 | Tecla para entrar/sair do modo de edicao. |

### Notas

- O minimapa e puramente client-side e nao afeta outros jogadores.
- `RevealRoomsMode = NativeGlobal` chama `RoomVolume.SetExplored()`, o que tambem revela salas no TAB nativo — comportamento opt-in documentado, nao e o padrao.
- Filtro de marcadores por sala inexplorada: **removido em v1.0** (a API nativa de exploracao de salas em modo Vanilla nao e confiavel o suficiente para uso em producao).

---

## English

SurveyorMap adds a persistent native-looking minimap to the bottom-left HUD during R.E.P.O. gameplay. It mirrors the real native map camera output — the same geometry, colors, and fidelity as the TAB map — no fake overlays.

### Features

- **Persistent minimap** — bottom-left HUD, visible only during gameplay. Disappears in menus and lobby.
- **Native map fidelity** — reads the game's own map camera (`activeTexture`). Shows real room geometry, native markers, and wall fidelity.
- **M toggle** — press `M` to show/hide the minimap without affecting the native TAB map.
- **TAB-safe** — when you open the TAB map, the persistent minimap hides automatically. TAB is unaffected.
- **Enemy markers** — shows enemy positions on the map. Dead/despawned enemies are cleaned up automatically within ~2s. **Shape** indicates threat level; **colour** indicates enemy type/family.
- **F8 Edit Mode** — press `F8` to enter edit mode: drag to reposition, grab the bottom-right corner to resize, scroll to zoom, `+`/`-` to adjust size, `Shift+scroll` to resize, `R` to reset to defaults. Config is saved automatically on exit.
- **RevealRooms (opt-in)** — set `RevealRoomsMode = NativeGlobal` to reveal all rooms on level load. **Note:** this also affects the native TAB map. Default is `Vanilla` (TAB stays original).

### Who needs to install it

Only the player who wants the HUD. SurveyorMap does not send RPCs, does not alter networking, and does not require other players to install it.

### Enemy marker visual system

#### Shape = threat level

**Shape** communicates how immediately dangerous the enemy is, derived from `difficulty` + keyword elevation:

| Threat | Shape | Examples |
|---|---|---|
| Low (Easy) | Circle | Head Grabber, Valuable Thrower |
| Medium | Square | Oogly, generic enemies |
| High | Triangle | Hunter/Huntsman, Beamer (veryheavy) |
| Critical (Elite) | Star | Trudge/Slow Walker, Birthday Boy, Elsa, bosses |

Enemies classified as `veryheavy` are automatically elevated to High threat (triangle) even if their base `difficulty` is Medium.
Keywords like `trudge`, `slow walker`, `boss`, `elite` elevate to Critical (star).

#### Colour = enemy type/family

**Colour** differentiates enemy behaviour and family:

| Family | Colour | Hex | Examples |
|---|---|---|---|
| Common / generic | Dark blue | `#1E6BFF` | fallback for unknown types |
| Small / critter | Ice-white green | `#DFFFE8` | Head Grabber, Valuable Thrower |
| Special / supernatural | Lilac | `#C084FC` | Beamer (Clown), Oogly, Elsa, Birthday Boy |
| Brute / hunter | Red / coral | `#FF3B30` | Hunter/Huntsman, Trudge/Slow Walker |

### Configuration

| Section | Key | Default | Effect |
|---|---|---|---|
| Minimap | EnableMinimap | true | Enable/disable the minimap HUD. |
| Minimap | ToggleKey | M | Key to toggle the minimap. |
| Minimap | Width | 260 | Minimap width in pixels. |
| Minimap | Height | 260 | Minimap height in pixels. |
| Minimap | PosX | 24 | Horizontal offset from the left edge. |
| Minimap | PosY | 120 | Vertical offset from the bottom edge. |
| Minimap | Opacity | 0.85 | Minimap opacity (0 = invisible, 1 = fully opaque). |
| Minimap | Zoom | 2.25 | Native map camera orthographic zoom. |
| Features | RevealRoomsMode | Vanilla | Vanilla = default (no SetExplored, TAB stays original). NativeGlobal = reveals via SetExplored, also affects TAB. |
| Features | ShowEnemies | true | Show enemy markers on the map. |
| Features | EnemyMarkerSize | 0.95 | Marker scale multiplier (0.30-2.00). Applies within ~2s, no restart needed. |
| EditMode | EditModeEnabled | true | Enable in-game edit mode (F8). |
| EditMode | EditModeKey | F8 | Key to enter/exit edit mode. |

### Notes

- The minimap is purely client-side and does not affect other players.
- `RevealRoomsMode = NativeGlobal` calls `RoomVolume.SetExplored()` which also reveals rooms in the native TAB map — intentional opt-in, not the default.
- Room-exploration-based marker filtering: **removed in v1.0** (the native room exploration API in Vanilla mode is not reliable enough for production use).
