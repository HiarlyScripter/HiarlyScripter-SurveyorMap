# SurveyorMap

---

## Português

O SurveyorMap adiciona um minimapa nativo persistente no HUD inferior esquerdo durante o gameplay do R.E.P.O. Ele espelha a saída real da câmera de mapa do jogo — mesma geometria, mesmas cores, mesma fidelidade que o TAB — sem overlays falsos.

### Funcionalidades

- **Minimapa persistente** — HUD inferior esquerdo, visível apenas durante o gameplay. Some nos menus e no lobby.
- **Fidelidade nativa** — lê a câmera de mapa real do jogo (`activeTexture`). Mostra a geometria real das salas, marcadores nativos e fidelidade das paredes.
- **Tecla M** — pressione `M` para mostrar/ocultar o minimapa sem afetar o TAB nativo.
- **TAB seguro** — ao abrir o TAB, o minimapa persistente some automaticamente. O TAB não é afetado.
- **Marcadores de inimigos** — mostra posições de inimigos como marcadores no mapa. Inimigos mortos/despawnados são removidos automaticamente em até ~2s. A **forma** e a **cor** indicam o nível de ameaça do inimigo de forma redundante, para máxima legibilidade no minimapa pequeno.
- **Modo de edição F8** — pressione `F8` para entrar no modo de edição. O cursor é liberado e a câmera é suspensa automaticamente — você pode editar o minimapa sem mover a câmera do jogador. Arraste para reposicionar, segure o canto inferior direito para redimensionar, scroll para zoom, `+`/`-` para ajustar tamanho, `Shift+scroll` para redimensionar, `R` para resetar para os valores padrão. Tudo é restaurado ao sair com `F8`. Config salva automaticamente.
- **RevealRooms (opt-in)** — defina `RevealRoomsMode = NativeGlobal` para revelar todas as salas ao entrar no nível. **Atenção:** este modo também afeta o TAB nativo. O padrão é `Vanilla` (TAB permanece original).

### Quem precisa instalar

Apenas o jogador que quer o HUD. O SurveyorMap não envia RPCs, não altera a rede e não requer instalação pelos outros jogadores.

### Sistema visual dos marcadores de inimigos

**Forma e cor comunicam o mesmo nível de ameaça do inimigo, de forma redundante.**
Isso maximiza a legibilidade no minimapa pequeno — você identifica o perigo tanto pela forma quanto pela cor.

| Ameaça | Forma | Cor | Hex |
|---|---|---|---|
| Baixa (Easy) | Círculo | Verde-gelo | `#DFFFE8` |
| Média (Medium) | Quadrado | Azul escuro | `#1E6BFF` |
| Alta (High) | Triângulo | Lilás | `#C084FC` |
| Crítica (Elite) | Estrela | Vermelho/coral | `#FF3B30` |

O nível de ameaça é derivado do campo `difficulty` do inimigo. Palavras-chave no nome/tipo podem elevar o nível (nunca reduzir):
- `veryheavy` → elevado para pelo menos Alta (triângulo/lilás)
- `trudge`, `slow walker`, `boss`, `elite` → elevado para Crítica (estrela/vermelho)
- `hunt`, `huntsman`, `bang`, `rush`, `charge` → elevado para pelo menos Alta (triângulo/lilás)

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
| Features | EnemyMarkerSize | 0.95 | Escala dos marcadores (0.30–2.00). Aplica em ~2s sem reiniciar. Não requer reinício. |
| EditMode | EditModeEnabled | true | Liga o modo de edição in-game (F8). |
| EditMode | EditModeKey | F8 | Tecla para entrar/sair do modo de edição. |
| EditMode | UnlockCursorInEditMode | true | Libera o cursor automaticamente ao entrar no modo de edição F8. Se o jogo ainda capturar o mouse, pressione ESC uma vez. |
| EditMode | FreezeCameraInEditMode | true | Suspende o mouse-look da câmera ao entrar no modo F8 (patch Harmony em `CameraAim.Update`). Nenhuma API de sistema, nenhuma simulação de tecla. O controle da câmera é restaurado ao sair. |
| Debug | DebugLogging | false | Habilita logs verbosos no BepInEx/LogOutput.log. Padrão false (silencioso em release). Ative somente para diagnóstico. |

