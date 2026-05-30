# SurveyorMap

SurveyorMap adds a client-side HUD minimap for R.E.P.O. with local player, teammate, spectator, and optional enemy markers.

## Features

- Client-side minimap HUD.
- Local player and teammate markers.
- Optional enemy markers, disabled by default.
- Spectator support.
- Configurable position, scale, opacity, zoom, marker scale, border, and background.
- SafeMode with per-feature fallback when a game API changes.
- Clear BepInEx logs with the `[SurveyorMap]` prefix.

## Who Needs To Install It

Only the player who wants the HUD needs to install SurveyorMap. The mod does not send RPCs, does not alter networking, and does not require other players to install it.

## Configuration

| Section | Key | Default | Effect |
| --- | --- | --- | --- |
| General | EnableMinimap | true | Enables the HUD minimap. |
| Controls | ToggleKey | M | Toggles the minimap. |
| Layout | PositionPreset | TopRight | Chooses the screen corner. |
| Layout | Scale | 1.0 | Scales the panel. |
| Layout | Opacity | 0.85 | Changes HUD opacity. |
| Layout | ShowBorder | true | Shows or hides the border. |
| Layout | BorderOpacity | 0.9 | Changes border opacity. |
| Layout | BackgroundOpacity | 0.25 | Changes background opacity. |
| Layout | UseSquareMap | true | Uses a square or wide panel. |
| Map | Zoom | 2.25 | Changes world-to-map zoom. |
| Map | RotateWithPlayer | true | Rotates markers with the player. |
| Visibility | HideInLobby | true | Hides the HUD in lobby. |
| Visibility | HideInShop | true | Hides the HUD in shop. |
| Visibility | HideWhenInventoryOpen | false | Hides the HUD when inventory UI is detected. |
| Markers | ShowSelfMarker | true | Shows the local player. |
| Markers | ShowTeammates | true | Shows teammates. |
| Markers | EnableNames | false | Shows teammate names. |
| Markers | ClampMarkersToBounds | true | Keeps markers inside the panel. |
| Markers | MarkerScale | 1.0 | Scales marker size. |
| Enemies | ShowEnemies | false | Enables enemy markers. |
| Enemies | EnemyDetectionMode | Off | Off, NearbyOnly, or All. |
| Enemies | EnemyDetectionRadius | 25 | Radius for NearbyOnly mode. |
| Enemies | EnemyUpdateInterval | 0.75 | Seconds between enemy scans. |
| Enemies | EnemyMarkerStyle | WarningOnly | WarningOnly, Dot, or Pulse. |
| Enemies | MaxEnemyMarkers | 20 | Caps visible enemy markers. |
| General | EnableSpectatorMode | true | Keeps the HUD available while spectating. |
| General | SafeMode | true | Disables failing features instead of spamming errors. |
| Diagnostics | LogLevel | Info | Error, Warning, Info, or Debug logs. |
| Diagnostics | EnableDebugOverlay | false | Shows a small HUD debug line. |
| Performance | UpdateRate | 0.05 | Seconds between visual updates. |

## Dependencies

- BepInEx-BepInExPack-5.4.2100

## Compatibility Notes

SurveyorMap avoids Harmony transpilers, game networking, AI, spawn/despawn, level generation, and native map mutation. If a R.E.P.O. update changes a supported API, SafeMode disables only the affected feature and logs the reason.

<!-- SCREENSHOTS_PLACEHOLDER -->

## Common Problems

- Enemy markers do not show: enable `ShowEnemies` and set `EnemyDetectionMode` to `NearbyOnly` or `All`.
- HUD is hidden in shop or lobby: check `HideInShop` and `HideInLobby`.
- A feature disabled itself: check `BepInEx/LogOutput.log` for `[SurveyorMap]` messages.

Mod criado por HiarlyScripter.
