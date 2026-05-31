using BepInEx.Configuration;
using UnityEngine;

namespace SurveyorMap
{
    public class SurveyorMapConfig
    {
        public readonly ConfigEntry<bool>    EnableMinimap;
        public readonly ConfigEntry<KeyCode> ToggleKey;
        public readonly ConfigEntry<float>   Width;
        public readonly ConfigEntry<float>   Height;
        public readonly ConfigEntry<float>   PosX;
        public readonly ConfigEntry<float>   PosY;
        public readonly ConfigEntry<float>   Opacity;
        public readonly ConfigEntry<float>   Zoom;
        public readonly ConfigEntry<bool>    RevealRooms;
        public readonly ConfigEntry<bool>    ShowEnemies;

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
            RevealRooms = cfg.Bind("Features", "RevealRooms", true,
                "Reveal all rooms on level load via native RoomVolume.SetExplored().");
            ShowEnemies = cfg.Bind("Features", "ShowEnemies", true,
                "Show enemy positions on the map using native MapCustom markers.");
        }
    }
}
