using BepInEx.Configuration;
using UnityEngine;

namespace SurveyorMap
{
    public class SurveyorMapConfig
    {
        // --- Minimap ---
        public readonly ConfigEntry<bool>    EnableMinimap;
        public readonly ConfigEntry<KeyCode> ToggleKey;
        public readonly ConfigEntry<float>   Width;
        public readonly ConfigEntry<float>   Height;
        public readonly ConfigEntry<float>   PosX;
        public readonly ConfigEntry<float>   PosY;
        public readonly ConfigEntry<float>   Opacity;
        public readonly ConfigEntry<float>   Zoom;

        // --- Features ---
        // Off = safe default (TAB vanilla). NativeGlobal = calls SetExplored (also reveals in TAB).
        public readonly ConfigEntry<string>  RevealRoomsMode;
        public readonly ConfigEntry<bool>    ShowEnemies;

        // --- Enemy markers ---
        public readonly ConfigEntry<float>   EnemyMarkerSize;

        // --- Edit mode ---
        public readonly ConfigEntry<bool>    EditModeEnabled;
        public readonly ConfigEntry<KeyCode> EditModeKey;
        public readonly ConfigEntry<bool>    UnlockCursorInEditMode;
        public readonly ConfigEntry<bool>    FreezeCameraInEditMode;
        public readonly ConfigEntry<bool>    EnableZoomHotkeysOutsideEdit;

        // --- Debug ---
        public readonly ConfigEntry<bool>    DebugLogging;

        public SurveyorMapConfig(ConfigFile cfg)
        {
            EnableMinimap = cfg.Bind("Minimap", "EnableMinimap", true,
                "Show persistent minimap HUD during gameplay.");
            ToggleKey = cfg.Bind("Minimap", "ToggleKey", KeyCode.M,
                "Key to toggle the minimap HUD on/off.");
            Width = cfg.Bind("Minimap", "Width", 260f,
                "Minimap width in pixels.");
            Height = cfg.Bind("Minimap", "Height", 260f,
                "Minimap height in pixels.");
            PosX = cfg.Bind("Minimap", "PosX", 24f,
                "Minimap X offset from the left edge of the screen.");
            PosY = cfg.Bind("Minimap", "PosY", 120f,
                "Minimap Y offset from the bottom edge of the screen.");
            Opacity = cfg.Bind("Minimap", "Opacity", 0.85f,
                "Minimap opacity (0 = invisible, 1 = fully opaque).");
            Zoom = cfg.Bind("Minimap", "Zoom", 2.25f,
                "Orthographic size of the native map camera while minimap is visible.");

            RevealRoomsMode = cfg.Bind("Features", "RevealRoomsMode", "Vanilla",
                "Vanilla = default, no SetExplored calls, TAB stays original. NativeGlobal = reveals rooms via RoomVolume.SetExplored() — also affects the native TAB map.");
            ShowEnemies = cfg.Bind("Features", "ShowEnemies", true,
                "Show enemy positions on the map using native MapCustom markers.");

            EnemyMarkerSize = cfg.Bind("Features", "EnemyMarkerSize", 0.95f,
                "Scale multiplier for enemy markers (0.30–2.00). Default 0.95. Changes apply within ~2s without restart.");

            EditModeEnabled = cfg.Bind("EditMode", "EditModeEnabled", true,
                "Enable the in-game minimap edit mode (F8 by default).");
            EditModeKey = cfg.Bind("EditMode", "EditModeKey", KeyCode.F8,
                "Key to enter/exit minimap edit mode. In edit mode: drag to move, resize from corner, scroll to zoom.");
            UnlockCursorInEditMode = cfg.Bind("EditMode", "UnlockCursorInEditMode", true,
                "When true, the cursor is unlocked and made visible automatically when F8 edit mode is active, " +
                "so you can drag and resize the minimap without pressing ESC first. " +
                "The cursor state is restored when you exit edit mode. " +
                "If the game still captures the cursor, press ESC once to release it manually.");
            FreezeCameraInEditMode = cfg.Bind("EditMode", "FreezeCameraInEditMode", true,
                "When true, mouse-look is suspended while F8 edit mode is active. " +
                "Calls InputManager.DisableAiming() every frame so the camera ignores mouse delta. " +
                "Camera control is restored automatically when you exit edit mode.");
            EnableZoomHotkeysOutsideEdit = cfg.Bind("EditMode", "EnableZoomHotkeysOutsideEdit", true,
                "When true, + and - keys (main keyboard and numpad) adjust the minimap zoom level " +
                "during normal gameplay without entering F8 edit mode. " +
                "Only active when the minimap is visible, edit mode is off, and the TAB map is not open.");

            DebugLogging = cfg.Bind("Debug", "DebugLogging", false,
                "Enable verbose debug logging to BepInEx/LogOutput.log. " +
                "Default false (silent in release). Set true only for diagnostics: " +
                "exposes sweep, AddCustom, enemy classification, and room-exploration logs.");
        }
    }
}
