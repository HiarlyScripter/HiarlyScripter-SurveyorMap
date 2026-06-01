# SurveyorMap

SurveyorMap adds a persistent native-looking minimap to the bottom-left HUD during R.E.P.O. gameplay. It mirrors the real native map camera output — the same geometry, colors, and fidelity as the TAB map — no fake overlays.

## Features

- **Persistent minimap** — bottom-left HUD, visible only during gameplay. Disappears in menus and lobby.
- **Native map fidelity** — reads the game's own map camera (`activeTexture`). Shows real room geometry, native markers, and wall fidelity.
- **M toggle** — press `M` to show/hide the minimap without affecting the native TAB map.
- **TAB-safe** — when you open the TAB map, the persistent minimap hides automatically. TAB is unaffected.
- **ShowEnemies** — shows enemy positions as markers on the map via native `MapCustom` markers. Marker shape and color vary by difficulty tier. Dead/despawned enemies are cleaned up automatically.
- **In-game Edit Mode** — press `F8` to enter edit mode: drag the minimap, resize from the corner, scroll to adjust zoom. Config is saved on exit.
- **RevealRooms (opt-in)** — set `RevealRoomsMode = NativeGlobal` to reveal all rooms on level load. Note: this also affects the native TAB map. Default is `Off` (TAB stays vanilla).

## Who Needs To Install It

Only the player who wants the HUD. SurveyorMap does not send RPCs, does not alter networking, and does not require other players to install it.

## Configuration

| Section   | Key                  | Default          | Effect                                                                              |
|-----------|----------------------|------------------|-------------------------------------------------------------------------------------|
| Minimap   | EnableMinimap        | true             | Enable or disable the minimap HUD.                                                  |
| Minimap   | ToggleKey            | M                | Key to toggle the minimap on/off.                                                   |
| Minimap   | Width                | 260              | Minimap width in pixels.                                                            |
| Minimap   | Height               | 260              | Minimap height in pixels.                                                           |
| Minimap   | PosX                 | 24               | Horizontal offset from the left edge.                                               |
| Minimap   | PosY                 | 120              | Vertical offset from the bottom edge.                                               |
| Minimap   | Opacity              | 0.85             | Minimap opacity (0 = invisible, 1 = opaque).                                        |
| Minimap   | Zoom                 | 2.25             | Native map camera orthographic zoom factor.                                         |
| Features  | RevealRoomsMode      | Off              | Off = safe (TAB vanilla). NativeGlobal = reveal via SetExplored (also affects TAB). |
| Features  | ShowEnemies          | true             | Show enemy markers on the map.                                                      |
| Features  | EnemyMarkerSize      | 1.0              | Scale multiplier for enemy markers (0.1–3.0).                                       |
| Features  | EnemyMarkerShapeMode | DifficultyShape  | DifficultyShape = shape+color per tier. Circle = all circles colored by difficulty. |
| EditMode  | EditModeEnabled      | true             | Enable the F8 in-game edit mode.                                                    |
| EditMode  | EditModeKey          | F8               | Key to enter/exit edit mode.                                                        |

Configuration is stored in `BepInEx/config/com.hiarlyscripter.surveyormap.cfg`.

## Enemy Marker Shapes (DifficultyShape mode)

| Difficulty | Shape    | Color  |
|------------|----------|--------|
| 1 (easy)   | Circle   | Green  |
| 2 (medium) | Diamond  | Yellow |
| 3 (hard)   | Triangle | Orange |
| 4+ (elite) | Star     | Red    |

## Notes

- The minimap is purely client-side and does not affect other players.
- Enemy markers are cleaned up on despawn, death, and via a periodic sweep every 2 seconds.
- RevealRooms `NativeGlobal` calls `RoomVolume.SetExplored()` which also reveals rooms in the native TAB map — this is intentional opt-in behavior, not the default.
- MinimapOnly RevealRooms (reveal only on the persistent minimap, TAB stays vanilla): **BLOCKED** — no safe implementation found that does not mutate `RoomVolume.Explored`. Not implemented.
