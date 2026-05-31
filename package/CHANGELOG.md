# Changelog - SurveyorMap

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
