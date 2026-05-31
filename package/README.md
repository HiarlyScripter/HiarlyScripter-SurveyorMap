# SurveyorMap

SurveyorMap adds a persistent native-looking minimap to the bottom-left HUD during R.E.P.O. gameplay. It mirrors the real native map camera output — the same geometry, colors, and fidelity as the TAB map — no fake overlays.

## Features

- **Persistent minimap** — bottom-left HUD, visible only during gameplay. Disappears in menus and lobby.
- **Native map fidelity** — reads the game's own map camera (`activeTexture`). Shows real room geometry, native markers, and wall fidelity.
- **M toggle** — press `M` to show/hide the minimap without affecting the native TAB map.
- **TAB-safe** — when you open the TAB map, the persistent minimap hides automatically. TAB is unaffected.
- **RevealRooms** — reveals all rooms on level load via the native room system (`RoomVolume.SetExplored()`). Rooms appear on both the minimap and the TAB map.
- **ShowEnemies** — shows enemy positions as markers on the map via native `MapCustom` markers. Markers are automatically removed when enemies despawn.

## Who Needs To Install It

Only the player who wants the HUD. SurveyorMap does not send RPCs, does not alter networking, and does not require other players to install it.

## Configuration

| Section   | Key          | Default | Effect                                         |
|-----------|--------------|---------|------------------------------------------------|
| Minimap   | EnableMinimap | true   | Enable or disable the minimap HUD.             |
| Minimap   | ToggleKey    | M       | Key to toggle the minimap on/off.              |
| Minimap   | Width        | 260     | Minimap width in pixels.                       |
| Minimap   | Height       | 260     | Minimap height in pixels.                      |
| Minimap   | PosX         | 24      | Horizontal offset from the left edge.          |
| Minimap   | PosY         | 120     | Vertical offset from the bottom edge.          |
| Minimap   | Opacity      | 0.85    | Minimap opacity (0 = invisible, 1 = opaque).   |
| Minimap   | Zoom         | 2.25    | Native map camera orthographic zoom factor.    |
| Features  | RevealRooms  | true    | Reveal all rooms on level load.                |
| Features  | ShowEnemies  | true    | Show enemy markers on the map.                 |

Configuration is stored in `BepInEx/config/com.hiarlyscripter.surveyormap.cfg`.

## Notes

- The minimap is purely client-side and does not affect other players.
- Enemy markers are cleaned up automatically on despawn.
- RevealRooms uses the native game mechanism; rooms appear as explored on both the minimap and the TAB map.
