# Changelog - SurveyorMap

---

## v1.0.0 - 2026-06-01 (patch)

**Compatibilidade:** R.E.P.O. · BepInEx `5.4.2100`

### Enemy Markers

- Registry-based tracking per `EnemyParent.instanceID` — no duplicate markers.
- Marker cleanup on despawn, death (`EnemyHealth.DeathRPC` / `DeathImpulseRPC`), and periodic sweep every 2 seconds.
- Sweep also removes markers whose host `GameObject` is null, inactive, or has `EnemyHealth.dead=true`.
- Marker shape and color per difficulty tier: circle (easy/green), diamond (medium/yellow), triangle (hard/orange), star (elite/red).
- New configs: `EnemyMarkerSize` (scale multiplier), `EnemyMarkerShapeMode` (DifficultyShape | Circle).

### In-game Edit Mode

- Press `F8` to enter/exit edit mode.
- Drag the minimap to reposition; drag the bottom-right corner to resize; scroll wheel adjusts zoom.
- Values (X/Y/W/H/Zoom) shown as overlay while in edit mode.
- Config is saved automatically on exit.
- New configs: `EditModeEnabled`, `EditModeKey`.

### RevealRooms

- `RevealRooms` boolean replaced by `RevealRoomsMode` string: `Vanilla` (default) | `NativeGlobal`.
- Default `Off` preserves the native TAB map exactly as vanilla (no `SetExplored` calls).
- `NativeGlobal` is the previous behavior (opt-in): reveals all rooms via `RoomVolume.SetExplored()` — also affects the TAB map.
- MinimapOnly reveal (TAB stays vanilla): **BLOCKED** — no safe implementation found without mutating `RoomVolume.Explored`.

---

## v1.0.0 - 2026-05-31

**Compatibilidade:** R.E.P.O. · BepInEx `5.4.2100`

### Complete Rebuild (v2 Architecture)

- Replaced previous implementation with a clean native-map mirror.
- Persistent minimap mirrors the native `Dirt Finder Map Camera` activeTexture — no fake overlays, no custom geometry.
- M key toggles the minimap HUD.
- TAB map is fully unaffected; minimap hides automatically while TAB is open.
- RevealRooms via native `RoomVolume.SetExplored()`.
- ShowEnemies via native `MapCustom` markers with cleanup on despawn.
- Removed: RuntimeLoop, RuntimeProbeBehaviour, CenterOnPlayer, fake room overlays, owned RenderTexture, cyan proof HUD.
