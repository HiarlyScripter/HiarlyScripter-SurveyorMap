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
        // ShowEnemiesInUnexploredRooms: false (default) = hide in unexplored rooms; true = always show
        public readonly ConfigEntry<bool>    ShowEnemiesInUnexploredRooms;

        // --- Edit mode ---
        public readonly ConfigEntry<bool>    EditModeEnabled;
        public readonly ConfigEntry<KeyCode> EditModeKey;

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

            EnemyMarkerSize = cfg.Bind("Features", "EnemyMarkerSize", 0.65f,
                "Scale multiplier for enemy markers (0.30–2.00). Default 0.65. Changes apply within ~2s without restart.");
            ShowEnemiesInUnexploredRooms = cfg.Bind("Features", "ShowEnemiesInUnexploredRooms", false,
                "false (default) = hide enemy markers in unexplored rooms (more vanilla). " +
                "true = show all enemy markers regardless of room exploration. " +
                "Detection uses physics overlap; if room cannot be determined, marker is shown (fail-safe).");

            EditModeEnabled = cfg.Bind("EditMode", "EditModeEnabled", true,
                "Enable the in-game minimap edit mode (F8 by default).");
            EditModeKey = cfg.Bind("EditMode", "EditModeKey", KeyCode.F8,
                "Key to enter/exit minimap edit mode. In edit mode: drag to move, resize from corner, scroll to zoom.");
        }
    }
}