### Notas

- O minimapa é puramente client-side e não afeta outros jogadores.
- `RevealRoomsMode = NativeGlobal` chama `RoomVolume.SetExplored()`, o que também revela salas no TAB nativo — comportamento opt-in documentado, não é o padrão.
- Filtro de marcadores por sala inexplorada: **removido em v1.0** (a API nativa de exploração de salas em modo Vanilla não é confiável o suficiente para uso em produção).

---

## English

SurveyorMap adds a persistent native-looking minimap to the bottom-left HUD during R.E.P.O. gameplay. It mirrors the real native map camera output — the same geometry, colors, and fidelity as the TAB map — no fake overlays.

### Features

- **Persistent minimap** — bottom-left HUD, visible only during gameplay. Disappears in menus and lobby.
- **Native map fidelity** — reads the game's own map camera (`activeTexture`). Shows real room geometry, native markers, and wall fidelity.
- **M toggle** — press `M` to show/hide the minimap without affecting the native TAB map.
- **TAB-safe** — when you open the TAB map, the persistent minimap hides automatically. TAB is unaffected.
- **Enemy markers** — shows enemy positions on the map. Dead/despawned enemies are cleaned up automatically within ~2s. **Shape** and **colour** both indicate threat level, redundantly, for maximum readability at minimap scale.
- **F8 Edit Mode** — press `F8` to enter edit mode. The cursor is unlocked and camera look is suspended automatically — you can edit the minimap without spinning the player's view. Drag to reposition, grab the bottom-right corner to resize, scroll to zoom, `+`/`-` to adjust size, `Shift+scroll` to resize, `R` to reset to defaults. Everything is restored on `F8` exit. Config is saved automatically.
- **RevealRooms (opt-in)** — set `RevealRoomsMode = NativeGlobal` to reveal all rooms on level load. **Note:** this also affects the native TAB map. Default is `Vanilla` (TAB stays original).

### Who needs to install it

Only the player who wants the HUD. SurveyorMap does not send RPCs, does not alter networking, and does not require other players to install it.

### Enemy marker visual system

#### Shape and colour = threat level (redundant)

**Both shape and colour communicate the same thing: how dangerous the enemy is.**
This redundancy maximises readability on a small minimap — you recognise the threat level by either glyph or colour at a glance.

| Threat | Shape | Colour | Hex |
|---|---|---|---|
| Low (Easy) | Circle | Ice-green | `#DFFFE8` |
| Medium | Square | Dark blue | `#1E6BFF` |
| High | Triangle | Lilac | `#C084FC` |
| Critical (Elite) | Star | Red / coral | `#FF3B30` |

Threat level is derived from the enemy's `difficulty` field. Name/type keywords can only elevate the tier (never reduce it):
- `veryheavy` → elevated to at least High (triangle / lilac)
- `trudge`, `slow walker`, `boss`, `elite` → elevated to Critical (star / red)
- `hunt`, `huntsman`, `bang`, `rush`, `charge` → elevated to at least High (triangle / lilac)

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
| Features | EnemyMarkerSize | 0.95 | Marker scale multiplier (0.30–2.00). Applies within ~2s without restart. |
| EditMode | EditModeEnabled | true | Enable in-game edit mode (F8). |
| EditMode | EditModeKey | F8 | Key to enter/exit edit mode. |
| EditMode | UnlockCursorInEditMode | true | Unlocks and shows the cursor automatically when F8 edit mode is active. If the game still captures the mouse, press ESC once to release it manually. |
| EditMode | FreezeCameraInEditMode | true | Suspends camera mouse-look when F8 edit mode is active (Harmony prefix on `CameraAim.Update`). No system API, no key simulation. Camera control is restored on exit. |
| Debug | DebugLogging | false | Enable verbose logging to BepInEx/LogOutput.log. Default false (silent in release). Enable only for diagnostics. |

### Notes

- The minimap is purely client-side and does not affect other players.
- `RevealRoomsMode = NativeGlobal` calls `RoomVolume.SetExplored()` which also reveals rooms in the native TAB map — intentional opt-in, not the default.
- Room-exploration-based marker filtering: **removed in v1.0** (the native room exploration API in Vanilla mode is not reliable enough for production use).
