using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SurveyorMap
{
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    public sealed class SurveyorMapPlugin : BaseUnityPlugin
    {
        public const string PluginGuid = "com.hiarlyscripter.surveyormap";
        public const string PluginName = "SurveyorMap";
        public const string PluginVersion = "1.0.0";

        internal static ManualLogSource Log { get; private set; }
        internal static SurveyorMapConfig Settings { get; private set; }
        internal static SurveyorMapPlugin Instance { get; private set; }

        private Harmony _harmony;
        private SurveyorMapController _controller;
        private CapabilityReport _capabilityReport;
        private Coroutine _runtimeLoop;
        private float _nextRuntimeLoopHeartbeat;
        private float _runtimeLoopLastHeartbeat;
        private int _toggleHandledFrame = -1;
        private float _proofLogNext;
        private bool _proofHudVisible = true;
        private bool _proofHudFirstLog;
        private int _proofUpdateCounter;
        private int _pluginOnGuiCount;

        // Exposed for RuntimeProbeBehaviour diagnostics
        internal string BuildTagShort { get; private set; } = "?";
        internal int PluginUpdateCount => _proofUpdateCounter;
        internal int PluginOnGuiCount => _pluginOnGuiCount;
        internal bool ControllerExists => _controller != null;
        internal bool HudExists => _controller != null && _controller.HudCreated;
        internal bool ProofHudVisible => _proofHudVisible;

        private void Awake()
        {
            Instance = this;
            // ── CRITICAL: detach from BepInEx parent and mark DontDestroyOnLoad ──────────
            // Without this, the plugin gameObject is destroyed on the first scene transition
            // (e.g., "Leave to Main Menu"), silently killing Update(), OnGUI() and the coroutine.
            // dig-Minimap uses the same approach. This is the root cause of the silent runtime.
            if (gameObject.transform.parent != null)
                gameObject.transform.parent = null;
            DontDestroyOnLoad(gameObject);

            Log = Logger;
            Settings = new SurveyorMapConfig(Config);
            LogHelper.Configure(Settings.LogLevel.Value);
            Settings.MigrateLayoutDefaults();

            LogHelper.Info("SurveyorMap v1.0.0 loading.");
            try
            {
                string dllPath = Assembly.GetExecutingAssembly().Location;
                System.DateTime ts = File.GetLastWriteTime(dllPath);
                byte[] raw = File.ReadAllBytes(dllPath);
                byte[] hashBytes = System.Security.Cryptography.MD5.Create().ComputeHash(raw);
                string shortHash = BitConverter.ToString(hashBytes, 0, 4).Replace("-", string.Empty).ToLowerInvariant();
                BuildTagShort = shortHash;
                LogHelper.Info("BuildTag: " + ts.ToString("yyyy-MM-dd HH:mm:ss") + " | md5=" + shortHash + " | " + dllPath);
            }
            catch (Exception ex)
            {
                LogHelper.Info("BuildTag: unavailable (" + ex.Message + ")");
            }
            CapabilityReport report = GameApi.ValidateCapabilities();
            _capabilityReport = report;
            LogHelper.Info("Capabilities: teammates=" + State(report.Teammates) +
                           ", enemies=" + State(report.Enemies) +
                           ", spectator=" + State(report.Spectator) +
                           ", ui=" + State(report.Ui));

            _harmony = new Harmony(PluginGuid);
            _harmony.PatchAll(typeof(SurveyorMapPatches));

            DestroyExistingRuntimeObjects();
            EnsureControllerAlive("awake");
            _runtimeLoop = StartCoroutine(RuntimeLoop());
            LogHelper.Info("RuntimeLoop scheduled. coroutine=" + (_runtimeLoop != null) +
                           ", pluginEnabled=" + enabled +
                           ", pluginActive=" + gameObject.activeInHierarchy +
                           ", dontDestroyOnLoad=True");
            // Independent probe on its own DontDestroyOnLoad GO — survives even if plugin GO dies
            RuntimeProbeBehaviour.EnsureCreated();
            LogHelper.Info("SurveyorMap initialized.");
        }

        private IEnumerator RuntimeLoop()
        {
            LogHelper.Info("RuntimeLoop started.");

            while (true)
            {
                _runtimeLoopLastHeartbeat = Time.unscaledTime;

                try
                {
                    EnsureControllerAlive("runtime-loop");
                }
                catch (Exception ex)
                {
                    LogHelper.RateLimitedError("runtime-loop-ensure", "EnsureControllerAlive exception in RuntimeLoop: " + ex.Message, 5f);
                }

                if (Settings != null && Input.GetKeyDown(Settings.ToggleKey.Value))
                {
                    _toggleHandledFrame = Time.frameCount;
                    LogHelper.Info("Toggle key detected in RuntimeLoop: " + Settings.ToggleKey.Value);

                    if (_controller != null)
                        _controller.ToggleFromRuntimeLoop(Settings.ToggleKey.Value);
                }

                float now = Time.unscaledTime;
                if (now >= _nextRuntimeLoopHeartbeat)
                {
                    _nextRuntimeLoopHeartbeat = now + 5f;
                    LogHelper.Debug("RuntimeLoop heartbeat. frame=" + Time.frameCount +
                                    ", pluginEnabled=" + enabled +
                                    ", pluginActive=" + gameObject.activeInHierarchy +
                                    ", controller=" + (_controller != null));
                }

                if (_controller != null)
                    _controller.RuntimeUpdate("runtime-loop");

                yield return null;
            }
        }

        private void Update()
        {
            try
            {
                float now = Time.unscaledTime;
                _proofUpdateCounter++;

                // ── ABSOLUTE heartbeat: no gate, no level check, fires every 5 s from frame 0 ─
                // If this NEVER appears in LogOutput.log, Plugin.Update is not being called
                // at all — the issue is at the MonoBehaviour lifecycle level.
                if (now >= _proofLogNext)
                {
                    _proofLogNext = now + 5f;
                    string sceneName = "?";
                    try { sceneName = SceneManager.GetActiveScene().name; } catch { }
                    LogHelper.Info("Plugin.Update absolute alive. frame=" + Time.frameCount +
                                   ", t=" + now.ToString("0.0") +
                                   ", scene=" + sceneName +
                                   ", activeSelf=" + gameObject.activeSelf +
                                   ", activeInHierarchy=" + gameObject.activeInHierarchy +
                                   ", enabled=" + enabled +
                                   ", forceProof=" + (Settings != null && Settings.ForceHudProofOfLife.Value) +
                                   ", toggleVisible=" + _proofHudVisible +
                                   ", updateCount=" + _proofUpdateCounter);
                }

                // ── Toggle M: unconditional when ForceHudProofOfLife=true ───────────────────
                if (Settings != null && Input.GetKeyDown(Settings.ToggleKey.Value) &&
                    _toggleHandledFrame != Time.frameCount)
                {
                    _toggleHandledFrame = Time.frameCount;
                    if (Settings.ForceHudProofOfLife.Value)
                    {
                        _proofHudVisible = !_proofHudVisible;
                        LogHelper.Info("Toggle key detected in absolute Plugin.Update: " + Settings.ToggleKey.Value +
                                       ", toggleVisible=" + _proofHudVisible);
                    }
                    else
                    {
                        LogHelper.Info("Toggle key detected in plugin Update: " + Settings.ToggleKey.Value);
                        if (_controller != null)
                            _controller.ToggleFromRuntimeLoop(Settings.ToggleKey.Value);
                    }
                }

                // ── Restart RuntimeLoop if heartbeat lost ────────────────────────────────────
                if (_runtimeLoopLastHeartbeat > 0f && now - _runtimeLoopLastHeartbeat > 2f)
                {
                    LogHelper.Info("RuntimeLoop heartbeat lost. last=" + _runtimeLoopLastHeartbeat.ToString("0.0") +
                                   "s, now=" + now.ToString("0.0") + "s, frame=" + Time.frameCount + ". Restarting.");
                    if (_runtimeLoop != null)
                        StopCoroutine(_runtimeLoop);
                    _runtimeLoopLastHeartbeat = 0f;
                    _runtimeLoop = StartCoroutine(RuntimeLoop());
                }

                // ── Fallback: ensure controller + RuntimeUpdate ──────────────────────────────
                EnsureControllerAlive("plugin-update");
                if (_controller != null)
                    _controller.RuntimeUpdate("plugin-update");
            }
            catch (Exception ex)
            {
                LogHelper.RateLimitedError("plugin-update", "Plugin.Update exception:\n" + ex, 5f);
            }
        }

        // dig-Minimap-style IsInLevel — for reference only; no longer used as a gate in proofs.
        private static bool IsInLevelDirect()
        {
            try
            {
                return SemiFunc.RunIsLevel() &&
                       GameDirector.instance != null &&
                       (int)GameDirector.instance.currentState == 2;
            }
            catch
            {
                return false;
            }
        }

        private void OnGUI()
        {
            try
            {
                _pluginOnGuiCount++;
                // ── ABSOLUTE Proof-of-Life overlay ─────────────────────────────────────────
                // NO level check. NO gate. NO toggle default guard.
                // Draws on EVERY frame (menu AND level) when ForceHudProofOfLife=true.
                // This proves Plugin.OnGUI is alive regardless of game state.
                // If this box NEVER appears: Plugin.OnGUI is dead (MonoBehaviour issue).
                if (Settings != null && Settings.ForceHudProofOfLife.Value && _proofHudVisible)
                {
                    if (!_proofHudFirstLog)
                    {
                        _proofHudFirstLog = true;
                        string sceneName = "?";
                        try { sceneName = SceneManager.GetActiveScene().name; } catch { }
                        LogHelper.Info("OnGUI absolute proof reached. scene=" + sceneName +
                                       ", frame=" + Time.frameCount);
                    }

                    float w = 260f;
                    float h = 260f;
                    float x = 24f;
                    // Canvas BottomLeft 260x260 at (24,120) → GUI y = Screen.height - 120 - 260
                    float y = Screen.height - 120f - h;

                    Color prev = GUI.color;

                    // Dark background
                    GUI.color = new Color(0.02f, 0.03f, 0.05f, 0.88f);
                    GUI.DrawTexture(new Rect(x, y, w, h), Texture2D.whiteTexture);

                    // Cyan border 4 px
                    GUI.color = new Color(0.18f, 0.92f, 0.98f, 0.95f);
                    GUI.DrawTexture(new Rect(x,           y,           w,  4f), Texture2D.whiteTexture);
                    GUI.DrawTexture(new Rect(x,           y + h - 4f,  w,  4f), Texture2D.whiteTexture);
                    GUI.DrawTexture(new Rect(x,           y,           4f, h),  Texture2D.whiteTexture);
                    GUI.DrawTexture(new Rect(x + w - 4f,  y,           4f, h),  Texture2D.whiteTexture);

                    // Text
                    GUI.color = Color.white;
                    string sceneTxt = "?";
                    try { sceneTxt = SceneManager.GetActiveScene().name; } catch { }
                    GUI.Label(new Rect(x + 8f, y + 8f,  w - 16f, 24f), "SurveyorMap LIVE");
                    GUI.Label(new Rect(x + 8f, y + 30f, w - 16f, 20f), "frame=" + Time.frameCount);
                    GUI.Label(new Rect(x + 8f, y + 48f, w - 16f, 20f), "scene=" + sceneTxt);
                    GUI.Label(new Rect(x + 8f, y + 66f, w - 16f, 20f), "toggle=True  M=hide");
                    GUI.Label(new Rect(x + 8f, y + 84f, w - 16f, 20f), "pluginUpdate=" + _proofUpdateCounter);

                    GUI.color = prev;
                }

                // ── Optional debug overlay ─────────────────────────────────────────────────
                if (Settings != null && Settings.EnableDebugOverlay.Value)
                {
                    const float width = 230f;
                    const float height = 58f;
                    Rect rect = new Rect(Screen.width - width - 20f, 20f, width, height);
                    GUI.Box(rect, "SurveyorMap DEBUG HUD\nRuntime loop visual test");
                }
            }
            catch (Exception ex)
            {
                LogHelper.RateLimitedError("plugin-ongui", "Plugin.OnGUI exception:\n" + ex, 5f);
            }
        }

        private void EnsureControllerAlive(string source)
        {
            bool controllerWasMissing = _controller == null;
            bool initializedNow = false;

            if (_controller == null)
            {
                if (source != "awake")
                    LogHelper.Info("Controller missing; recreating.");

                DestroyOrphanRuntimeObjects();
                _controller = gameObject.GetComponent<SurveyorMapController>();
                if (_controller == null)
                    _controller = gameObject.AddComponent<SurveyorMapController>();

                LogHelper.Info("Controller AddComponent result: component=" + (_controller != null) +
                               ", enabled=" + (_controller != null && _controller.enabled) +
                               ", activeSelf=" + gameObject.activeSelf +
                               ", activeInHierarchy=" + gameObject.activeInHierarchy +
                               ", isActiveAndEnabled=" + (_controller != null && _controller.isActiveAndEnabled));
            }

            if (_controller != null && (!_controller.IsInitialized || !_controller.HudCreated))
            {
                _controller.Initialize(_capabilityReport);
                initializedNow = true;
            }

            if (_controller != null && (controllerWasMissing || initializedNow))
            {
                LogHelper.Info("Controller ready. controller=True" +
                               ", hudCreated=" + _controller.HudCreated +
                               ", hudActive=" + _controller.HudActive);
            }
        }

        private void OnDestroy()
        {
            if (_runtimeLoop != null)
                StopCoroutine(_runtimeLoop);

            _controller = null;

            if (_harmony != null)
                _harmony.UnpatchSelf();
        }

        private static string State(bool value)
        {
            return value ? "enabled" : "disabled";
        }

        private static void DestroyExistingRuntimeObjects()
        {
            DestroyOrphanRuntimeObjects();
        }

        private static void DestroyOrphanRuntimeObjects()
        {
            GameObject[] objects = Resources.FindObjectsOfTypeAll<GameObject>();
            for (int i = 0; i < objects.Length; i++)
            {
                GameObject obj = objects[i];
                if (obj == null)
                    continue;

                if (obj.name != "SurveyorMap.HUD" && obj.name != "SurveyorMap.Root")
                    continue;

                LogHelper.Info("Destroying orphan runtime object: name=" + obj.name +
                               ", id=" + obj.GetInstanceID() +
                               ", scene=" + SceneName(obj) +
                               ", activeSelf=" + obj.activeSelf +
                               ", activeInHierarchy=" + obj.activeInHierarchy);
                Destroy(obj);
            }
        }

        internal static string SceneName(GameObject obj)
        {
            if (obj == null)
                return "null";

            return obj.scene.IsValid() ? obj.scene.name : "invalid";
        }
    }

    internal enum PositionPreset
    {
        BottomLeft,
        BottomRight,
        TopLeft,
        TopRight,
        Custom
    }

    internal enum EnemyDetectionMode
    {
        Off,
        NearbyOnly,
        All
    }

    internal enum RevealMode
    {
        Off,
        ExploredOnly,
        FullReveal,
        OnLevelStart
    }

    internal enum EnemyDangerTier
    {
        Low,
        Medium,
        High,
        Unknown
    }

    internal enum EnemyMarkerStyle
    {
        WarningOnly,
        Dot,
        Pulse
    }

    internal enum LogLevelSetting
    {
        Error,
        Warning,
        Info,
        Debug
    }

    internal sealed class SurveyorMapConfig
    {
        public readonly ConfigEntry<bool> EnableMinimap;
        public readonly ConfigEntry<KeyCode> ToggleKey;
        public readonly ConfigEntry<PositionPreset> PositionPreset;
        public readonly ConfigEntry<float> Scale;
        public readonly ConfigEntry<float> Opacity;
        public readonly ConfigEntry<float> Zoom;
        public readonly ConfigEntry<bool> RotateWithPlayer;
        public readonly ConfigEntry<bool> RevealRooms;
        public readonly ConfigEntry<RevealMode> RevealMode;
        public readonly ConfigEntry<bool> ShowSelfMarker;
        public readonly ConfigEntry<bool> ShowTeammates;
        public readonly ConfigEntry<bool> ShowEnemies;
        public readonly ConfigEntry<EnemyDetectionMode> EnemyDetectionMode;
        public readonly ConfigEntry<float> EnemyDetectionRadius;
        public readonly ConfigEntry<float> EnemyUpdateInterval;
        public readonly ConfigEntry<EnemyMarkerStyle> EnemyMarkerStyle;
        public readonly ConfigEntry<bool> EnableSpectatorMode;
        public readonly ConfigEntry<bool> SafeMode;
        public readonly ConfigEntry<LogLevelSetting> LogLevel;
        public readonly ConfigEntry<bool> ShowBorder;
        public readonly ConfigEntry<float> BorderOpacity;
        public readonly ConfigEntry<float> BorderThickness;
        public readonly ConfigEntry<float> BackgroundOpacity;
        public readonly ConfigEntry<float> MapOpacity;
        public readonly ConfigEntry<bool> SoftFrame;
        public readonly ConfigEntry<bool> ShowNativeQuestionMarks;
        public readonly ConfigEntry<bool> HideInLobby;
        public readonly ConfigEntry<bool> HideInShop;
        public readonly ConfigEntry<bool> HideWhenInventoryOpen;
        public readonly ConfigEntry<bool> EnableNames;
        public readonly ConfigEntry<bool> ClampMarkersToBounds;
        public readonly ConfigEntry<int> MaxEnemyMarkers;
        public readonly ConfigEntry<bool> DebugEnemyTelemetry;
        public readonly ConfigEntry<bool> EnableDebugOverlay;
        public readonly ConfigEntry<float> UpdateRate;
        public readonly ConfigEntry<float> CaptureFPS;
        public readonly ConfigEntry<bool> CaptureOnlyWhenVisible;
        public readonly ConfigEntry<bool> PauseCaptureWhenToggleOff;
        public readonly ConfigEntry<int> RenderTextureSize;
        public readonly ConfigEntry<bool> EnableProfiling;
        public readonly ConfigEntry<float> ProfilingLogInterval;
        public readonly ConfigEntry<float> RenderWarningMs;
        public readonly ConfigEntry<bool> AutoThrottleCapture;
        public readonly ConfigEntry<float> MarkerScale;
        public readonly ConfigEntry<bool> UseSquareMap;
        public readonly ConfigEntry<float> PosX;
        public readonly ConfigEntry<float> PosY;
        public readonly ConfigEntry<float> Width;
        public readonly ConfigEntry<float> Height;
        public readonly ConfigEntry<bool> CenterOnPlayer;
        public readonly ConfigEntry<bool> SmoothShapes;
        public readonly ConfigEntry<bool> ForceHudProofOfLife;
        private readonly ConfigEntry<int> _layoutDefaultsVersion;

        public SurveyorMapConfig(ConfigFile config)
        {
            EnableMinimap = config.Bind("General", "EnableMinimap", true, "Enable the SurveyorMap HUD.");
            ToggleKey = config.Bind("Controls", "ToggleKey", KeyCode.M, "Toggle the minimap HUD.");
            PositionPreset = config.Bind("Layout", "PositionPreset", SurveyorMap.PositionPreset.BottomLeft, "Screen corner for the minimap.");
            Scale = config.Bind("Layout", "Scale", 1.0f, new ConfigDescription("Overall minimap scale.", new AcceptableValueRange<float>(0.5f, 2.0f)));
            Opacity = config.Bind("Layout", "Opacity", 0.85f, new ConfigDescription("Overall HUD opacity.", new AcceptableValueRange<float>(0.1f, 1.0f)));
            Zoom = config.Bind("Map", "Zoom", 2.25f, new ConfigDescription("World-to-minimap zoom.", new AcceptableValueRange<float>(0.5f, 8.0f)));
            RotateWithPlayer = config.Bind("Map", "RotateWithPlayer", false, "Rotate minimap content around the local player's yaw.");
            RevealRooms = config.Bind("Reveal", "RevealRooms", true, "Draw SurveyorMap's own room overlay according to RevealMode.");
            RevealMode = config.Bind("Reveal", "RevealMode", SurveyorMap.RevealMode.FullReveal, "Room reveal behavior for the SurveyorMap overlay.");
            ShowSelfMarker = config.Bind("Markers", "ShowSelfMarker", true, "Show the local player marker.");
            ShowTeammates = config.Bind("Markers", "ShowTeammates", true, "Show teammate markers.");
            ShowEnemies = config.Bind("Enemies", "ShowEnemies", true, "Show enemy markers on the minimap overlay.");
            EnemyDetectionMode = config.Bind("Enemies", "EnemyDetectionMode", SurveyorMap.EnemyDetectionMode.NearbyOnly, "Enemy scan mode.");
            EnemyDetectionRadius = config.Bind("Enemies", "EnemyDetectionRadius", 25f, new ConfigDescription("Radius for NearbyOnly enemy detection.", new AcceptableValueRange<float>(5f, 100f)));
            EnemyUpdateInterval = config.Bind("Enemies", "EnemyUpdateInterval", 0.5f, new ConfigDescription("Seconds between enemy scans.", new AcceptableValueRange<float>(0.25f, 5f)));
            EnemyMarkerStyle = config.Bind("Enemies", "EnemyMarkerStyle", SurveyorMap.EnemyMarkerStyle.WarningOnly, "Visual style for enemy markers.");
            EnableSpectatorMode = config.Bind("General", "EnableSpectatorMode", true, "Keep the minimap visible while spectating.");
            SafeMode = config.Bind("General", "SafeMode", true, "Disable only failing features instead of throwing repeated errors.");
            LogLevel = config.Bind("Diagnostics", "LogLevel", LogLevelSetting.Info, "Log verbosity.");
            ShowBorder = config.Bind("Layout", "ShowBorder", true, "Show the minimap border.");
            BorderOpacity = config.Bind("Layout", "BorderOpacity", 0.9f, new ConfigDescription("Border opacity.", new AcceptableValueRange<float>(0f, 1f)));
            BorderThickness = config.Bind("Layout", "BorderThickness", 3f, new ConfigDescription("Border thickness in pixels.", new AcceptableValueRange<float>(1f, 8f)));
            BackgroundOpacity = config.Bind("Layout", "BackgroundOpacity", 0.55f, new ConfigDescription("Background opacity behind the native map texture.", new AcceptableValueRange<float>(0f, 1f)));
            MapOpacity = config.Bind("Layout", "MapOpacity", 0.78f, new ConfigDescription("Opacity of the captured native map texture.", new AcceptableValueRange<float>(0.1f, 1f)));
            SoftFrame = config.Bind("Layout", "SoftFrame", true, "Use a darker soft overlay and cleaner frame styling.");
            ShowNativeQuestionMarks = config.Bind("Visual", "ShowNativeQuestionMarks", false, "Show native question-mark map icons. Disable for a cleaner minimap.");
            HideInLobby = config.Bind("Visibility", "HideInLobby", true, "Hide the minimap in lobby scenes.");
            HideInShop = config.Bind("Visibility", "HideInShop", true, "Hide the minimap in shop scenes.");
            HideWhenInventoryOpen = config.Bind("Visibility", "HideWhenInventoryOpen", false, "Hide when the inventory UI appears to be open.");
            EnableNames = config.Bind("Markers", "EnableNames", false, "Show teammate names next to markers.");
            ClampMarkersToBounds = config.Bind("Markers", "ClampMarkersToBounds", true, "Clamp off-map markers to the minimap bounds.");
            MaxEnemyMarkers = config.Bind("Enemies", "MaxEnemyMarkers", 20, new ConfigDescription("Maximum enemy markers shown at once.", new AcceptableValueRange<int>(0, 100)));
            DebugEnemyTelemetry = config.Bind("Diagnostics", "DebugEnemyTelemetry", false, "Log detailed enemy scan telemetry: name, type, positions, distance, alive/active/renderer state, and accept/reject reason.");
            EnableDebugOverlay = config.Bind("Diagnostics", "EnableDebugOverlay", false, "Show a small debug status line under the minimap.");
            UpdateRate = config.Bind("Performance", "UpdateRate", 0.05f, new ConfigDescription("Seconds between visual HUD updates.", new AcceptableValueRange<float>(0.02f, 0.5f)));
            CaptureFPS = config.Bind("Performance", "CaptureFPS", 5f, new ConfigDescription("Maximum native map RenderTexture captures per second.", new AcceptableValueRange<float>(1f, 30f)));
            CaptureOnlyWhenVisible = config.Bind("Performance", "CaptureOnlyWhenVisible", true, "Capture the native map only when the HUD gate is open.");
            PauseCaptureWhenToggleOff = config.Bind("Performance", "PauseCaptureWhenToggleOff", true, "Pause native map capture while the minimap is toggled off.");
            RenderTextureSize = config.Bind("Performance", "RenderTextureSize", 256, new ConfigDescription("SurveyorMap capture RenderTexture size.", new AcceptableValueList<int>(128, 256, 512)));
            EnableProfiling = config.Bind("Performance", "EnableProfiling", false, "Log lightweight SurveyorMap runtime profiling metrics.");
            ProfilingLogInterval = config.Bind("Performance", "ProfilingLogInterval", 5f, new ConfigDescription("Seconds between profiling summaries.", new AcceptableValueRange<float>(2f, 30f)));
            RenderWarningMs = config.Bind("Performance", "RenderWarningMs", 4f, new ConfigDescription("Warn and auto-throttle when map camera rendering exceeds this many milliseconds on average.", new AcceptableValueRange<float>(1f, 25f)));
            AutoThrottleCapture = config.Bind("Performance", "AutoThrottleCapture", true, "Temporarily reduce capture rate when rendering is too expensive or frame time spikes.");
            MarkerScale = config.Bind("Markers", "MarkerScale", 1.0f, new ConfigDescription("Marker size multiplier.", new AcceptableValueRange<float>(0.5f, 2.5f)));
            UseSquareMap = config.Bind("Layout", "UseSquareMap", true, "Use a square minimap. If false, use a wider panel.");
            PosX = config.Bind("Layout", "PosX", 24f, new ConfigDescription("Custom minimap X position in screen pixels from the bottom-left anchor.", new AcceptableValueRange<float>(0f, 4096f)));
            PosY = config.Bind("Layout", "PosY", 120f, new ConfigDescription("Custom minimap Y position in screen pixels from the bottom-left anchor.", new AcceptableValueRange<float>(0f, 4096f)));
            Width = config.Bind("Layout", "Width", 260f, new ConfigDescription("Minimap width before scale.", new AcceptableValueRange<float>(160f, 640f)));
            Height = config.Bind("Layout", "Height", 260f, new ConfigDescription("Minimap height before scale.", new AcceptableValueRange<float>(140f, 640f)));
            CenterOnPlayer = config.Bind("Map", "CenterOnPlayer", true, "Keep the local player marker centered and move the map around it.");
            SmoothShapes = config.Bind("Map", "SmoothShapes", false, "Reserved for smoother room rendering in a future visual pass.");
            ForceHudProofOfLife = config.Bind("Diagnostics", "ForceHudProofOfLife", true,
                "DIAGNOSTIC: draw a bare 260x260 cyan-border overlay in the level via OnGUI, " +
                "bypassing all capture gates. Disable once the minimap is working correctly.");
            _layoutDefaultsVersion = config.Bind("Migration", "LayoutDefaultsVersion", 0, "Internal migration marker for SurveyorMap layout defaults.");
        }

        public void MigrateLayoutDefaults()
        {
            if (_layoutDefaultsVersion.Value >= 7)
                return;

            if (_layoutDefaultsVersion.Value < 1 && PositionPreset.Value == SurveyorMap.PositionPreset.TopRight)
            {
                PositionPreset.Value = SurveyorMap.PositionPreset.BottomLeft;
                LogHelper.Info("Migrated PositionPreset from previous TopRight default to BottomLeft.");
            }

            PositionPreset.Value = SurveyorMap.PositionPreset.BottomLeft;
            Scale.Value = 1f;
            UseSquareMap.Value = true;
            PosX.Value = 24f;
            PosY.Value = 120f;
            Width.Value = 260f;
            Height.Value = 260f;
            RotateWithPlayer.Value = false;
            CenterOnPlayer.Value = false;
            BackgroundOpacity.Value = 0.55f;
            BorderOpacity.Value = 0.85f;
            BorderThickness.Value = 3f;
            MapOpacity.Value = 0.78f;
            SoftFrame.Value = true;
            // Show native question marks so TAB map looks unmodified
            ShowNativeQuestionMarks.Value = true;
            // Disable room overlay and enemy markers — native map mirror mode
            RevealRooms.Value = false;
            RevealMode.Value = SurveyorMap.RevealMode.Off;
            ShowEnemies.Value = false;
            EnemyDetectionMode.Value = SurveyorMap.EnemyDetectionMode.Off;
            EnemyUpdateInterval.Value = 0.5f;
            EnableProfiling.Value = false;
            LogHelper.Info("NativeMapMirror defaults applied: overlay=off, enemies=off, centerOnPlayer=off, questionMarks=on.");

            _layoutDefaultsVersion.Value = 7;
        }
    }

    public sealed class SurveyorMapController : MonoBehaviour
    {
        private const float PlayerRefreshInterval = 0.25f;
        private const float InventoryRefreshInterval = 0.25f;
        private const float UpdateHeartbeatInterval = 5f;

        private readonly List<PlayerAvatar> _players = new List<PlayerAvatar>(8);
        private readonly List<Enemy> _enemies = new List<Enemy>(32);
        private readonly MarkerPool _teammatePool = new MarkerPool();
        private readonly MarkerPool _enemyPool = new MarkerPool();
        private readonly NativeMapCaptureProvider _nativeMapCapture = new NativeMapCaptureProvider();
        private readonly SurveyorRoomMapCache _roomMapCache = new SurveyorRoomMapCache();

        private SurveyorMapConfig _config;
        private CapabilityReport _capabilities;
        private Canvas _canvas;
        private RectTransform _mapRoot;
        private RectTransform _clipRoot;
        private RectTransform _contentRoot;
        private RectTransform _markerRoot;
        private Image _background;
        private Image _mapShade;
        private SurveyorRoomOverlayGraphic _roomOverlay;
        private HudTextureView _hudTexture;
        // _mapViewPivot: reserved for future player-centering feature; not yet created in CreateHud.
        private Image[] _borderLines;
        private CanvasGroup _canvasGroup;
        private MarkerView _selfMarker;
        private Text _debugText;
        private Font _font;
        private bool _visibleByToggle = true;
        private bool _teammatesEnabled;
        private bool _enemiesEnabled;
        private bool _spectatorEnabled;
        private bool _inventoryVisible;
        private float _nextVisualUpdate;
        private float _nextCaptureUpdate;
        private float _nextEnemyUpdate;
        private float _nextRevealAttempt;
        private float _nextInventoryUpdate;
        private Type _inventoryUiType;
        private string _lastDebugText;
        private PositionPreset _lastPositionPreset;
        private bool _lastUseSquareMap;
        private bool _lastShowBorder;
        private float _lastPosX = -1f;
        private float _lastPosY = -1f;
        private float _lastWidth = -1f;
        private float _lastHeight = -1f;
        private bool _lastHudVisibility;
        private bool _hasHudVisibilityLog;
        private bool _lastLocalPlayerPresent;
        private bool _hasLocalPlayerLog;
        private bool _lastPlayerApiReady;
        private bool _hasPlayerApiReadyLog;
        private int _lastRuntimeUpdateFrame = -1;
        private float _nextUpdateHeartbeat;
        private string _lastRunStateSummary;
        private bool _hasNativeCaptureLog;
        private string _lastNativeCaptureSummary;
        private float _nextNativeCaptureSummary;
        private string _lastVisibleGateSummary;
        private bool _nativeTextureReady;
        private float _lastScale = -1f;
        private float _lastOpacity = -1f;
        private float _lastBorderOpacity = -1f;
        private float _lastBackgroundOpacity = -1f;
        private float _lastBorderThickness = -1f;
        private float _lastMapOpacity = -1f;
        private bool _lastSoftFrame;
        private float _nextHierarchyAudit;
        private string _lastHierarchySummary;
        private bool _lastGateOpenForCapture;
        private int _capturesThisWindow;
        private int _captureSkipsThisWindow;
        private float _profileWindowStart;
        private float _nextProfilingLog;
        private float _frameMsAverage;
        private float _captureIntervalMultiplier = 1f;
        private float _autoThrottleUntil;
        private long _lastGcBytes;
        private string _lastProfileSummary;
        private int _lastRevealLevelId = -1;
        private bool _revealAppliedForLevel;
        private bool _loggedRevealNativeLimit;
        private string _lastRevealSummary;
        private string _lastRoomOverlaySummary;
        private bool _lastRoomOverlayDirty;
        private string _lastEnemySummary;
        private string _lastEnemyStableKey;
        private float _nextNoisyEnemyLog;
        private const float NoisyEnemyLogCooldown = 8f;
        private EnemyScanStats _lastEnemyScanStats;
        private bool _hasEnemyScanStats;
        private bool _baseMapTextureVisible;
        private bool _baseMapTextureReady;
        private bool _enemiesDisabledByError;

        public bool IsInitialized { get; private set; }

        public bool HudCreated
        {
            get { return _canvas != null; }
        }

        public bool HudActive
        {
            get { return _canvas != null && _canvas.gameObject.activeSelf; }
        }

        internal void Initialize(CapabilityReport report)
        {
            if (IsInitialized && _canvas != null)
                return;

            if (_canvas != null)
                Destroy(_canvas.gameObject);

            _config = SurveyorMapPlugin.Settings;
            _capabilities = report;
            _teammatesEnabled = false;
            _enemiesEnabled = false;
            _spectatorEnabled = _capabilities.Spectator && _config.EnableSpectatorMode.Value;
            _font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            _inventoryUiType = GameApi.FindType("InventoryUI");

            LogHelper.Info("Controller Initialize. Creating inactive HUD. toggleKey=" + _config.ToggleKey.Value);
            CreateHud();
            LogHelper.Info("Feature summary: self=" + Enabled(_config.ShowSelfMarker.Value) +
                           ", teammates=" + Enabled(_teammatesEnabled) +
                           ", enemies=" + Enabled(_enemiesEnabled) +
                           ", spectator=" + Enabled(_spectatorEnabled) +
                           ", safeMode=" + Enabled(_config.SafeMode.Value));

            IsInitialized = true;
        }

        private void Start()
        {
            LogHelper.Info("Controller Start. controller=" + gameObject.name +
                           ", hudCreated=" + (_canvas != null) +
                           ", hudActive=" + (_canvas != null && _canvas.gameObject.activeSelf));
            ApplyVisibility(false, GameApi.GetRunState(_config));
        }

        private void Update()
        {
            RuntimeUpdate("controller");
        }

        public void RuntimeUpdate(string source)
        {
            try
            {
                RuntimeUpdateCore(source);
            }
            catch (Exception ex)
            {
                // ex.ToString() includes type, message AND full stack trace.
                LogHelper.RateLimitedError("runtime-update", "RuntimeUpdate exception:\n" + ex.ToString(), 2f);
            }
        }

        private void RuntimeUpdateCore(string source)
        {
            if (_config == null || _canvas == null || _mapRoot == null || _hudTexture == null)
                return;

            if (_lastRuntimeUpdateFrame == Time.frameCount)
                return;

            _lastRuntimeUpdateFrame = Time.frameCount;

            float now = Time.unscaledTime;
            UpdateFrameProfiler(now);
            if (now >= _nextUpdateHeartbeat)
            {
                _nextUpdateHeartbeat = now + UpdateHeartbeatInterval;
                LogHelper.Debug("Update heartbeat via " + source +
                                ". frame=" + Time.frameCount +
                                ", controllerEnabled=" + enabled +
                                ", controllerActive=" + gameObject.activeInHierarchy +
                                ", hudCreated=" + (_canvas != null) +
                                ", hudActive=" + (_canvas != null && _canvas.gameObject.activeSelf));
            }

            RunState runState = GameApi.GetRunState(_config);
            LogRunStateIfChanged(runState);
            LogPlayerApiReadyIfChanged(runState);
            LogVisibleGateIfChanged(runState);

            if (_config.HideWhenInventoryOpen.Value && now >= _nextInventoryUpdate)
            {
                _nextInventoryUpdate = now + InventoryRefreshInterval;
                _inventoryVisible = GameApi.IsInventoryVisible(_inventoryUiType);
            }

            if (now < _nextVisualUpdate)
                return;

            _nextVisualUpdate = now + Mathf.Max(0.02f, _config.UpdateRate.Value);
            RefreshVisuals(now, runState);
        }

        public void ToggleFromRuntimeLoop(KeyCode key)
        {
            if (_config == null)
                return;

            bool visibleBefore = _visibleByToggle;
            _visibleByToggle = !_visibleByToggle;
            ApplyVisibility(false, GameApi.GetRunState(_config));
            LogHelper.Info("HUD toggle applied. visibleBefore=" + visibleBefore +
                           ", visibleByToggle=" + _visibleByToggle +
                           ", hudActive=" + (_canvas != null && _canvas.gameObject.activeSelf));
        }

        private void OnDestroy()
        {
            IsInitialized = false;
            if (_nativeMapCapture != null)
                _nativeMapCapture.ReleaseRuntimeState();

            if (_canvas != null)
                Destroy(_canvas.gameObject);
        }

        private void CreateHud()
        {
            GameObject canvasObject = new GameObject("SurveyorMap.HUD");
            DontDestroyOnLoad(canvasObject);
            _canvas = canvasObject.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 1100;
            canvasObject.AddComponent<CanvasScaler>();
            canvasObject.AddComponent<GraphicRaycaster>();

            _canvasGroup = canvasObject.AddComponent<CanvasGroup>();
            _canvasGroup.alpha = Mathf.Clamp01(_config.Opacity.Value);

            GameObject rootObject = new GameObject("SurveyorMap.Panel");
            rootObject.transform.SetParent(canvasObject.transform, false);
            _mapRoot = rootObject.AddComponent<RectTransform>();

            _background = CreateImage("Background", _mapRoot, new Color(0.02f, 0.03f, 0.04f, GetVisibleBackgroundOpacity()));
            _background.raycastTarget = false;
            Stretch(_background.rectTransform);

            GameObject clipObject = new GameObject("ClipRoot");
            clipObject.transform.SetParent(_mapRoot, false);
            _clipRoot = clipObject.AddComponent<RectTransform>();
            clipObject.AddComponent<RectMask2D>();
            Stretch(_clipRoot);

            // Content root — stretched to fill the clip window; all map content lives here.
            // Note: _mapViewPivot is reserved for future player-centering; currently unused.
            GameObject contentObject = new GameObject("MapContent");
            contentObject.transform.SetParent(_clipRoot, false);
            _contentRoot = contentObject.AddComponent<RectTransform>();
            Stretch(_contentRoot);

            _hudTexture = HudTextureView.Create(_contentRoot);

            _mapShade = CreateImage("MapShade", _contentRoot, Color.clear);
            _mapShade.raycastTarget = false;
            Stretch(_mapShade.rectTransform);

            GameObject roomOverlayObject = new GameObject("RoomOverlay");
            roomOverlayObject.transform.SetParent(_contentRoot, false);
            _roomOverlay = roomOverlayObject.AddComponent<SurveyorRoomOverlayGraphic>();
            _roomOverlay.raycastTarget = false;
            Stretch(_roomOverlay.rectTransform);

            GameObject markerObject = new GameObject("MarkerRoot");
            markerObject.transform.SetParent(_contentRoot, false);
            _markerRoot = markerObject.AddComponent<RectTransform>();
            Stretch(_markerRoot);

            _borderLines = CreateBorder(_mapRoot);

            _selfMarker = MarkerView.Create(_markerRoot, _font, "SelfMarker", new Color(0.4f, 1f, 0.95f, 1f));

            GameObject debugObject = new GameObject("Debug");
            debugObject.transform.SetParent(_mapRoot, false);
            _debugText = debugObject.AddComponent<Text>();
            _debugText.font = _font;
            _debugText.fontSize = 10;
            _debugText.alignment = TextAnchor.UpperLeft;
            _debugText.color = new Color(0.8f, 1f, 0.9f, 0.85f);
            _debugText.raycastTarget = false;
            RectTransform debugRect = _debugText.rectTransform;
            debugRect.anchorMin = new Vector2(0f, 0f);
            debugRect.anchorMax = new Vector2(1f, 0f);
            debugRect.pivot = new Vector2(0f, 1f);
            debugRect.anchoredPosition = new Vector2(4f, -4f);
            debugRect.sizeDelta = new Vector2(0f, 18f);

            ResetLayoutState();
            ApplyLayout();
            _mapRoot.gameObject.SetActive(false);
            _canvas.gameObject.SetActive(false);
            LogHelper.Info("HUD canvas created. renderMode=" + _canvas.renderMode +
                           ", sortingOrder=" + _canvas.sortingOrder +
                           ", panelSize=" + _mapRoot.sizeDelta +
                           ", panelAnchor=" + _mapRoot.anchorMin +
                           ", active=" + _canvas.gameObject.activeSelf);
        }

        private void ApplyLayout()
        {
            if (_mapRoot == null)
                return;

            if (!LayoutDirty())
                return;

            float scale = Mathf.Clamp(_config.Scale.Value, 0.5f, 2f);
            float width = Mathf.Clamp(_config.Width.Value, 160f, 640f);
            float height = Mathf.Clamp(_config.Height.Value, 140f, 640f);
            if (_config.UseSquareMap.Value)
            {
                float size = Mathf.Min(width, height);
                width = size;
                height = size;
            }
            _mapRoot.sizeDelta = new Vector2(width * scale, height * scale);

            Vector2 anchor = new Vector2(0f, 0f);
            Vector2 pivot = new Vector2(0f, 0f);
            Vector2 offset = new Vector2(24f, 120f);

            if (_config.PositionPreset.Value == PositionPreset.TopLeft)
            {
                anchor = new Vector2(0f, 1f);
                pivot = new Vector2(0f, 1f);
                offset = new Vector2(18f, -18f);
            }
            else if (_config.PositionPreset.Value == PositionPreset.BottomRight)
            {
                anchor = new Vector2(1f, 0f);
                pivot = new Vector2(1f, 0f);
                offset = new Vector2(-18f, 18f);
            }
            else if (_config.PositionPreset.Value == PositionPreset.BottomLeft)
            {
                anchor = new Vector2(0f, 0f);
                pivot = new Vector2(0f, 0f);
                offset = new Vector2(24f, 120f);
            }
            else if (_config.PositionPreset.Value == PositionPreset.TopRight)
            {
                anchor = new Vector2(1f, 1f);
                pivot = new Vector2(1f, 1f);
                offset = new Vector2(-18f, -18f);
            }
            else if (_config.PositionPreset.Value == PositionPreset.Custom)
            {
                anchor = new Vector2(0f, 0f);
                pivot = new Vector2(0f, 0f);
                offset = new Vector2(_config.PosX.Value, _config.PosY.Value);
            }

            _mapRoot.anchorMin = anchor;
            _mapRoot.anchorMax = anchor;
            _mapRoot.pivot = pivot;
            _mapRoot.anchoredPosition = offset;
            _canvasGroup.alpha = Mathf.Clamp01(_config.Opacity.Value);
            _background.color = new Color(0.02f, 0.03f, 0.04f, GetVisibleBackgroundOpacity());
            if (_hudTexture != null)
                _hudTexture.SetOpacity(Mathf.Clamp01(_config.MapOpacity.Value));
            if (_mapShade != null)
                _mapShade.color = _config.SoftFrame.Value ? new Color(0f, 0f, 0f, 0.18f) : Color.clear;
            ApplyBorder();
            SaveLayoutState();
            LogHelper.Info("Layout applied: PositionPreset=" + _config.PositionPreset.Value +
                           ", anchorMin=" + _mapRoot.anchorMin +
                           ", anchorMax=" + _mapRoot.anchorMax +
                           ", pivot=" + _mapRoot.pivot +
                           ", anchoredPosition=" + _mapRoot.anchoredPosition +
                           ", size=" + _mapRoot.sizeDelta);
        }

        private void ApplyVisibility(bool cheap, RunState runState)
        {
            if (_canvas == null)
                return;

            bool playerReady = runState.PlayerApiReady && runState.HasLocalPlayer;
            bool shouldShow = _config.EnableMinimap.Value &&
                              _visibleByToggle &&
                              runState.Allowed &&
                              playerReady &&
                              _nativeTextureReady;
            if (shouldShow && _config.HideWhenInventoryOpen.Value && _inventoryVisible)
                shouldShow = false;

            if (!shouldShow && cheap)
            {
                _canvas.gameObject.SetActive(false);
                if (_mapRoot != null)
                    _mapRoot.gameObject.SetActive(false);
                if (!_visibleByToggle || !runState.Allowed || !runState.PlayerApiReady)
                    _nativeMapCapture.ReleaseRuntimeState();
                _nativeTextureReady = false;
                _baseMapTextureReady = false;
                _baseMapTextureVisible = false;
                if (_hudTexture != null)
                    _hudTexture.ClearTexture();
                if (_selfMarker != null)
                    _selfMarker.SetActive(false);
                if (_roomOverlay != null)
                    _roomOverlay.ClearRooms();
                LogHudVisibilityIfChanged(false, runState);
                return;
            }

            if (_canvas.gameObject.activeSelf != shouldShow)
            {
                if (shouldShow)
                    LogHelper.Info("HUD activation start. canvasActive=" + _canvas.gameObject.activeSelf +
                                   ", panelActive=" + (_mapRoot != null && _mapRoot.gameObject.activeSelf) +
                                   ", rawImage=" + (_hudTexture != null ? _hudTexture.DescribeState() : "null"));

                _canvas.gameObject.SetActive(shouldShow);
                if (_mapRoot != null)
                    _mapRoot.gameObject.SetActive(shouldShow);
                if (!shouldShow)
                {
                    _nativeMapCapture.ReleaseRuntimeState();
                    _nativeTextureReady = false;
                    _baseMapTextureReady = false;
                    _baseMapTextureVisible = false;
                    if (_hudTexture != null)
                        _hudTexture.ClearTexture();
                    if (_selfMarker != null)
                        _selfMarker.SetActive(false);
                    if (_roomOverlay != null)
                        _roomOverlay.ClearRooms();
                }
                else
                {
                    LogHelper.Info("HUD activation success. hudActive=" + HudActive +
                                   ", canvasActive=" + _canvas.gameObject.activeSelf +
                                   ", panelActive=" + (_mapRoot != null && _mapRoot.gameObject.activeSelf) +
                                   ", rawImage=" + (_hudTexture != null ? _hudTexture.DescribeState() : "null"));
                }
            }

            LogHudVisibilityIfChanged(shouldShow, runState);
            if (!shouldShow)
                LogHierarchySnapshot("hidden-gate", runState, null);
        }

        private void RefreshPlayers()
        {
            if (!_capabilities.Teammates && !_config.ShowSelfMarker.Value)
                return;

            try
            {
                _players.Clear();
                GameApi.FillPlayers(_players);
                PlayerAvatar local = GameApi.GetLocalPlayer();
                bool localFound = local != null;
                if (!_hasLocalPlayerLog || _lastLocalPlayerPresent != localFound)
                {
                    _hasLocalPlayerLog = true;
                    _lastLocalPlayerPresent = localFound;
                    LogHelper.Info("PlayerAvatarLocal " + (localFound ? "found" : "not found") +
                                   ". players=" + _players.Count);
                }
            }
            catch (Exception ex)
            {
                if (_config.SafeMode.Value)
                {
                    _teammatesEnabled = false;
                    LogHelper.RateLimitedWarning("players", "Player markers disabled: " + ex.Message, 10f);
                }
                else
                {
                    throw;
                }
            }
        }

        private void LogRunStateIfChanged(RunState runState)
        {
            string summary = runState.ToSummary();
            string key = runState.Allowed
                ? summary
                : "RunState=" + runState.Name +
                  ", RunIsLobby=" + runState.InLobby +
                  ", RunIsShop=" + runState.InShop +
                  ", MenuLevel=" + runState.MenuLevel +
                  ", RunIsLevel=" + runState.RunIsLevel +
                  ", PlayerAvatarLocal=" + runState.HasLocalPlayer +
                  ", levelGenDone=" + runState.LevelGenDone +
                  ", levelGenerated=" + runState.LevelGenerated +
                  ", playerApiReady=" + runState.PlayerApiReady +
                  ", allowed=" + runState.Allowed +
                  ", reason=" + runState.Reason;

            if (_lastRunStateSummary == key)
                return;

            _lastRunStateSummary = key;
            LogHelper.Info("Run state: " + summary);
        }

        private void LogPlayerApiReadyIfChanged(RunState runState)
        {
            if (_hasPlayerApiReadyLog && _lastPlayerApiReady == runState.PlayerApiReady)
                return;

            _hasPlayerApiReadyLog = true;
            _lastPlayerApiReady = runState.PlayerApiReady;
            LogHelper.Info("Player API ready=" + runState.PlayerApiReady +
                           ", RunState=" + runState.Name +
                           ", PlayerAvatarLocal=" + runState.HasLocalPlayer +
                           ", levelObjects=" + runState.HasLevelObjects);
        }

        private void LogVisibleGateIfChanged(RunState runState)
        {
            string summary = "VisibleGate: playerApiReady=" + runState.PlayerApiReady +
                             ", playerLocal=" + runState.HasLocalPlayer +
                             ", toggle=" + _visibleByToggle +
                             ", enableMinimap=" + _config.EnableMinimap.Value;
            if (_lastVisibleGateSummary == summary)
                return;

            _lastVisibleGateSummary = summary;
            LogHelper.Info(summary);
        }

        private void LogHudVisibilityIfChanged(bool visible, RunState runState)
        {
            if (_hasHudVisibilityLog && _lastHudVisibility == visible)
                return;

            _hasHudVisibilityLog = true;
            _lastHudVisibility = visible;
            LogHelper.Info("HUD visible=" + visible +
                           ", reason=" + (_visibleByToggle ? runState.Name : "toggle-off") +
                           ", toggle=" + _visibleByToggle +
                           ", playerReady=" + (runState.PlayerApiReady && runState.HasLocalPlayer) +
                           ", enableMinimap=" + _config.EnableMinimap.Value +
                           ", allowed=" + runState.Allowed +
                           ", nativeTextureReady=" + _nativeTextureReady +
                           ", canvasActive=" + (_canvas != null && _canvas.gameObject.activeSelf));
        }

        private void ClearPlayerMarkers()
        {
            _players.Clear();
        }

        private float GetVisibleBackgroundOpacity()
        {
            return Mathf.Clamp01(_config.BackgroundOpacity.Value);
        }

        private IEnumerator EnemyScanLoop()
        {
            while (true)
            {
                float wait = _config != null ? Mathf.Max(0.25f, _config.EnemyUpdateInterval.Value) : 0.75f;
                yield return new WaitForSeconds(wait);

                if (_config == null || !_config.ShowEnemies.Value || _config.EnemyDetectionMode.Value == EnemyDetectionMode.Off || !_capabilities.Enemies)
                {
                    _enemies.Clear();
                    continue;
                }

                try
                {
                    GameApi.FillEnemies(_enemies, GetCenterPosition(), _config.EnemyDetectionMode.Value, _config.EnemyDetectionRadius.Value, _config.MaxEnemyMarkers.Value, _config.DebugEnemyTelemetry.Value, ref _lastEnemyScanStats);
                    _enemiesEnabled = true;
                }
                catch (Exception ex)
                {
                    _enemies.Clear();
                    _enemiesEnabled = false;
                    if (_config.SafeMode.Value)
                        LogHelper.RateLimitedWarning("enemies", "Enemy markers disabled: " + ex.Message, 10f);
                    else
                        throw;
                }
            }
        }

        private void RefreshVisuals(float now, RunState runState)
        {
            ApplyLayout();

            bool gateOpenForCapture = IsHudGateOpenForCapture(runState);
            if (!gateOpenForCapture)
            {
                if (_lastGateOpenForCapture || _nativeTextureReady)
                    _nativeMapCapture.ReleaseRuntimeState();
                _lastGateOpenForCapture = false;
                _nativeTextureReady = false;
                _baseMapTextureReady = false;
                _baseMapTextureVisible = false;
                _enemiesEnabled = false;
                _enemies.Clear();
                if (_hudTexture != null)
                    _hudTexture.ClearTexture();
                if (_selfMarker != null)
                    _selfMarker.SetActive(false);
                if (_roomOverlay != null)
                    _roomOverlay.ClearRooms();
                _enemyPool.HideAll();
                ApplyVisibility(true, runState);
                UpdateDebug();
                return;
            }

            if (!_lastGateOpenForCapture)
                _nextCaptureUpdate = 0f;

            _lastGateOpenForCapture = true;

            if (ShouldCaptureNow(now))
                UpdateNativeMapTexture(runState, now);

            Vector3 center = GetCenterPosition();
            PlayerAvatar localPlayer = GameApi.GetLocalPlayer();
            float yaw = GetYaw(localPlayer);
            Vector2 playerHudPoint = ComputePlayerHudPoint(center);
            ApplyMapViewTransform(now, playerHudPoint, yaw);
            UpdateRoomMapOverlay(now, runState, center);
            UpdateEnemyCache(now);
            UpdateSelfMarkerOverlay(playerHudPoint);
            UpdateOverlayMarkers(now, center);
            ApplyVisibility(true, runState);
            UpdateDebug();
        }

        private bool IsHudGateOpenForCapture(RunState runState)
        {
            if (!runState.PlayerApiReady || !runState.Allowed)
                return false;

            if (!_visibleByToggle && _config.PauseCaptureWhenToggleOff.Value)
                return false;

            if (!_config.EnableMinimap.Value)
                return false;

            if (_config.CaptureOnlyWhenVisible.Value && _config.HideWhenInventoryOpen.Value && _inventoryVisible)
                return false;

            return true;
        }

        private bool ShouldCaptureNow(float now)
        {
            if (now < _nextCaptureUpdate)
            {
                _captureSkipsThisWindow++;
                return false;
            }

            float captureFps = Mathf.Clamp(_config.CaptureFPS.Value, 1f, 30f);
            _nextCaptureUpdate = now + (1f / captureFps) * Mathf.Max(1f, _captureIntervalMultiplier);
            return true;
        }

        private void UpdateNativeMapTexture(RunState runState, float now)
        {
            if (_hudTexture == null)
                return;

            NativeMapCapture capture = _nativeMapCapture.Capture(_config, runState);
            _capturesThisWindow++;
            bool mapPopulated = capture.MapModuleCount > 0 && capture.LayerCount > 1;
            // dig-style readiness: the texture comes straight from camera.activeTexture, so as soon
            // as it exists and we are in an allowed run state, the minimap is ready. We no longer
            // gate on NativeVisualHidden (we don't hide anymore) or mapPopulated (kept for logging).
            _nativeTextureReady = runState.Allowed &&
                                  capture.Texture != null &&
                                  capture.RenderResult == "render-ok";
            _baseMapTextureReady = _nativeTextureReady;
            _baseMapTextureVisible = capture.TextureHasVisiblePixels;

            if (_nativeTextureReady)
                _hudTexture.SetTexture(capture.Texture);
            else
                _hudTexture.ClearTexture();

            string stateSummary = "NativeMapCapture: ready=" + capture.Ready +
                                  ", visiblePixels=" + capture.TextureHasVisiblePixels +
                                  ", mapPopulated=" + mapPopulated +
                                  ", render=" + capture.RenderResult +
                                  ", camera=" + capture.CameraName +
                                  ", texture=" + capture.TextureDescription +
                                  ", source=" + capture.Source +
                                  ", mapActive=" + capture.MapActive +
                                  ", activeParent=" + capture.ActiveParentActive +
                                  ", nativeVisualHidden=" + capture.NativeVisualHidden +
                                  ", questionMarkersHidden=" + capture.NativeQuestionMarkersHidden +
                                  ", questionMarkers=" + capture.NativeQuestionMarkerCount +
                                  ", modules=" + capture.MapModuleCount +
                                  ", layers=" + capture.LayerCount +
                                  ", reason=" + capture.Reason;

            bool stateChanged = !_hasNativeCaptureLog || _lastNativeCaptureSummary != stateSummary;
            if (stateChanged)
            {
                _hasNativeCaptureLog = true;
                _lastNativeCaptureSummary = stateSummary;
                if (capture.Ready)
                    LogHelper.Info(stateSummary);
                else
                    LogHelper.RateLimitedWarning("native-map-capture", stateSummary, 5f);
            }
            else if (!capture.Ready)
            {
                LogHelper.RateLimitedWarning("native-map-capture", stateSummary, 10f);
            }
            else if (_config.EnableProfiling.Value && now >= _nextNativeCaptureSummary)
            {
                _nextNativeCaptureSummary = now + Mathf.Max(10f, _config.ProfilingLogInterval.Value);
                LogHelper.Info(stateSummary +
                               ", captureMs=" + capture.CaptureMs.ToString("0.00") +
                               ", renderMs=" + capture.RenderMs.ToString("0.00") +
                               ", renderAvgMs=" + capture.RenderAverageMs.ToString("0.00") +
                               ", rtCreated=" + capture.RenderTextureCreated);
            }

            LogHierarchySnapshot("native-capture", runState, capture);
            UpdateCaptureProfiler(now, capture);
        }

        private void UpdateSelfMarkerOverlay(Vector2 playerHudPoint)
        {
            if (_selfMarker == null)
                return;

            bool show = _config.ShowSelfMarker.Value && _nativeTextureReady;
            _selfMarker.SetActive(show);
            if (!show)
                return;

            // Self marker always at center (player-centered pan is disabled for stability).
            _selfMarker.SetPosition(Vector2.zero);
            _selfMarker.SetSize(9f * Mathf.Clamp(_config.MarkerScale.Value, 0.5f, 2.5f));
            _selfMarker.SetColor(new Color(0.35f, 1f, 0.9f, 0.98f));
            _selfMarker.SetRotation(45f);
            _selfMarker.SetName(null);
        }

        private Vector2 ComputePlayerHudPoint(Vector3 playerPos)
        {
            if (!_config.CenterOnPlayer.Value || _contentRoot == null)
                return Vector2.zero;

            Vector2 point;
            if (_nativeMapCapture.TryProjectWorldToHud(playerPos, _contentRoot, false, out point))
                return point;

            return Vector2.zero;
        }

        private void ApplyMapViewTransform(float now, Vector2 playerHudPoint, float yaw)
        {
            // Player-centered view is disabled for native-map-mirror stability.
            // Reset any residual pan/rotation to keep the content anchored.
            if (_contentRoot == null)
                return;
            if (_contentRoot.anchoredPosition != Vector2.zero)
                _contentRoot.anchoredPosition = Vector2.zero;
            if (_contentRoot.localEulerAngles != Vector3.zero)
                _contentRoot.localEulerAngles = Vector3.zero;
        }

        private void ApplyRevealRooms(float now)
        {
            if (!_config.RevealRooms.Value || _config.RevealMode.Value == RevealMode.Off)
                return;

            if (!_loggedRevealNativeLimit)
            {
                _loggedRevealNativeLimit = true;
                LogHelper.Info("RevealRooms uses native RoomVolume.SetExplored; native code hides room-volume MapModules and does not rebuild floor/wall geometry.");
            }

            if (now < _nextRevealAttempt)
                return;

            int levelId = GameApi.GetLevelGeneratorId();
            if (levelId != _lastRevealLevelId)
            {
                _lastRevealLevelId = levelId;
                _revealAppliedForLevel = false;
            }

            if (_config.RevealMode.Value == RevealMode.OnLevelStart && _revealAppliedForLevel)
                return;

            _nextRevealAttempt = now + (_config.RevealMode.Value == RevealMode.FullReveal ? 3f : 30f);
            int explored;
            int total;
            if (!GameApi.TryRevealRooms(out explored, out total))
                return;

            _revealAppliedForLevel = true;
            string summary = "RevealRooms: mode=" + _config.RevealMode.Value +
                             ", exploredNow=" + explored +
                             ", total=" + total +
                             ", levelId=" + levelId;
            if (_lastRevealSummary == summary)
                return;

            _lastRevealSummary = summary;
            LogHelper.Info(summary);
        }

        private void UpdateRoomMapOverlay(float now, RunState runState, Vector3 playerPosition)
        {
            if (_roomOverlay == null)
                return;

            if (!_config.RevealRooms.Value || _config.RevealMode.Value == RevealMode.Off || !_nativeTextureReady)
            {
                _roomOverlay.ClearRooms();
                return;
            }

            int levelId = GameApi.GetLevelGeneratorId();
            bool cacheChanged = _roomMapCache.RefreshIfNeeded(levelId, now);
            bool explorationChanged = _roomMapCache.UpdateExploration(playerPosition, _config.RevealMode.Value);
            if (cacheChanged || explorationChanged || _lastRoomOverlayDirty)
            {
                RebuildRoomOverlay(runState, levelId);
                _lastRoomOverlayDirty = false;
            }
            else if (_roomOverlay.RoomCount == 0 && _roomMapCache.RoomCount > 0)
            {
                RebuildRoomOverlay(runState, levelId);
            }
        }

        private void RebuildRoomOverlay(RunState runState, int levelId)
        {
            _roomOverlay.BeginRooms();

            int projected = 0;
            for (int i = 0; i < _roomMapCache.RoomCount; i++)
            {
                SurveyorRoomData room = _roomMapCache.GetRoom(i);
                if (!room.Explored)
                    continue;

                Vector2 a;
                Vector2 b;
                Vector2 c;
                Vector2 d;
                if (!_nativeMapCapture.TryProjectWorldToHud(room.Corners[0], _roomOverlay.rectTransform, false, out a) ||
                    !_nativeMapCapture.TryProjectWorldToHud(room.Corners[1], _roomOverlay.rectTransform, false, out b) ||
                    !_nativeMapCapture.TryProjectWorldToHud(room.Corners[2], _roomOverlay.rectTransform, false, out c) ||
                    !_nativeMapCapture.TryProjectWorldToHud(room.Corners[3], _roomOverlay.rectTransform, false, out d))
                {
                    continue;
                }

                Color fill = RoomFillColor(room);
                Color border = RoomBorderColor(room);
                _roomOverlay.AddRoom(a, b, c, d, fill, border, room.Extraction || room.Truck ? 2.2f : 1.4f);
                projected++;
            }

            _roomOverlay.EndRooms();

            string summary = "RoomOverlay: levelId=" + levelId +
                             ", rooms=" + _roomMapCache.RoomCount +
                             ", explored=" + _roomMapCache.ExploredCount +
                             ", projected=" + projected +
                             ", mapObjects=" + _roomMapCache.MapObjectCount +
                             ", mapModules=" + _roomMapCache.MapModuleCount +
                             ", mode=" + _config.RevealMode.Value;
            if (_lastRoomOverlaySummary == summary)
                return;

            _lastRoomOverlaySummary = summary;
            LogHelper.Info(summary);
        }

        private static Color RoomFillColor(SurveyorRoomData room)
        {
            if (room.Truck)
                return new Color(0.15f, 0.55f, 0.62f, 0.30f);

            if (room.Extraction)
                return new Color(0.5f, 0.95f, 0.45f, 0.32f);

            if (room.StartRoom)
                return new Color(0.24f, 0.62f, 1f, 0.28f);

            return new Color(0.10f, 0.23f, 0.30f, 0.42f);
        }

        private static Color RoomBorderColor(SurveyorRoomData room)
        {
            if (room.Truck)
                return new Color(0.25f, 0.95f, 1f, 0.82f);

            if (room.Extraction)
                return new Color(0.65f, 1f, 0.55f, 0.90f);

            if (room.StartRoom)
                return new Color(0.42f, 0.82f, 1f, 0.82f);

            return new Color(0.18f, 0.90f, 1f, 0.58f);
        }

        private void UpdateEnemyCache(float now)
        {
            if (!_config.ShowEnemies.Value || _config.EnemyDetectionMode.Value == EnemyDetectionMode.Off || _enemiesDisabledByError)
            {
                _enemiesEnabled = false;
                _enemies.Clear();
                _enemyPool.HideAll();
                _lastEnemyScanStats.Reset();
                _lastEnemyScanStats.RenderedEnemyMarkers = 0;
                _hasEnemyScanStats = true;
                return;
            }

            if (now < _nextEnemyUpdate)
                return;

            _nextEnemyUpdate = now + Mathf.Max(0.25f, _config.EnemyUpdateInterval.Value);
            try
            {
                GameApi.FillEnemies(_enemies, GetCenterPosition(), _config.EnemyDetectionMode.Value, _config.EnemyDetectionRadius.Value, _config.MaxEnemyMarkers.Value, _config.DebugEnemyTelemetry.Value, ref _lastEnemyScanStats);
                _enemiesEnabled = _enemies.Count > 0;
                _hasEnemyScanStats = true;
            }
            catch (Exception ex)
            {
                _enemies.Clear();
                _enemiesEnabled = false;
                _lastEnemyScanStats.Reset();
                _lastEnemyScanStats.ExceptionCount = 1;
                _lastEnemyScanStats.LastException = ex.Message;
                _hasEnemyScanStats = true;
                if (_config.SafeMode.Value)
                {
                    _enemiesDisabledByError = true;
                    LogHelper.RateLimitedWarning("enemies", "Enemy markers disabled after scan failure: " + ex.Message, 10f);
                }
                else
                {
                    throw;
                }
            }
        }

        private void UpdateOverlayMarkers(float now, Vector3 center)
        {
            if (!_nativeTextureReady || _mapRoot == null || _markerRoot == null)
            {
                _enemyPool.HideAll();
                if (_hasEnemyScanStats)
                {
                    _lastEnemyScanStats.RenderedEnemyMarkers = 0;
                    TryLogEnemyScanSummary(now);
                }
                return;
            }

            float markerScale = Mathf.Clamp(_config.MarkerScale.Value, 0.5f, 2.5f);
            int rendered = UpdateEnemyMarkers(center, markerScale, now);
            if (_hasEnemyScanStats)
            {
                _lastEnemyScanStats.RenderedEnemyMarkers = rendered;
                TryLogEnemyScanSummary(now);
            }
        }

        private void UpdateFrameProfiler(float now)
        {
            if (_profileWindowStart <= 0f)
            {
                _profileWindowStart = now;
                _nextProfilingLog = now + Mathf.Max(2f, _config.ProfilingLogInterval.Value);
                _lastGcBytes = GC.GetTotalMemory(false);
            }

            float frameMs = Time.unscaledDeltaTime * 1000f;
            if (_frameMsAverage <= 0f)
                _frameMsAverage = frameMs;
            else
                _frameMsAverage = Mathf.Lerp(_frameMsAverage, frameMs, 0.05f);

            if (_config.AutoThrottleCapture.Value && now > _autoThrottleUntil && _captureIntervalMultiplier > 1f)
                _captureIntervalMultiplier = Mathf.Max(1f, _captureIntervalMultiplier * 0.5f);
        }

        private void UpdateCaptureProfiler(float now, NativeMapCapture capture)
        {
            bool renderSlow = capture.RenderAverageMs > Mathf.Max(1f, _config.RenderWarningMs.Value);
            bool frameSlow = _frameMsAverage > 28f;
            if (_config.AutoThrottleCapture.Value && capture.Ready && (renderSlow || frameSlow))
            {
                _captureIntervalMultiplier = Mathf.Min(8f, Mathf.Max(2f, _captureIntervalMultiplier * 2f));
                _autoThrottleUntil = now + 10f;
            }

            if (!_config.EnableProfiling.Value || now < _nextProfilingLog)
                return;

            float elapsed = Mathf.Max(0.1f, now - _profileWindowStart);
            float capturesPerSecond = _capturesThisWindow / elapsed;
            long gcBytes = GC.GetTotalMemory(false);
            long gcDelta = gcBytes - _lastGcBytes;
            string summary = "Profiling: captureMs(avg)=" + capture.CaptureAverageMs.ToString("0.00") +
                             ", renderMs(avg)=" + capture.RenderAverageMs.ToString("0.00") +
                             ", renderMs(last)=" + capture.RenderMs.ToString("0.00") +
                             ", capturesPerSec=" + capturesPerSecond.ToString("0.00") +
                             ", captures=" + _capturesThisWindow +
                             ", throttledSkips=" + _captureSkipsThisWindow +
                             ", throttleMultiplier=" + _captureIntervalMultiplier.ToString("0.0") +
                             ", frameMs(avg)=" + _frameMsAverage.ToString("0.00") +
                             ", gcDeltaKB=" + (gcDelta / 1024f).ToString("0.0") +
                             ", rt=" + capture.TextureDescription +
                             ", renderTextureCreates=" + capture.RenderTextureCreateCount +
                             ", cameraEnabled=" + capture.CameraEnabled +
                             ", cameraInfo=" + capture.CameraRenderInfo;

            if (_lastProfileSummary != summary)
            {
                _lastProfileSummary = summary;
                if (renderSlow)
                    LogHelper.RateLimitedWarning("profile-render-slow", summary, 5f);
                else
                    LogHelper.Info(summary);
            }

            _capturesThisWindow = 0;
            _captureSkipsThisWindow = 0;
            _profileWindowStart = now;
            _nextProfilingLog = now + Mathf.Max(2f, _config.ProfilingLogInterval.Value);
            _lastGcBytes = gcBytes;
        }

        private void LogHierarchySnapshot(string reason, RunState runState, NativeMapCapture? capture)
        {
            if (_config != null && !_config.EnableDebugOverlay.Value && !_config.EnableProfiling.Value)
                return;

            float now = Time.unscaledTime;
            if (now < _nextHierarchyAudit && capture.HasValue && capture.Value.Ready)
                return;

            _nextHierarchyAudit = now + 5f;
            string summary = "HUD hierarchy [" + reason + "]: " +
                             DescribeGameObject("canvas", _canvas != null ? _canvas.gameObject : null) + " | " +
                             DescribeRect("panel", _mapRoot) + " | " +
                             DescribeRect("clip", _clipRoot) + " | " +
                             (_hudTexture != null ? _hudTexture.Describe("rawImage") : "rawImage=null") + " | " +
                             DescribeRect("markerRoot", _markerRoot) + " | " +
                             "border=" + DescribeBorders() + " | " +
                             "canvasGroupAlpha=" + (_canvasGroup != null ? _canvasGroup.alpha.ToString("0.00") : "null") + " | " +
                             "sortingOrder=" + (_canvas != null ? _canvas.sortingOrder.ToString() : "null") + " | " +
                             "runState=" + runState.Name +
                             ", playerApiReady=" + runState.PlayerApiReady;

            if (capture.HasValue)
                summary += " | " + _nativeMapCapture.DescribeCameraAndTexture(capture.Value);

            if (_lastHierarchySummary == summary)
                return;

            _lastHierarchySummary = summary;
            LogHelper.Info(summary);
        }

        private static string DescribeGameObject(string label, GameObject obj)
        {
            if (obj == null)
                return label + "=null";

            Transform parent = obj.transform.parent;
            return label + "{id=" + obj.GetInstanceID() +
                   ", activeSelf=" + obj.activeSelf +
                   ", activeInHierarchy=" + obj.activeInHierarchy +
                   ", parent=" + (parent != null ? parent.name : "null") +
                   ", scene=" + SurveyorMapPlugin.SceneName(obj) + "}";
        }

        private static string DescribeRect(string label, RectTransform rect)
        {
            if (rect == null)
                return label + "=null";

            return DescribeGameObject(label, rect.gameObject) +
                   ", size=" + rect.rect.size +
                   ", anchored=" + rect.anchoredPosition;
        }

        private string DescribeBorders()
        {
            if (_borderLines == null)
                return "null";

            int active = 0;
            for (int i = 0; i < _borderLines.Length; i++)
            {
                if (_borderLines[i] != null && _borderLines[i].gameObject.activeInHierarchy)
                    active++;
            }

            return active + "/" + _borderLines.Length;
        }

        private Vector3 GetCenterPosition()
        {
            if (_spectatorEnabled && GameApi.IsSpectating())
                return GameApi.GetObservedPosition();

            PlayerAvatar local = GameApi.GetLocalPlayer();
            return GameApi.GetPlayerPosition(local, Vector3.zero);
        }

        private float GetYaw(PlayerAvatar local)
        {
            if (local == null)
                return 0f;

            Transform transform = local.playerTransform != null ? local.playerTransform : local.transform;
            return transform != null ? transform.eulerAngles.y : 0f;
        }

        private void UpdateSelfMarker(PlayerAvatar local, Vector3 center, float yaw, Vector2 halfSize, float markerScale)
        {
            if (!_config.ShowSelfMarker.Value || local == null)
            {
                _selfMarker.SetActive(false);
                return;
            }

            Vector2 point = MarkerPoint(GameApi.GetPlayerPosition(local, center), center, yaw, halfSize);
            _selfMarker.SetActive(true);
            _selfMarker.SetPosition(point);
            _selfMarker.SetSize(14f * markerScale);
            _selfMarker.SetColor(new Color(0.45f, 0.95f, 1f, 1f));
            _selfMarker.SetRotation(_config.RotateWithPlayer.Value ? 0f : -yaw);
            _selfMarker.SetName(null);
        }

        private void UpdateTeammateMarkers(PlayerAvatar local, Vector3 center, float yaw, Vector2 halfSize, float markerScale)
        {
            if (!_teammatesEnabled || !_config.ShowTeammates.Value)
            {
                _teammatePool.HideAll();
                return;
            }

            int count = 0;
            for (int i = 0; i < _players.Count; i++)
            {
                PlayerAvatar player = _players[i];
                if (player == null || player == local)
                    continue;

                MarkerView marker = _teammatePool.Get(_markerRoot, _font, count);
                Vector2 point = MarkerPoint(GameApi.GetPlayerPosition(player, center), center, yaw, halfSize);
                marker.SetActive(true);
                marker.SetPosition(point);
                marker.SetSize(10f * markerScale);
                marker.SetColor(GameApi.GetPlayerColor(player, i));
                marker.SetRotation(0f);
                marker.SetName(_config.EnableNames.Value ? GameApi.GetPlayerName(player) : null);
                count++;
            }

            _teammatePool.HideFrom(count);
        }

        private int UpdateEnemyMarkers(Vector3 center, float markerScale, float now)
        {
            if (!_enemiesEnabled || !_config.ShowEnemies.Value || _config.EnemyDetectionMode.Value == EnemyDetectionMode.Off)
            {
                _enemyPool.HideAll();
                return 0;
            }

            bool clamp = _config.ClampMarkersToBounds.Value;
            bool debugTelemetry = _config.DebugEnemyTelemetry.Value;
            bool trackStats = _hasEnemyScanStats;

            if (trackStats)
            {
                _lastEnemyScanStats.RenderedUnclamped = 0;
                _lastEnemyScanStats.RenderedClamped = 0;
                _lastEnemyScanStats.OffViewportBeforeClamp = 0;
                _lastEnemyScanStats.MarkerAtEdgeCount = 0;
            }

            StringBuilder debugSb = (debugTelemetry && _enemies.Count > 0) ? new StringBuilder() : null;
            int debugLogged = 0;

            int count = 0;
            for (int i = 0; i < _enemies.Count; i++)
            {
                Enemy enemy = _enemies[i];
                if (enemy == null)
                    continue;

                string posSource = null;
                Vector3 worldPos = debugTelemetry
                    ? GameApi.GetEnemyPositionWithSource(enemy, center, out posSource)
                    : GameApi.GetEnemyPosition(enemy, center);

                Vector2 point;
                bool wasClamped, wasOffViewport;
                Vector3 nativePos, viewportPos;

                if (!_nativeMapCapture.TryProjectWorldToHudEx(worldPos, _markerRoot, clamp,
                    out point, out wasClamped, out wasOffViewport, out nativePos, out viewportPos))
                {
                    if (trackStats)
                        _lastEnemyScanStats.RejectedProjection++;
                    continue;
                }

                if (trackStats)
                {
                    if (wasOffViewport)
                        _lastEnemyScanStats.OffViewportBeforeClamp++;
                    if (wasClamped)
                        _lastEnemyScanStats.RenderedClamped++;
                    else
                        _lastEnemyScanStats.RenderedUnclamped++;

                    // "At edge" = final viewport within 2px of [0,1] after projection
                    Rect r = _markerRoot != null ? _markerRoot.rect : Rect.zero;
                    float edgeThreshold = 4f;
                    if (Mathf.Abs(point.x - (-r.width * 0.5f)) < edgeThreshold ||
                        Mathf.Abs(point.x - (r.width * 0.5f)) < edgeThreshold ||
                        Mathf.Abs(point.y - (-r.height * 0.5f)) < edgeThreshold ||
                        Mathf.Abs(point.y - (r.height * 0.5f)) < edgeThreshold)
                        _lastEnemyScanStats.MarkerAtEdgeCount++;
                }

                MarkerView marker = _enemyPool.Get(_markerRoot, _font, count);
                EnemyDangerTier tier = GameApi.GetEnemyDangerTier(enemy);
                Color color = EnemyColor(tier, now);
                marker.SetActive(true);
                marker.SetPosition(point);

                float pulse = 1f;
                if (_config.EnemyMarkerStyle.Value == EnemyMarkerStyle.Pulse || tier == EnemyDangerTier.High)
                    pulse = 1f + Mathf.Sin(now * 7f) * 0.18f;

                marker.SetSize(12f * markerScale * pulse);
                marker.SetColor(color);
                marker.SetSprite(MarkerShapeSprites.ForDanger(tier));
                marker.SetRotation(tier == EnemyDangerTier.High ? 45f : 0f);
                marker.SetName(null);

                if (debugTelemetry && debugSb != null && debugLogged < 5)
                {
                    float dist = Vector3.Distance(worldPos, center);
                    EnemyParent ep = GameApi.GetEnemyParent(enemy);
                    string name = ep != null ? ep.enemyName : "?";
                    Sprite shape = MarkerShapeSprites.ForDanger(tier);
                    debugSb.AppendLine(string.Format(
                        "EnemyDebug[{0}]: name={1} tier={2} dist={3:F1} src={4} " +
                        "world=({5:F1},{6:F1},{7:F1}) vp=({8:F3},{9:F3}) hud=({10:F1},{11:F1}) " +
                        "clamped={12} offVp={13} edge={14} shape={15} color=({16:F2},{17:F2},{18:F2})",
                        count, name, tier, dist, posSource ?? "?",
                        worldPos.x, worldPos.y, worldPos.z,
                        viewportPos.x, viewportPos.y,
                        point.x, point.y,
                        wasClamped, wasOffViewport,
                        trackStats && _lastEnemyScanStats.MarkerAtEdgeCount > count,
                        shape != null ? shape.name : "null",
                        color.r, color.g, color.b));
                    debugLogged++;
                }

                count++;
            }

            _enemyPool.HideFrom(count);

            if (debugTelemetry && debugSb != null && debugSb.Length > 0)
                LogHelper.Info(debugSb.ToString());

            return count;
        }

        private Color EnemyColor(EnemyDangerTier tier, float now)
        {
            if (tier == EnemyDangerTier.Low)
                return new Color(0.64f, 1f, 0.22f, 0.95f);

            if (tier == EnemyDangerTier.Medium)
                return new Color(1f, 0.56f, 0.12f, 0.96f);

            if (tier == EnemyDangerTier.High)
                return new Color(1f, 0.12f, 0.1f, 0.98f);

            if (_config.EnemyMarkerStyle.Value == EnemyMarkerStyle.Pulse)
            {
                float alpha = 0.65f + Mathf.Sin(now * 7f) * 0.25f;
                return new Color(0.72f, 0.42f, 1f, Mathf.Clamp01(alpha));
            }

            return new Color(0.65f, 0.5f, 0.9f, 0.9f);
        }

        private void TryLogEnemyScanSummary(float now)
        {
            if (!_hasEnemyScanStats)
                return;

            string summary = _lastEnemyScanStats.ToSummary(_config.EnemyDetectionMode.Value,
                                                           _config.EnemyDetectionRadius.Value,
                                                           _baseMapTextureReady,
                                                           _baseMapTextureVisible,
                                                           _roomOverlay != null ? _roomOverlay.RoomCount : 0);
            if (_lastEnemySummary == summary)
                return;

            _lastEnemySummary = summary;

            // Stable key covers only signal-carrying fields.
            // Noisy fields (RejectedDuplicate grows monotonically, NoRendererButAccepted and
            // RejectedOutOfRange oscillate) change every scan cycle without conveying new info.
            string stableKey = _lastEnemyScanStats.ValidEnemies + ":"
                             + _lastEnemyScanStats.RenderedEnemyMarkers + ":"
                             + _lastEnemyScanStats.AliveEnemies + ":"
                             + _lastEnemyScanStats.ExceptionCount;

            bool stableChanged = stableKey != _lastEnemyStableKey;
            if (stableChanged)
            {
                _lastEnemyStableKey = stableKey;
                _nextNoisyEnemyLog = now + NoisyEnemyLogCooldown;
            }
            else if (now < _nextNoisyEnemyLog)
            {
                return; // noisy-only change, suppress until cooldown expires
            }
            else
            {
                _nextNoisyEnemyLog = now + NoisyEnemyLogCooldown;
            }

            LogHelper.Info(summary);

            if (_config.DebugEnemyTelemetry.Value && !string.IsNullOrEmpty(_lastEnemyScanStats.Telemetry))
                LogHelper.Info(_lastEnemyScanStats.Telemetry);
        }

        private Vector2 WorldToMap(Vector3 world, Vector3 center, float yaw, Vector2 halfSize, bool clamp)
        {
            Vector3 delta = world - center;
            Vector2 point = new Vector2(delta.x, delta.z) * Mathf.Max(0.5f, _config.Zoom.Value);

            if (_config.RotateWithPlayer.Value)
            {
                float radians = yaw * Mathf.Deg2Rad;
                float sin = Mathf.Sin(radians);
                float cos = Mathf.Cos(radians);
                point = new Vector2(point.x * cos - point.y * sin, point.x * sin + point.y * cos);
            }

            if (clamp && _config.ClampMarkersToBounds.Value)
            {
                point.x = Mathf.Clamp(point.x, -halfSize.x + 8f, halfSize.x - 8f);
                point.y = Mathf.Clamp(point.y, -halfSize.y + 8f, halfSize.y - 8f);
            }

            return point;
        }

        private Vector2 MarkerPoint(Vector3 world, Vector3 center, float yaw, Vector2 halfSize)
        {
            if (_config.CenterOnPlayer.Value && (world - center).sqrMagnitude < 0.01f)
                return Vector2.zero;

            return WorldToMap(world, center, yaw, halfSize, true);
        }

        private void UpdateDebug()
        {
            bool enabled = _config.EnableDebugOverlay.Value;
            _debugText.gameObject.SetActive(enabled);
            if (!enabled)
                return;

            string text = "players " + _players.Count + " | enemies " + _enemies.Count + " | spectator " + GameApi.IsSpectating();
            if (_lastDebugText == text)
                return;

            _lastDebugText = text;
            _debugText.text = text;
        }

        private static string Enabled(bool value)
        {
            return value ? "enabled" : "disabled";
        }

        private static Image CreateImage(string name, Transform parent, Color color)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            Image image = obj.AddComponent<Image>();
            image.color = color;
            return image;
        }

        private static Image[] CreateBorder(RectTransform parent)
        {
            Image[] lines = new Image[4];
            lines[0] = CreateImage("BorderTop", parent, Color.white);
            lines[1] = CreateImage("BorderBottom", parent, Color.white);
            lines[2] = CreateImage("BorderLeft", parent, Color.white);
            lines[3] = CreateImage("BorderRight", parent, Color.white);

            AnchorBorder(lines[0].rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -1f), new Vector2(0f, 2f));
            AnchorBorder(lines[1].rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 1f), new Vector2(0f, 2f));
            AnchorBorder(lines[2].rectTransform, new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, 0.5f), new Vector2(1f, 0f), new Vector2(2f, 0f));
            AnchorBorder(lines[3].rectTransform, new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(1f, 0.5f), new Vector2(-1f, 0f), new Vector2(2f, 0f));

            for (int i = 0; i < lines.Length; i++)
                lines[i].raycastTarget = false;

            return lines;
        }

        private static void AnchorBorder(RectTransform rect, Vector2 min, Vector2 max, Vector2 pivot, Vector2 position, Vector2 size)
        {
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.pivot = pivot;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        private void ApplyBorder()
        {
            float thickness = Mathf.Clamp(_config.BorderThickness.Value, 1f, 8f);
            AnchorBorder(_borderLines[0].rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -thickness * 0.5f), new Vector2(0f, thickness));
            AnchorBorder(_borderLines[1].rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, thickness * 0.5f), new Vector2(0f, thickness));
            AnchorBorder(_borderLines[2].rectTransform, new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, 0.5f), new Vector2(thickness * 0.5f, 0f), new Vector2(thickness, 0f));
            AnchorBorder(_borderLines[3].rectTransform, new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(1f, 0.5f), new Vector2(-thickness * 0.5f, 0f), new Vector2(thickness, 0f));

            Color color = new Color(0.18f, 0.92f, 0.98f, _config.ShowBorder.Value ? Mathf.Clamp01(_config.BorderOpacity.Value) : 0f);
            for (int i = 0; i < _borderLines.Length; i++)
                _borderLines[i].color = color;
        }

        private bool LayoutDirty()
        {
            return _lastScale != _config.Scale.Value ||
                   _lastOpacity != _config.Opacity.Value ||
                   _lastBorderOpacity != _config.BorderOpacity.Value ||
                   _lastBackgroundOpacity != _config.BackgroundOpacity.Value ||
                   _lastBorderThickness != _config.BorderThickness.Value ||
                   _lastMapOpacity != _config.MapOpacity.Value ||
                   _lastSoftFrame != _config.SoftFrame.Value ||
                   _lastPosX != _config.PosX.Value ||
                   _lastPosY != _config.PosY.Value ||
                   _lastWidth != _config.Width.Value ||
                   _lastHeight != _config.Height.Value ||
                   _lastPositionPreset != _config.PositionPreset.Value ||
                   _lastUseSquareMap != _config.UseSquareMap.Value ||
                   _lastShowBorder != _config.ShowBorder.Value;
        }

        private void SaveLayoutState()
        {
            _lastScale = _config.Scale.Value;
            _lastOpacity = _config.Opacity.Value;
            _lastBorderOpacity = _config.BorderOpacity.Value;
            _lastBackgroundOpacity = _config.BackgroundOpacity.Value;
            _lastBorderThickness = _config.BorderThickness.Value;
            _lastMapOpacity = _config.MapOpacity.Value;
            _lastSoftFrame = _config.SoftFrame.Value;
            _lastPosX = _config.PosX.Value;
            _lastPosY = _config.PosY.Value;
            _lastWidth = _config.Width.Value;
            _lastHeight = _config.Height.Value;
            _lastPositionPreset = _config.PositionPreset.Value;
            _lastUseSquareMap = _config.UseSquareMap.Value;
            _lastShowBorder = _config.ShowBorder.Value;
        }

        private void ResetLayoutState()
        {
            _lastScale = -1f;
            _lastOpacity = -1f;
            _lastBorderOpacity = -1f;
            _lastBackgroundOpacity = -1f;
            _lastBorderThickness = -1f;
            _lastMapOpacity = -1f;
            _lastSoftFrame = !_config.SoftFrame.Value;
            _lastPosX = -1f;
            _lastPosY = -1f;
            _lastWidth = -1f;
            _lastHeight = -1f;
            _lastPositionPreset = PositionPreset.Custom;
            _lastUseSquareMap = !_config.UseSquareMap.Value;
            _lastShowBorder = !_config.ShowBorder.Value;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }

    internal struct NativeMapCapture
    {
        public bool Ready;
        public Texture Texture;
        public string CameraName;
        public string TextureDescription;
        public string Source;
        public bool MapActive;
        public bool ActiveParentActive;
        public bool CameraEnabled;
        public float CameraOrthographicSize;
        public Rect CameraPixelRect;
        public int CameraCullingMask;
        public bool NativeVisualHidden;
        public int TargetTextureId;
        public int ActiveTextureId;
        public string PixelSample;
        public bool TextureHasVisiblePixels;
        public string RenderResult;
        public bool NativeQuestionMarkersHidden;
        public int NativeQuestionMarkerCount;
        public int MapModuleCount;
        public int LayerCount;
        public float CaptureMs;
        public float CaptureAverageMs;
        public float RenderMs;
        public float RenderCameraOnceMs;
        public float RenderCameraOnceAverageMs;
        public float RenderAverageMs;
        public bool RenderTextureCreated;
        public int RenderTextureCreateCount;
        public string CameraRenderInfo;
        public string Reason;
    }

    internal sealed class NativeMapCaptureProvider
    {
        private const string NativeMapCameraName = "Dirt Finder Map Camera";
        private Camera _camera;
        private Texture _lastTexture;
        private bool _loggedNativeActivation;
        private float _nextPixelSample;
        private string _lastCameraState;
        private bool _lastTextureHasVisiblePixels;
        private float _lastValidatedOrthographicSize = -1f;
        private int _lastValidatedTargetTextureId;
        private bool _textureValidatedOnce;
        private bool _nativeMarkerFilterApplied;
        private bool _cachedQuestionMarkersHidden;
        private int _cachedQuestionMarkerCount;
        private int _lastFilterModuleCount = -1;
        private int _lastFilterLayerCount = -1;
        private float _nextMarkerFilterRefresh;
        private RenderTexture _ownedRenderTexture;
        private int _ownedRenderTextureSize;
        private int _ownedRenderTextureCreateCount;
        private float _captureAverageMs;
        private bool _lastNativeTabOpen;
        private static Type _mapToolType;
        private static FieldInfo _mapToolInstanceField;
        private static FieldInfo _mapToolActiveField;
        private static bool _mapToolReflectionDone;

        public NativeMapCapture Capture(SurveyorMapConfig config, RunState runState)
        {
            long captureStart = Stopwatch.GetTimestamp();
            NativeMapCapture capture = new NativeMapCapture();
            capture.CameraName = "none";
            capture.TextureDescription = "none";
            capture.Source = "none";
            capture.PixelSample = "not-sampled";
            capture.RenderResult = "not-rendered";
            capture.Reason = "not-ready";

            if (config == null || !runState.PlayerApiReady || !runState.HasLocalPlayer)
            {
                capture.Reason = "visibility-gate-blocked";
                FinishCaptureProfile(captureStart, ref capture);
                return capture;
            }

            if (!runState.Allowed)
            {
                capture.Reason = "gameplay-gate-blocked";
                ReleaseRuntimeState();
                FinishCaptureProfile(captureStart, ref capture);
                return capture;
            }

            Map map = Map.Instance;
            if (map == null)
            {
                capture.Reason = "Map.Instance missing";
                FinishCaptureProfile(captureStart, ref capture);
                return capture;
            }

            FillMapState(map, ref capture);

            _camera = ResolveCamera();
            if (_camera == null)
            {
                capture.Reason = "Dirt Finder Map Camera missing";
                FinishCaptureProfile(captureStart, ref capture);
                return capture;
            }

            bool nativeTabOpen = IsNativeMapTabOpen();
            if (nativeTabOpen != _lastNativeTabOpen)
            {
                _lastNativeTabOpen = nativeTabOpen;
                if (nativeTabOpen)
                    LogHelper.Info("NativeMapState: nativeMapTabOpen=True, capturePaused=True");
                else
                    LogHelper.Info("NativeMapState: nativeMapTabOpen=False, resumingCapture=True");
            }
            if (nativeTabOpen)
            {
                bool cameraRestored = false;
                bool parentRestored = false;
                if (_camera != null && !_camera.enabled)
                {
                    _camera.enabled = true;
                    cameraRestored = true;
                }
                if (map != null && map.ActiveParent != null && !map.ActiveParent.activeSelf)
                {
                    map.ActiveParent.SetActive(true);
                    parentRestored = true;
                }
                if (cameraRestored || parentRestored)
                    LogHelper.Info("NativeMapState: originalTargetTextureRestored=True" +
                                   ", cameraRestored=" + cameraRestored +
                                   ", activeParentRestored=" + parentRestored);
                FillMapState(map, ref capture);
                capture.NativeVisualHidden = false;
                capture.Reason = "native-map-tab-active";
                FinishCaptureProfile(captureStart, ref capture);
                return capture;
            }

            FillCameraState(_camera, ref capture);

            // dig-Minimap architecture: keep the native map active and read the texture the
            // game already renders every frame into the Dirt Finder Map Camera. We never create
            // our own RenderTexture, never call camera.Render(), and never hijack targetTexture.
            // This avoids the "Releasing render texture that is set as Camera.targetTexture" error
            // and removes all the fragility around manually rendering and hiding the native map.
            try
            {
                if (!map.Active)
                    map.ActiveSet(true);

                if (map.ActiveParent != null && !map.ActiveParent.activeSelf)
                    map.ActiveParent.SetActive(true);
            }
            catch (Exception ex)
            {
                capture.Reason = "Map.ActiveSet failed: " + ex.Message;
                FinishCaptureProfile(captureStart, ref capture);
                return capture;
            }

            FillMapState(map, ref capture);
            ApplyNativeMarkerFilters(config, ref capture);

            if (!_loggedNativeActivation)
            {
                _loggedNativeActivation = true;
                LogHelper.Info("Native map mirror active (dig-style activeTexture capture; no owned RenderTexture).");
            }

            Texture nativeTexture = _camera.activeTexture;
            if (nativeTexture == null)
            {
                capture.Reason = "native activeTexture not ready yet";
                FinishCaptureProfile(captureStart, ref capture);
                return capture;
            }

            FillCameraState(_camera, ref capture);
            _lastTexture = nativeTexture;
            capture.Texture = nativeTexture;
            capture.TextureDescription = TextureDescription(nativeTexture);
            capture.Source = "native.activeTexture";
            capture.RenderResult = "render-ok";
            capture.TextureHasVisiblePixels = true;
            capture.NativeVisualHidden = true;
            capture.Ready = true;
            capture.Reason = "native-map-rendered";
            capture.CameraRenderInfo = CameraRenderInfo(_camera);
            FinishCaptureProfile(captureStart, ref capture);
            return capture;
        }

        public void ReleaseRuntimeState()
        {
            _lastTexture = null;
            _lastTextureHasVisiblePixels = false;
            _textureValidatedOnce = false;
            _lastValidatedOrthographicSize = -1f;
            _lastValidatedTargetTextureId = 0;
            _nativeMarkerFilterApplied = false;
            _cachedQuestionMarkersHidden = false;
            _cachedQuestionMarkerCount = 0;
            _lastFilterModuleCount = -1;
            _lastFilterLayerCount = -1;
            _nextMarkerFilterRefresh = 0f;
            // dig-style: we never own a RenderTexture and never disable the native camera.
            // ReleaseOwnedRenderTexture is a safe no-op if nothing was ever created.
            // We intentionally do NOT call HideNativeVisuals here — disabling the Dirt Finder
            // Map Camera is what previously risked breaking the native TAB map.
            ReleaseOwnedRenderTexture();
        }

        public string DescribeCameraAndTexture(NativeMapCapture capture)
        {
            string state = "cameraState{name=" + capture.CameraName +
                           ", enabled=" + capture.CameraEnabled +
                           ", ortho=" + capture.CameraOrthographicSize.ToString("0.00") +
                           ", pixelRect=" + capture.CameraPixelRect +
                           ", cullingMask=" + capture.CameraCullingMask +
                           ", targetTextureId=" + capture.TargetTextureId +
                           ", activeTextureId=" + capture.ActiveTextureId +
                           ", texture=" + capture.TextureDescription +
                           ", render=" + capture.RenderResult +
                           ", nativeVisualHidden=" + capture.NativeVisualHidden +
                           ", pixelSample=" + capture.PixelSample + "}";

            if (_lastCameraState != state)
            {
                _lastCameraState = state;
                return state + " changed=True";
            }

            return state + " changed=False";
        }

        public bool TryProjectWorldToHud(Vector3 world, RectTransform hudRoot, bool clamp, out Vector2 point)
        {
            point = Vector2.zero;

            Map map = Map.Instance;
            Camera camera = _camera != null ? _camera : ResolveCamera();
            if (map == null || map.OverLayerParent == null || camera == null || hudRoot == null)
                return false;

            Vector3 nativePosition = WorldToNativeMapPosition(map, world);
            Vector3 viewport = camera.WorldToViewportPoint(nativePosition);
            if (viewport.z < 0f)
                return false;

            if (clamp)
            {
                viewport.x = Mathf.Clamp01(viewport.x);
                viewport.y = Mathf.Clamp01(viewport.y);
            }
            else if (viewport.x < 0f || viewport.x > 1f || viewport.y < 0f || viewport.y > 1f)
            {
                return false;
            }

            Rect rect = hudRoot.rect;
            point = new Vector2((viewport.x - 0.5f) * rect.width, (viewport.y - 0.5f) * rect.height);
            return true;
        }

        // Extended projection that returns intermediate values for spatial telemetry.
        public bool TryProjectWorldToHudEx(Vector3 world, RectTransform hudRoot, bool clamp,
            out Vector2 point, out bool wasClamped, out bool wasOffViewport,
            out Vector3 nativePos, out Vector3 viewportPos)
        {
            point = Vector2.zero;
            wasClamped = false;
            wasOffViewport = false;
            nativePos = Vector3.zero;
            viewportPos = Vector3.zero;

            Map map = Map.Instance;
            Camera camera = _camera != null ? _camera : ResolveCamera();
            if (map == null || map.OverLayerParent == null || camera == null || hudRoot == null)
                return false;

            nativePos = WorldToNativeMapPosition(map, world);
            viewportPos = camera.WorldToViewportPoint(nativePos);
            if (viewportPos.z < 0f)
                return false;

            wasOffViewport = viewportPos.x < 0f || viewportPos.x > 1f ||
                             viewportPos.y < 0f || viewportPos.y > 1f;

            if (clamp)
            {
                wasClamped = wasOffViewport;
                viewportPos.x = Mathf.Clamp01(viewportPos.x);
                viewportPos.y = Mathf.Clamp01(viewportPos.y);
            }
            else if (wasOffViewport)
            {
                return false;
            }

            Rect rect = hudRoot.rect;
            point = new Vector2((viewportPos.x - 0.5f) * rect.width, (viewportPos.y - 0.5f) * rect.height);
            return true;
        }

        private static Vector3 WorldToNativeMapPosition(Map map, Vector3 world)
        {
            Transform overLayer = map.OverLayerParent;
            Vector3 position = world * map.Scale + overLayer.position;
            Vector3 local = overLayer.InverseTransformPoint(position);
            local.y = 0f;
            return overLayer.TransformPoint(local);
        }

        private void EnsureNativeMapReadyForCapture(Map map, ref NativeMapCapture capture)
        {
            if (map == null)
                return;

            try
            {
                if (!map.Active)
                    map.ActiveSet(true);

                if (map.ActiveParent != null && !map.ActiveParent.activeSelf)
                    map.ActiveParent.SetActive(true);

                FillMapState(map, ref capture);
                ApplyNativeMarkerFilters(SurveyorMapPlugin.Settings, ref capture);
                if (!_loggedNativeActivation)
                {
                    _loggedNativeActivation = true;
                    LogHelper.Info("Native map prepared for hidden RenderTexture capture.");
                }
            }
            catch (Exception ex)
            {
                capture.Reason = "Map.ActiveSet failed: " + ex.Message;
            }
        }

        private void ApplyNativeMarkerFilters(SurveyorMapConfig config, ref NativeMapCapture capture)
        {
            if (config == null || config.ShowNativeQuestionMarks.Value)
            {
                _nativeMarkerFilterApplied = false;
                _cachedQuestionMarkersHidden = false;
                _cachedQuestionMarkerCount = 0;
                return;
            }

            bool mapChanged = capture.MapModuleCount != _lastFilterModuleCount ||
                              capture.LayerCount != _lastFilterLayerCount;
            float now = Time.unscaledTime;
            if (_nativeMarkerFilterApplied && !mapChanged)
            {
                capture.NativeQuestionMarkerCount = _cachedQuestionMarkerCount;
                capture.NativeQuestionMarkersHidden = _cachedQuestionMarkersHidden;
                return;
            }

            if (_nativeMarkerFilterApplied && now < _nextMarkerFilterRefresh)
            {
                capture.NativeQuestionMarkerCount = _cachedQuestionMarkerCount;
                capture.NativeQuestionMarkersHidden = _cachedQuestionMarkersHidden;
                return;
            }

            int disabled = 0;
            disabled += DisableSpriteRenderersOnType("MapCustomEntity");
            disabled += DisableSpriteRenderersOnType("MapCustom");
            _nativeMarkerFilterApplied = true;
            _cachedQuestionMarkerCount = disabled;
            _cachedQuestionMarkersHidden = disabled > 0;
            _lastFilterModuleCount = capture.MapModuleCount;
            _lastFilterLayerCount = capture.LayerCount;
            _nextMarkerFilterRefresh = now + 1f;
            capture.NativeQuestionMarkerCount = _cachedQuestionMarkerCount;
            capture.NativeQuestionMarkersHidden = _cachedQuestionMarkersHidden;
        }

        private static int DisableSpriteRenderersOnType(string typeName)
        {
            Type type = GameApi.FindType(typeName);
            if (type == null)
                return 0;

            int disabled = 0;
            UnityEngine.Object[] objects = Resources.FindObjectsOfTypeAll(type);
            for (int i = 0; i < objects.Length; i++)
            {
                Component component = objects[i] as Component;
                if (component == null || component.gameObject == null)
                    continue;

                SpriteRenderer[] renderers = component.GetComponentsInChildren<SpriteRenderer>(true);
                for (int j = 0; j < renderers.Length; j++)
                {
                    SpriteRenderer renderer = renderers[j];
                    if (renderer != null && renderer.enabled)
                    {
                        renderer.enabled = false;
                        disabled++;
                    }
                }
            }

            return disabled;
        }

        private static void HideNativeVisuals(Map map, Camera camera, ref NativeMapCapture capture)
        {
            try
            {
                if (camera != null && camera.enabled)
                    camera.enabled = false;

                if (map != null && map.ActiveParent != null && map.ActiveParent.activeSelf)
                    map.ActiveParent.SetActive(false);

                FillMapState(map, ref capture);
                capture.CameraEnabled = camera != null && camera.enabled;
                capture.NativeVisualHidden = (camera == null || !camera.enabled) &&
                                             (map == null || map.ActiveParent == null || !map.ActiveParent.activeSelf);
            }
            catch (Exception ex)
            {
                capture.NativeVisualHidden = false;
                capture.Reason = "hide-native-visuals failed: " + ex.Message;
            }
        }

        private static void FillMapState(Map map, ref NativeMapCapture capture)
        {
            capture.MapActive = map != null && map.Active;
            capture.ActiveParentActive = map != null && map.ActiveParent != null && map.ActiveParent.activeSelf;
            capture.MapModuleCount = map != null && map.MapModules != null ? map.MapModules.Count : 0;
            capture.LayerCount = map != null && map.Layers != null ? map.Layers.Count : 0;
        }

        private static void FillCameraState(Camera camera, ref NativeMapCapture capture)
        {
            capture.CameraName = camera != null ? camera.name : "none";
            capture.CameraEnabled = camera != null && camera.enabled;
            capture.CameraOrthographicSize = camera != null ? camera.orthographicSize : 0f;
            capture.CameraPixelRect = camera != null ? camera.pixelRect : Rect.zero;
            capture.CameraCullingMask = camera != null ? camera.cullingMask : 0;
            capture.TargetTextureId = camera != null && camera.targetTexture != null ? camera.targetTexture.GetInstanceID() : 0;
            capture.ActiveTextureId = camera != null && camera.activeTexture != null ? camera.activeTexture.GetInstanceID() : 0;
        }

        private void RevalidateAfterCameraChange(NativeMapCapture capture)
        {
            int textureId = capture.TargetTextureId != 0 ? capture.TargetTextureId : capture.ActiveTextureId;
            bool orthographicChanged = Mathf.Abs(capture.CameraOrthographicSize - _lastValidatedOrthographicSize) > 0.01f;
            bool textureChanged = textureId != _lastValidatedTargetTextureId;
            if (!orthographicChanged && !textureChanged)
                return;

            _lastValidatedOrthographicSize = capture.CameraOrthographicSize;
            _lastValidatedTargetTextureId = textureId;
            _lastTextureHasVisiblePixels = false;
            _textureValidatedOnce = false;
            _nextPixelSample = 0f;
        }

        private Camera ResolveCamera()
        {
            if (_camera != null && _camera.gameObject != null && _camera.name == NativeMapCameraName)
                return _camera;

            Camera[] cameras = UnityEngine.Object.FindObjectsOfType<Camera>(true);
            for (int i = 0; i < cameras.Length; i++)
            {
                Camera camera = cameras[i];
                if (camera != null && camera.name == NativeMapCameraName)
                    return camera;
            }

            return null;
        }

        private static bool IsNativeMapTabOpen()
        {
            if (!_mapToolReflectionDone)
            {
                _mapToolReflectionDone = true;
                _mapToolType = GameApi.FindType("MapToolController");
                if (_mapToolType != null)
                {
                    _mapToolInstanceField = AccessTools.Field(_mapToolType, "instance");
                    _mapToolActiveField = AccessTools.Field(_mapToolType, "Active");
                }
            }
            if (_mapToolInstanceField == null || _mapToolActiveField == null)
                return false;
            try
            {
                object inst = _mapToolInstanceField.GetValue(null);
                if (inst == null)
                    return false;
                return (bool)_mapToolActiveField.GetValue(inst);
            }
            catch
            {
                return false;
            }
        }

        private RenderTexture ResolveRenderTexture(SurveyorMapConfig config, Camera camera, out bool created)
        {
            created = false;
            if (config == null || camera == null)
                return null;

            int size = Mathf.Clamp(config.RenderTextureSize.Value, 128, 512);
            if (size != 128 && size != 256 && size != 512)
                size = 256;

            if (_ownedRenderTexture != null && _ownedRenderTextureSize != size)
                ReleaseOwnedRenderTexture();

            if (_ownedRenderTexture == null)
            {
                _ownedRenderTextureSize = size;
                _ownedRenderTexture = new RenderTexture(size, size, 16, RenderTextureFormat.ARGB32);
                _ownedRenderTexture.name = "SurveyorMap.RenderTexture";
                _ownedRenderTexture.antiAliasing = 1;
                _ownedRenderTexture.useMipMap = false;
                _ownedRenderTexture.autoGenerateMips = false;
                _ownedRenderTexture.filterMode = FilterMode.Bilinear;
                _ownedRenderTexture.wrapMode = TextureWrapMode.Clamp;
                _ownedRenderTexture.Create();
                _ownedRenderTextureCreateCount++;
                created = true;
            }
            else if (!_ownedRenderTexture.IsCreated())
            {
                _ownedRenderTexture.Create();
                _ownedRenderTextureCreateCount++;
                created = true;
            }

            return _ownedRenderTexture;
        }

        private void ReleaseOwnedRenderTexture()
        {
            if (_ownedRenderTexture == null)
                return;

            // Clear targetTexture on every camera (including disabled) that references this RT.
            // Camera.allCameras only returns ENABLED cameras, which misses the Dirt Finder Map Camera
            // after HideNativeVisuals sets camera.enabled=false — causing the Unity release error.
            int cleared = 0;
            Camera[] allCams = UnityEngine.Object.FindObjectsOfType<Camera>(true);
            for (int i = 0; i < allCams.Length; i++)
            {
                if (allCams[i] != null && allCams[i].targetTexture == _ownedRenderTexture)
                {
                    allCams[i].targetTexture = null;
                    cleared++;
                }
            }
            if (cleared > 0)
                LogHelper.Info("RT release: cleared targetTexture from " + cleared + " camera(s).");

            if (_ownedRenderTexture.IsCreated())
                _ownedRenderTexture.Release();

            UnityEngine.Object.Destroy(_ownedRenderTexture);
            _ownedRenderTexture = null;
            _ownedRenderTextureSize = 0;
        }

        private static void ConfigureTexture(RenderTexture renderTexture)
        {
            if (renderTexture == null)
                return;

            renderTexture.filterMode = FilterMode.Bilinear;
            renderTexture.wrapMode = TextureWrapMode.Clamp;

            if (!renderTexture.IsCreated())
                renderTexture.Create();
        }

        private static string RenderCameraOnce(Camera camera, RenderTexture renderTexture, out float renderCameraOnceMs, out float renderMs)
        {
            long methodStart = Stopwatch.GetTimestamp();
            renderCameraOnceMs = 0f;
            renderMs = 0f;
            if (camera == null)
                return "camera-missing";

            if (renderTexture == null || !renderTexture.IsCreated())
                return "rendertexture-not-created";

            RenderTexture previousTarget = camera.targetTexture;
            RenderTexture previousActive = RenderTexture.active;
            bool previousEnabled = camera.enabled;
            try
            {
                if (camera.targetTexture != renderTexture)
                    camera.targetTexture = renderTexture;

                camera.enabled = false;
                RenderTexture.active = renderTexture;
                long renderStart = Stopwatch.GetTimestamp();
                camera.Render();
                renderMs = ElapsedMs(renderStart);
                return "render-ok";
            }
            catch (Exception ex)
            {
                renderMs = ElapsedMs(methodStart);
                return "render-failed:" + ex.GetType().Name;
            }
            finally
            {
                RenderTexture.active = previousActive;
                camera.enabled = previousEnabled;
                if (camera.targetTexture != previousTarget)
                    camera.targetTexture = previousTarget;
                renderCameraOnceMs = ElapsedMs(methodStart);
            }
        }

        private static string TextureDescription(Texture texture)
        {
            if (texture == null)
                return "none";

            RenderTexture renderTexture = texture as RenderTexture;
            if (renderTexture != null)
                return renderTexture.width + "x" + renderTexture.height +
                       " format=" + renderTexture.format +
                       " depth=" + renderTexture.depth +
                       " aa=" + renderTexture.antiAliasing +
                       " hdr=" + renderTexture.sRGB +
                       " mipmaps=" + renderTexture.useMipMap +
                       " filter=" + renderTexture.filterMode;

            return texture.width + "x" + texture.height + " filter=" + texture.filterMode;
        }

        private static string CameraRenderInfo(Camera camera)
        {
            if (camera == null)
                return "none";

            return "clear=" + camera.clearFlags +
                   ", hdr=" + camera.allowHDR +
                   ", msaa=" + camera.allowMSAA +
                   ", path=" + camera.actualRenderingPath +
                   ", depth=" + camera.depth +
                   ", targetDisplay=" + camera.targetDisplay;
        }

        private void FinishCaptureProfile(long captureStart, ref NativeMapCapture capture)
        {
            capture.CaptureMs = ElapsedMs(captureStart);
            _captureAverageMs = MovingAverage(_captureAverageMs, capture.CaptureMs, 0.2f);
            capture.CaptureAverageMs = _captureAverageMs;
            // dig-style capture has no manual camera.Render(), so these render-timing metrics
            // stay at zero. They are kept only for the optional profiling log format.
            capture.RenderMs = 0f;
            capture.RenderAverageMs = 0f;
            capture.RenderCameraOnceMs = 0f;
            capture.RenderCameraOnceAverageMs = 0f;
            capture.RenderTextureCreated = false;
            capture.RenderTextureCreateCount = 0;
            if (string.IsNullOrEmpty(capture.CameraRenderInfo))
                capture.CameraRenderInfo = CameraRenderInfo(_camera);
        }

        private static float MovingAverage(float previous, float value, float alpha)
        {
            if (previous <= 0f)
                return value;

            return previous + (value - previous) * Mathf.Clamp01(alpha);
        }

        private static float ElapsedMs(long startTicks)
        {
            long elapsed = Stopwatch.GetTimestamp() - startTicks;
            return (float)(elapsed * 1000.0 / Stopwatch.Frequency);
        }

        private bool TrySampleTexture(RenderTexture renderTexture, out string result)
        {
            if (renderTexture == null)
            {
                result = "not-rendertexture";
                return false;
            }

            float now = Time.unscaledTime;
            bool debugSampling = SurveyorMapPlugin.Settings != null &&
                                 SurveyorMapPlugin.Settings.EnableDebugOverlay.Value;
            if (_textureValidatedOnce && !debugSampling)
            {
                result = "validated-cached";
                return _lastTextureHasVisiblePixels;
            }

            if (now < _nextPixelSample)
            {
                result = "cached-texture";
                return _lastTextureHasVisiblePixels;
            }

            _nextPixelSample = now + (debugSampling ? 5f : 15f);
            RenderTexture previous = RenderTexture.active;
            Texture2D sample = null;
            try
            {
                RenderTexture.active = renderTexture;
                int width = Mathf.Min(16, renderTexture.width);
                int height = Mathf.Min(16, renderTexture.height);
                if (width <= 0 || height <= 0)
                {
                    result = "invalid-size";
                    return false;
                }

                sample = new Texture2D(width, height, TextureFormat.RGB24, false);
                sample.ReadPixels(new Rect(0, 0, width, height), 0, 0, false);
                sample.Apply(false);
                Color[] pixels = sample.GetPixels();
                float r = 0f;
                float g = 0f;
                float b = 0f;
                for (int i = 0; i < pixels.Length; i++)
                {
                    r += pixels[i].r;
                    g += pixels[i].g;
                    b += pixels[i].b;
                }

                float count = Mathf.Max(1, pixels.Length);
                float avgR = r / count;
                float avgG = g / count;
                float avgB = b / count;
                float luminance = avgR * 0.2126f + avgG * 0.7152f + avgB * 0.0722f;
                result = "avg=(" + avgR.ToString("0.000") + "," +
                         avgG.ToString("0.000") + "," +
                         avgB.ToString("0.000") + "), lum=" + luminance.ToString("0.000");
                _lastTextureHasVisiblePixels = luminance > 0.01f;
                if (_lastTextureHasVisiblePixels)
                    _textureValidatedOnce = true;
                return _lastTextureHasVisiblePixels;
            }
            catch (Exception ex)
            {
                result = "sample-failed:" + ex.GetType().Name;
                _lastTextureHasVisiblePixels = false;
                return false;
            }
            finally
            {
                RenderTexture.active = previous;
                if (sample != null)
                    UnityEngine.Object.Destroy(sample);
            }
        }
    }

    internal sealed class SurveyorRoomData
    {
        public readonly Vector3[] Corners = new Vector3[4];
        public int InstanceId;
        public Bounds Bounds;
        public Vector3 Center;
        public bool Truck;
        public bool Extraction;
        public bool StartRoom;
        public bool Explored;
        public int NeighborCount;
        public int WallMask;

        public bool ContainsPlayer(Vector3 position)
        {
            Bounds expanded = Bounds;
            expanded.Expand(new Vector3(1.25f, 8f, 1.25f));
            return expanded.Contains(position);
        }
    }

    internal sealed class SurveyorRoomMapCache
    {
        private const float RefreshInterval = 2f;
        private static readonly FieldInfo ModuleStartRoomField = typeof(Module).GetField("StartRoom", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

        private readonly List<SurveyorRoomData> _rooms = new List<SurveyorRoomData>(128);
        private float _nextRefresh;
        private int _levelId;
        private bool _onLevelStartApplied;

        public int RoomCount
        {
            get { return _rooms.Count; }
        }

        public int ExploredCount { get; private set; }
        public int MapObjectCount { get; private set; }
        public int MapModuleCount { get; private set; }

        public SurveyorRoomData GetRoom(int index)
        {
            return _rooms[index];
        }

        public bool RefreshIfNeeded(int levelId, float now)
        {
            if (levelId == 0)
                return false;

            if (levelId == _levelId && now < _nextRefresh)
                return false;

            bool newLevel = levelId != _levelId;
            _levelId = levelId;
            _nextRefresh = now + RefreshInterval;
            if (newLevel)
                _onLevelStartApplied = false;

            return Refresh(newLevel);
        }

        public bool UpdateExploration(Vector3 playerPosition, RevealMode mode)
        {
            bool changed = false;
            if (mode == RevealMode.FullReveal || (mode == RevealMode.OnLevelStart && !_onLevelStartApplied))
            {
                for (int i = 0; i < _rooms.Count; i++)
                {
                    if (!_rooms[i].Explored)
                    {
                        _rooms[i].Explored = true;
                        changed = true;
                    }
                }

                if (mode == RevealMode.OnLevelStart)
                    _onLevelStartApplied = true;
            }
            else if (mode == RevealMode.ExploredOnly)
            {
                for (int i = 0; i < _rooms.Count; i++)
                {
                    SurveyorRoomData room = _rooms[i];
                    if (!room.Explored && room.ContainsPlayer(playerPosition))
                    {
                        room.Explored = true;
                        changed = true;
                    }
                }
            }

            if (changed)
                RecountExplored();

            return changed;
        }

        private bool Refresh(bool newLevel)
        {
            int previousRoomCount = _rooms.Count;
            List<SurveyorRoomData> previous = new List<SurveyorRoomData>(_rooms);
            _rooms.Clear();
            MapObjectCount = 0;
            MapModuleCount = Map.Instance != null && Map.Instance.MapModules != null ? Map.Instance.MapModules.Count : 0;

            RoomVolume[] rooms = UnityEngine.Object.FindObjectsOfType<RoomVolume>();
            for (int i = 0; i < rooms.Length; i++)
            {
                RoomVolume room = rooms[i];
                if (!IsValidRoom(room))
                    continue;

                AddRoomColliders(room, previous, newLevel);
            }

            MapObject[] mapObjects = UnityEngine.Object.FindObjectsOfType<MapObject>();
            for (int i = 0; i < mapObjects.Length; i++)
            {
                MapObject mapObject = mapObjects[i];
                if (mapObject != null && mapObject.gameObject != null && mapObject.gameObject.scene.IsValid())
                    MapObjectCount++;
            }

            ComputeNeighbors();
            RecountExplored();
            return newLevel || _rooms.Count != previousRoomCount;
        }

        private void AddRoomColliders(RoomVolume room, List<SurveyorRoomData> previous, bool newLevel)
        {
            bool added = false;
            BoxCollider[] boxes = room.GetComponentsInChildren<BoxCollider>(true);
            for (int i = 0; i < boxes.Length; i++)
            {
                BoxCollider box = boxes[i];
                if (box == null || box.gameObject == null || !box.enabled)
                    continue;

                SurveyorRoomData data = CreateRoomData(room, box.GetInstanceID(), previous, newLevel);
                Vector3 center = box.transform.TransformPoint(box.center);
                Vector3 right = box.transform.TransformVector(new Vector3(box.size.x * 0.5f, 0f, 0f));
                Vector3 forward = box.transform.TransformVector(new Vector3(0f, 0f, box.size.z * 0.5f));
                data.Corners[0] = center - right - forward;
                data.Corners[1] = center + right - forward;
                data.Corners[2] = center + right + forward;
                data.Corners[3] = center - right + forward;
                data.Bounds = BoundsFromCorners(data.Corners);
                data.Center = data.Bounds.center;
                _rooms.Add(data);
                added = true;
            }

            MeshCollider[] meshes = room.GetComponentsInChildren<MeshCollider>(true);
            for (int i = 0; i < meshes.Length; i++)
            {
                MeshCollider mesh = meshes[i];
                if (mesh == null || mesh.gameObject == null || !mesh.enabled)
                    continue;

                SurveyorRoomData data = CreateRoomData(room, mesh.GetInstanceID(), previous, newLevel);
                SetCornersFromBounds(data.Corners, mesh.bounds);
                data.Bounds = mesh.bounds;
                data.Center = data.Bounds.center;
                _rooms.Add(data);
                added = true;
            }

            if (added)
                return;

            Renderer renderer = room.GetComponentInChildren<Renderer>(true);
            if (renderer == null)
                return;

            SurveyorRoomData fallback = CreateRoomData(room, room.GetInstanceID(), previous, newLevel);
            SetCornersFromBounds(fallback.Corners, renderer.bounds);
            fallback.Bounds = renderer.bounds;
            fallback.Center = fallback.Bounds.center;
            _rooms.Add(fallback);
        }

        private SurveyorRoomData CreateRoomData(RoomVolume room, int instanceId, List<SurveyorRoomData> previous, bool newLevel)
        {
            SurveyorRoomData data = new SurveyorRoomData();
            data.InstanceId = instanceId;
            data.Truck = room.Truck;
            data.Extraction = room.Extraction;
            data.StartRoom = IsStartRoom(room);
            if (!newLevel)
                data.Explored = WasExplored(previous, instanceId);
            return data;
        }

        private static bool WasExplored(List<SurveyorRoomData> previous, int instanceId)
        {
            for (int i = 0; i < previous.Count; i++)
            {
                if (previous[i].InstanceId == instanceId)
                    return previous[i].Explored;
            }

            return false;
        }

        private void ComputeNeighbors()
        {
            for (int i = 0; i < _rooms.Count; i++)
            {
                SurveyorRoomData room = _rooms[i];
                room.NeighborCount = 0;
                room.WallMask = 0x0F;
                Bounds expanded = room.Bounds;
                expanded.Expand(new Vector3(1.0f, 2f, 1.0f));
                for (int j = 0; j < _rooms.Count; j++)
                {
                    if (i == j)
                        continue;

                    SurveyorRoomData other = _rooms[j];
                    if (!expanded.Intersects(other.Bounds))
                        continue;

                    room.NeighborCount++;
                    Vector3 delta = other.Center - room.Center;
                    if (Mathf.Abs(delta.x) > Mathf.Abs(delta.z))
                    {
                        if (delta.x >= 0f)
                            room.WallMask &= ~0x02;
                        else
                            room.WallMask &= ~0x08;
                    }
                    else
                    {
                        if (delta.z >= 0f)
                            room.WallMask &= ~0x01;
                        else
                            room.WallMask &= ~0x04;
                    }
                }
            }
        }

        private void RecountExplored()
        {
            int explored = 0;
            for (int i = 0; i < _rooms.Count; i++)
            {
                if (_rooms[i].Explored)
                    explored++;
            }

            ExploredCount = explored;
        }

        private static bool IsValidRoom(RoomVolume room)
        {
            return room != null &&
                   room.gameObject != null &&
                   room.gameObject.scene.IsValid() &&
                   room.gameObject.scene.isLoaded &&
                   room.gameObject.activeInHierarchy;
        }

        private static bool IsStartRoom(RoomVolume room)
        {
            if (room == null || room.Module == null || ModuleStartRoomField == null)
                return false;

            object value = ModuleStartRoomField.GetValue(room.Module);
            return value is bool && (bool)value;
        }

        private static Bounds BoundsFromCorners(Vector3[] corners)
        {
            Bounds bounds = new Bounds(corners[0], Vector3.zero);
            for (int i = 1; i < corners.Length; i++)
                bounds.Encapsulate(corners[i]);

            return bounds;
        }

        private static void SetCornersFromBounds(Vector3[] corners, Bounds bounds)
        {
            Vector3 min = bounds.min;
            Vector3 max = bounds.max;
            float y = bounds.center.y;
            corners[0] = new Vector3(min.x, y, min.z);
            corners[1] = new Vector3(max.x, y, min.z);
            corners[2] = new Vector3(max.x, y, max.z);
            corners[3] = new Vector3(min.x, y, max.z);
        }
    }

    internal struct SurveyorRoomOverlayCommand
    {
        public Vector2 A;
        public Vector2 B;
        public Vector2 C;
        public Vector2 D;
        public Color Fill;
        public Color Border;
        public float BorderThickness;
    }

    internal sealed class SurveyorRoomOverlayGraphic : Graphic
    {
        private readonly List<SurveyorRoomOverlayCommand> _rooms = new List<SurveyorRoomOverlayCommand>(128);

        public int RoomCount
        {
            get { return _rooms.Count; }
        }

        protected override void Awake()
        {
            base.Awake();
            color = Color.white;
        }

        public void BeginRooms()
        {
            _rooms.Clear();
        }

        public void AddRoom(Vector2 a, Vector2 b, Vector2 c, Vector2 d, Color fill, Color border, float borderThickness)
        {
            SurveyorRoomOverlayCommand command = new SurveyorRoomOverlayCommand();
            command.A = a;
            command.B = b;
            command.C = c;
            command.D = d;
            command.Fill = fill;
            command.Border = border;
            command.BorderThickness = borderThickness;
            _rooms.Add(command);
        }

        public void EndRooms()
        {
            gameObject.SetActive(_rooms.Count > 0);
            SetVerticesDirty();
        }

        public void ClearRooms()
        {
            if (_rooms.Count == 0 && !gameObject.activeSelf)
                return;

            _rooms.Clear();
            gameObject.SetActive(false);
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            for (int i = 0; i < _rooms.Count; i++)
            {
                SurveyorRoomOverlayCommand room = _rooms[i];
                AddQuad(vh, room.A, room.B, room.C, room.D, room.Fill);
                AddLine(vh, room.A, room.B, room.BorderThickness, room.Border);
                AddLine(vh, room.B, room.C, room.BorderThickness, room.Border);
                AddLine(vh, room.C, room.D, room.BorderThickness, room.Border);
                AddLine(vh, room.D, room.A, room.BorderThickness, room.Border);
            }
        }

        private static void AddQuad(VertexHelper vh, Vector2 a, Vector2 b, Vector2 c, Vector2 d, Color color)
        {
            int index = vh.currentVertCount;
            UIVertex vertex = UIVertex.simpleVert;
            vertex.color = color;
            vertex.position = a;
            vh.AddVert(vertex);
            vertex.position = b;
            vh.AddVert(vertex);
            vertex.position = c;
            vh.AddVert(vertex);
            vertex.position = d;
            vh.AddVert(vertex);
            vh.AddTriangle(index, index + 1, index + 2);
            vh.AddTriangle(index, index + 2, index + 3);
        }

        private static void AddLine(VertexHelper vh, Vector2 a, Vector2 b, float thickness, Color color)
        {
            Vector2 delta = b - a;
            if (delta.sqrMagnitude < 0.001f)
                return;

            Vector2 normal = new Vector2(-delta.y, delta.x).normalized * Mathf.Max(0.5f, thickness * 0.5f);
            AddQuad(vh, a - normal, a + normal, b + normal, b - normal, color);
        }
    }

    internal struct EnemyScanStats
    {
        public int TotalEnemiesFound;
        public int DirectorParentsFound;
        public int SceneEnemiesFound;
        public int ActiveEnemies;
        public int AliveEnemies;
        public int ValidEnemies;
        public int RenderedEnemyMarkers;
        public int AcceptedFromDirector;
        public int AcceptedFromScene;
        public int RejectedNull;
        public int RejectedInactive;
        public int RejectedInvalidScene;
        public int RejectedDespawn;
        public int RejectedParentNotSpawned;
        public int RejectedParentMismatch;
        public int RejectedDead;
        public int RejectedNoRenderer;
        public int RejectedNoTransform;
        public int NoRendererButAccepted;
        public int RejectedOutOfRange;
        public int RejectedDuplicate;
        public int RejectedProjection;
        public int ExceptionCount;
        public string LastException;
        public string Telemetry;
        // Clamping / spatial counters
        public int RenderedUnclamped;
        public int RenderedClamped;
        public int OffViewportBeforeClamp;
        public int MarkerAtEdgeCount;

        public void Reset()
        {
            TotalEnemiesFound = 0;
            DirectorParentsFound = 0;
            SceneEnemiesFound = 0;
            ActiveEnemies = 0;
            AliveEnemies = 0;
            ValidEnemies = 0;
            RenderedEnemyMarkers = 0;
            AcceptedFromDirector = 0;
            AcceptedFromScene = 0;
            RejectedNull = 0;
            RejectedInactive = 0;
            RejectedInvalidScene = 0;
            RejectedDespawn = 0;
            RejectedParentNotSpawned = 0;
            RejectedParentMismatch = 0;
            RejectedDead = 0;
            RejectedNoRenderer = 0;
            RejectedNoTransform = 0;
            NoRendererButAccepted = 0;
            RejectedOutOfRange = 0;
            RejectedDuplicate = 0;
            RejectedProjection = 0;
            ExceptionCount = 0;
            LastException = null;
            Telemetry = null;
            RenderedUnclamped = 0;
            RenderedClamped = 0;
            OffViewportBeforeClamp = 0;
            MarkerAtEdgeCount = 0;
        }

        public string ToSummary(EnemyDetectionMode mode, float radius, bool baseMapReady, bool baseMapVisible, int overlayRooms)
        {
            return "EnemyScan: mode=" + mode +
                   ", radius=" + radius.ToString("0.0") +
                   ", totalEnemiesFound=" + TotalEnemiesFound +
                   ", directorParents=" + DirectorParentsFound +
                   ", sceneEnemies=" + SceneEnemiesFound +
                   ", activeEnemies=" + ActiveEnemies +
                   ", aliveEnemies=" + AliveEnemies +
                   ", validEnemies=" + ValidEnemies +
                   ", renderedEnemyMarkers=" + RenderedEnemyMarkers +
                   ", noRendererButAccepted=" + NoRendererButAccepted +
                   ", rejected{null=" + RejectedNull +
                   ", inactive=" + RejectedInactive +
                   ", invalidScene=" + RejectedInvalidScene +
                   ", despawn=" + RejectedDespawn +
                   ", parentNotSpawned=" + RejectedParentNotSpawned +
                   ", parentMismatch=" + RejectedParentMismatch +
                   ", dead=" + RejectedDead +
                   ", noRenderer=" + RejectedNoRenderer +
                   ", noTransform=" + RejectedNoTransform +
                   ", outOfRange=" + RejectedOutOfRange +
                   ", duplicate=" + RejectedDuplicate +
                   ", projection=" + RejectedProjection +
                   "}, clamped=" + RenderedClamped +
                   ", unclamped=" + RenderedUnclamped +
                   ", offViewportBeforeClamp=" + OffViewportBeforeClamp +
                   ", markerAtEdge=" + MarkerAtEdgeCount +
                   ", baseMapReady=" + baseMapReady +
                   ", baseMapTextureVisible=" + baseMapVisible +
                   ", roomOverlayRooms=" + overlayRooms +
                   (ExceptionCount > 0 ? ", exceptions=" + ExceptionCount + ", lastException=" + LastException : string.Empty);
        }
    }

    internal sealed class HudTextureView
    {
        private readonly RawImage _image;

        private HudTextureView(RawImage image)
        {
            _image = image;
        }

        public static HudTextureView Create(RectTransform parent)
        {
            GameObject obj = new GameObject("NativeMapTexture");
            obj.transform.SetParent(parent, false);
            RectTransform rect = obj.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            RawImage image = obj.AddComponent<RawImage>();
            image.raycastTarget = false;
            image.color = Color.white;
            image.texture = null;
            image.uvRect = new Rect(0f, 0f, 1f, 1f);
            return new HudTextureView(image);
        }

        public void SetTexture(Texture texture)
        {
            if (_image.texture != texture)
                _image.texture = texture;

            _image.enabled = texture != null;
        }

        public void SetOpacity(float opacity)
        {
            Color color = _image.color;
            color.a = Mathf.Clamp01(opacity);
            _image.color = color;
        }

        public void ClearTexture()
        {
            _image.enabled = false;
            _image.texture = null;
        }

        public bool Enabled
        {
            get { return _image != null && _image.enabled; }
        }

        public bool HasTexture
        {
            get { return _image != null && _image.texture != null; }
        }

        public string TextureSize
        {
            get
            {
                Texture texture = _image != null ? _image.texture : null;
                return texture != null ? texture.width + "x" + texture.height : "none";
            }
        }

        public string DescribeState()
        {
            return "enabled=" + Enabled +
                   ", textureAssigned=" + HasTexture +
                   ", textureSize=" + TextureSize;
        }

        public string Describe(string label)
        {
            Texture texture = _image.texture;
            RectTransform rect = _image.rectTransform;
            Material material = _image.material;
            Shader shader = material != null ? material.shader : null;
            return label + "{id=" + _image.gameObject.GetInstanceID() +
                   ", activeSelf=" + _image.gameObject.activeSelf +
                   ", activeInHierarchy=" + _image.gameObject.activeInHierarchy +
                   ", enabled=" + _image.enabled +
                   ", textureAssigned=" + (texture != null) +
                   ", textureId=" + (texture != null ? texture.GetInstanceID() : 0) +
                   ", textureSize=" + (texture != null ? texture.width + "x" + texture.height : "none") +
                   ", material=" + (material != null ? material.name : "default") +
                   ", shader=" + (shader != null ? shader.name : "default") +
                   ", uvRect=" + _image.uvRect +
                   ", color=" + _image.color +
                   ", rectSize=" + rect.rect.size +
                   ", anchored=" + rect.anchoredPosition +
                   ", parent=" + (rect.parent != null ? rect.parent.name : "null") +
                   ", scene=" + SurveyorMapPlugin.SceneName(_image.gameObject) + "}";
        }
    }

    internal sealed class MarkerPool
    {
        private readonly List<MarkerView> _items = new List<MarkerView>(16);

        public MarkerView Get(RectTransform parent, Font font, int index)
        {
            while (_items.Count <= index)
                _items.Add(MarkerView.Create(parent, font, "Marker" + _items.Count, Color.white));

            return _items[index];
        }

        public void HideFrom(int index)
        {
            for (int i = index; i < _items.Count; i++)
                _items[i].SetActive(false);
        }

        public void HideAll()
        {
            HideFrom(0);
        }
    }

    internal static class MarkerShapeSprites
    {
        private static Sprite _circle;
        private static Sprite _triangle;
        private static Sprite _diamond;
        private static Sprite _square;

        public static Sprite ForDanger(EnemyDangerTier tier)
        {
            if (tier == EnemyDangerTier.Low)
                return Circle();

            if (tier == EnemyDangerTier.Medium)
                return Triangle();

            if (tier == EnemyDangerTier.High)
                return Diamond();

            return Square();
        }

        private static Sprite Circle()
        {
            if (_circle == null)
                _circle = Create("SurveyorMap.Circle", ShapeCircle);

            return _circle;
        }

        private static Sprite Triangle()
        {
            if (_triangle == null)
                _triangle = Create("SurveyorMap.Triangle", ShapeTriangle);

            return _triangle;
        }

        private static Sprite Diamond()
        {
            if (_diamond == null)
                _diamond = Create("SurveyorMap.Diamond", ShapeDiamond);

            return _diamond;
        }

        private static Sprite Square()
        {
            if (_square == null)
                _square = Create("SurveyorMap.Square", ShapeSquare);

            return _square;
        }

        private static Sprite Create(string name, Func<int, int, bool> shape)
        {
            const int size = 32;
            Texture2D texture = new Texture2D(size, size, TextureFormat.ARGB32, false);
            texture.name = name + ".Texture";
            texture.filterMode = FilterMode.Bilinear;
            Color clear = new Color(1f, 1f, 1f, 0f);
            Color solid = Color.white;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                    texture.SetPixel(x, y, shape(x, y) ? solid : clear);
            }

            texture.Apply(false, true);
            return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), size);
        }

        private static bool ShapeCircle(int x, int y)
        {
            float dx = x - 15.5f;
            float dy = y - 15.5f;
            return dx * dx + dy * dy <= 13.5f * 13.5f;
        }

        private static bool ShapeTriangle(int x, int y)
        {
            float fy = y / 31f;
            float halfWidth = 0.08f + fy * 0.42f;
            float fx = Mathf.Abs((x / 31f) - 0.5f);
            return fy > 0.12f && fx <= halfWidth;
        }

        private static bool ShapeDiamond(int x, int y)
        {
            return Mathf.Abs(x - 15.5f) + Mathf.Abs(y - 15.5f) <= 14f;
        }

        private static bool ShapeSquare(int x, int y)
        {
            return x >= 5 && x <= 26 && y >= 5 && y <= 26;
        }
    }

    internal sealed class MarkerView
    {
        private readonly RectTransform _rect;
        private readonly Image _image;
        private readonly Text _nameText;

        private MarkerView(RectTransform rect, Image image, Text nameText)
        {
            _rect = rect;
            _image = image;
            _nameText = nameText;
        }

        public static MarkerView Create(RectTransform parent, Font font, string name, Color color)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            RectTransform rect = obj.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);

            Image image = obj.AddComponent<Image>();
            image.raycastTarget = false;
            image.color = color;

            GameObject textObj = new GameObject("Name");
            textObj.transform.SetParent(obj.transform, false);
            Text text = textObj.AddComponent<Text>();
            text.font = font;
            text.fontSize = 10;
            text.alignment = TextAnchor.MiddleLeft;
            text.color = new Color(0.9f, 1f, 1f, 0.9f);
            text.raycastTarget = false;
            RectTransform textRect = text.rectTransform;
            textRect.anchorMin = new Vector2(1f, 0.5f);
            textRect.anchorMax = new Vector2(1f, 0.5f);
            textRect.pivot = new Vector2(0f, 0.5f);
            textRect.anchoredPosition = new Vector2(6f, 0f);
            textRect.sizeDelta = new Vector2(96f, 16f);
            textObj.SetActive(false);

            return new MarkerView(rect, image, text);
        }

        public void SetActive(bool active)
        {
            if (_rect == null)
                return;

            if (_rect.gameObject.activeSelf != active)
                _rect.gameObject.SetActive(active);
        }

        public void SetPosition(Vector2 position)
        {
            _rect.anchoredPosition = position;
        }

        public void SetSize(float size)
        {
            _rect.sizeDelta = new Vector2(size, size);
        }

        public void SetColor(Color color)
        {
            _image.color = color;
        }

        public void SetSprite(Sprite sprite)
        {
            if (_image.sprite != sprite)
                _image.sprite = sprite;
        }

        public void SetRotation(float degrees)
        {
            _rect.localEulerAngles = new Vector3(0f, 0f, degrees);
        }

        public void SetName(string value)
        {
            bool show = !string.IsNullOrEmpty(value);
            _nameText.gameObject.SetActive(show);
            if (show && _nameText.text != value)
                _nameText.text = value;
        }
    }

    internal struct CapabilityReport
    {
        public bool Teammates;
        public bool Enemies;
        public bool Spectator;
        public bool Ui;
    }

    internal struct RunState
    {
        public string Name;
        public bool InLevel;
        public bool InLobby;
        public bool InShop;
        public bool MenuLevel;
        public bool RunIsLevel;
        public bool Spectating;
        public bool HasLocalPlayer;
        public bool HasLevelObjects;
        public bool LevelGenDone;
        public bool LevelGenerated;
        public int ModulesSpawned;
        public bool PlayerApiReady;
        public bool Allowed;
        public string Reason;

        public string ToSummary()
        {
            return "RunState=" + Name +
                   ", inferredLevel=" + InLevel +
                   ", RunIsLobby=" + InLobby +
                   ", RunIsShop=" + InShop +
                   ", MenuLevel=" + MenuLevel +
                   ", RunIsLevel=" + RunIsLevel +
                   ", spectating=" + Spectating +
                   ", PlayerAvatarLocal=" + HasLocalPlayer +
                   ", levelObjects=" + HasLevelObjects +
                   ", levelGenDone=" + LevelGenDone +
                   ", levelGenerated=" + LevelGenerated +
                   ", modulesSpawned=" + ModulesSpawned +
                   ", playerApiReady=" + PlayerApiReady +
                   ", allowed=" + Allowed +
                   ", reason=" + Reason;
        }
    }

    internal static class GameApi
    {
        private const float LevelObjectCheckInterval = 1f;

        private static MethodInfo _playerGetList;
        private static MethodInfo _isSpectating;
        private static MethodInfo _runIsLobby;
        private static MethodInfo _runIsShop;
        private static MethodInfo _runIsLevel;
        private static MethodInfo _menuLevel;
        private static MethodInfo _levelGenDone;
        private static MethodInfo _isLevelShop;
        private static MethodInfo _playerAvatarLocal;
        private static MethodInfo _playerGetObservedPosition;
        private static MethodInfo _playerGetColorMain;
        private static MethodInfo _playerGetName;
        private static FieldInfo _gameDirectorInstance;
        private static FieldInfo _gameDirectorCurrentState;
        private static FieldInfo _levelGeneratorModulesSpawned;
        private static FieldInfo _roomVolumeExplored;
        private static FieldInfo _enemyParentField;
        private static FieldInfo _enemyHealthField;
        private static FieldInfo _enemyHasHealthField;
        private static FieldInfo _enemyHealthDeadField;
        private static FieldInfo _enemyHealthCurrentField;
        private static FieldInfo _enemyParentSpawnedField;
        private static FieldInfo _enemyParentEnemyField;
        private static FieldInfo _enemyDirectorInstanceField;
        private static FieldInfo _enemyDirectorSpawnedField;
        private static Func<List<PlayerAvatar>> _playerGetListFunc;
        private static Func<bool> _isSpectatingFunc;
        private static Func<bool> _runIsLobbyFunc;
        private static Func<bool> _runIsShopFunc;
        private static Func<bool> _runIsLevelFunc;
        private static Func<bool> _menuLevelFunc;
        private static Func<bool> _levelGenDoneFunc;
        private static Func<bool> _isLevelShopFunc;
        private static Func<PlayerAvatar> _playerAvatarLocalFunc;
        private static Func<Vector3> _playerGetObservedPositionFunc;
        private static Func<PlayerAvatar, Color> _playerGetColorMainFunc;
        private static Func<PlayerAvatar, string> _playerGetNameFunc;
        private static float _nextLevelObjectCheck;
        private static bool _cachedHasLevelObjects;
        private static readonly Color[] PlayerPalette =
        {
            new Color(0.3f, 0.8f, 1f, 1f),
            new Color(0.5f, 1f, 0.45f, 1f),
            new Color(1f, 0.75f, 0.25f, 1f),
            new Color(0.95f, 0.45f, 1f, 1f)
        };

        public static CapabilityReport ValidateCapabilities()
        {
            Type semiFunc = typeof(SemiFunc);
            _playerGetList = semiFunc.GetMethod("PlayerGetList", BindingFlags.Public | BindingFlags.Static);
            _isSpectating = semiFunc.GetMethod("IsSpectating", BindingFlags.Public | BindingFlags.Static);
            _runIsLobby = semiFunc.GetMethod("RunIsLobby", BindingFlags.Public | BindingFlags.Static);
            _runIsShop = semiFunc.GetMethod("RunIsShop", BindingFlags.Public | BindingFlags.Static);
            _runIsLevel = semiFunc.GetMethod("RunIsLevel", BindingFlags.Public | BindingFlags.Static);
            _menuLevel = semiFunc.GetMethod("MenuLevel", BindingFlags.Public | BindingFlags.Static);
            _levelGenDone = semiFunc.GetMethod("LevelGenDone", BindingFlags.Public | BindingFlags.Static);
            _isLevelShop = semiFunc.GetMethod("IsLevelShop", BindingFlags.Public | BindingFlags.Static);
            _playerAvatarLocal = semiFunc.GetMethod("PlayerAvatarLocal", BindingFlags.Public | BindingFlags.Static);
            _playerGetObservedPosition = semiFunc.GetMethod("PlayerGetObservedPosition", BindingFlags.Public | BindingFlags.Static);
            _playerGetColorMain = semiFunc.GetMethod("PlayerGetColorMain", BindingFlags.Public | BindingFlags.Static);
            _playerGetName = semiFunc.GetMethod("PlayerGetName", BindingFlags.Public | BindingFlags.Static);
            _gameDirectorInstance = typeof(GameDirector).GetField("instance", BindingFlags.Public | BindingFlags.Static);
            _gameDirectorCurrentState = typeof(GameDirector).GetField("currentState", BindingFlags.Public | BindingFlags.Instance);
            _levelGeneratorModulesSpawned = typeof(LevelGenerator).GetField("ModulesSpawned", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            _roomVolumeExplored = typeof(RoomVolume).GetField("Explored", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            _enemyParentField = typeof(Enemy).GetField("EnemyParent", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            _enemyHealthField = typeof(Enemy).GetField("Health", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            _enemyHasHealthField = typeof(Enemy).GetField("HasHealth", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            _enemyHealthDeadField = typeof(EnemyHealth).GetField("dead", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            _enemyHealthCurrentField = typeof(EnemyHealth).GetField("healthCurrent", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            _enemyParentSpawnedField = typeof(EnemyParent).GetField("Spawned", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            _enemyParentEnemyField = typeof(EnemyParent).GetField("Enemy", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            _enemyDirectorInstanceField = typeof(EnemyDirector).GetField("instance", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            _enemyDirectorSpawnedField = typeof(EnemyDirector).GetField("enemiesSpawned", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

            _playerGetListFunc = CreateDelegate<Func<List<PlayerAvatar>>>(_playerGetList);
            _isSpectatingFunc = CreateDelegate<Func<bool>>(_isSpectating);
            _runIsLobbyFunc = CreateDelegate<Func<bool>>(_runIsLobby);
            _runIsShopFunc = CreateDelegate<Func<bool>>(_runIsShop);
            _runIsLevelFunc = CreateDelegate<Func<bool>>(_runIsLevel);
            _menuLevelFunc = CreateDelegate<Func<bool>>(_menuLevel);
            _levelGenDoneFunc = CreateDelegate<Func<bool>>(_levelGenDone);
            _isLevelShopFunc = CreateDelegate<Func<bool>>(_isLevelShop);
            _playerAvatarLocalFunc = CreateDelegate<Func<PlayerAvatar>>(_playerAvatarLocal);
            _playerGetObservedPositionFunc = CreateDelegate<Func<Vector3>>(_playerGetObservedPosition);
            _playerGetColorMainFunc = CreateDelegate<Func<PlayerAvatar, Color>>(_playerGetColorMain);
            _playerGetNameFunc = CreateDelegate<Func<PlayerAvatar, string>>(_playerGetName);

            CapabilityReport report = new CapabilityReport();
            report.Teammates = _playerGetListFunc != null && _playerAvatarLocalFunc != null;
            report.Enemies = typeof(Enemy) != null;
            report.Spectator = _isSpectatingFunc != null;
            report.Ui = typeof(Canvas) != null && typeof(Image) != null;
            return report;
        }

        public static Type FindType(string typeName)
        {
            Assembly assembly = typeof(SemiFunc).Assembly;
            Type[] types = assembly.GetTypes();
            for (int i = 0; i < types.Length; i++)
            {
                if (types[i].Name == typeName)
                    return types[i];
            }

            return null;
        }

        public static bool IsInAllowedRunState(SurveyorMapConfig config)
        {
            return GetRunState(config).Allowed;
        }

        public static RunState GetRunState(SurveyorMapConfig config)
        {
            RunState state = new RunState();
            state.Name = "Unknown";
            state.Reason = "initial";

            try
            {
                state.InLobby = SafeBool(_runIsLobbyFunc, "RunIsLobby");
                state.InShop = SafeBool(_runIsShopFunc, "RunIsShop") || SafeBool(_isLevelShopFunc, "IsLevelShop");
                state.MenuLevel = SafeBool(_menuLevelFunc, "MenuLevel");
                state.RunIsLevel = SafeBool(_runIsLevelFunc, "RunIsLevel");
                state.Spectating = IsSpectating();
                state.HasLocalPlayer = GetLocalPlayer() != null;
                state.HasLevelObjects = HasLevelObjects();
                LevelGenerator levelGenerator = GetLevelGenerator();
                state.LevelGenDone = SafeBool(_levelGenDoneFunc, "LevelGenDone");
                state.LevelGenerated = levelGenerator != null && levelGenerator.Generated;
                state.ModulesSpawned = GetIntField(_levelGeneratorModulesSpawned, levelGenerator);
                state.PlayerApiReady = _playerAvatarLocalFunc != null &&
                                       _playerGetListFunc != null &&
                                       state.HasLocalPlayer &&
                                       state.HasLevelObjects;

                if (state.InShop)
                {
                    state.Name = "Menu";
                    state.Reason = "shop";
                    return state;
                }

                if (state.InLobby)
                {
                    state.Name = "Menu";
                    state.Reason = "lobby";
                    return state;
                }

                if (state.MenuLevel || !state.RunIsLevel)
                {
                    state.Name = "Menu";
                    state.Reason = state.MenuLevel ? "menu-level" : "not-run-level";
                    return state;
                }

                // dig-Minimap gate: it shows the minimap whenever RunIsLevel() && GameDirector
                // currentState == 2 (gameplay-active). We adopt the same primary condition so the
                // gate reliably opens in a real level, instead of depending on the fragile
                // levelGenDone/levelGenerated/modulesSpawned reflection chain (kept for logging).
                bool gameplayReady = IsGameplayState();
                bool generatedLevelReady = state.LevelGenDone &&
                                           state.LevelGenerated &&
                                           state.ModulesSpawned > 0;
                bool levelReady = gameplayReady || generatedLevelReady;

                if (state.PlayerApiReady && levelReady)
                {
                    state.Name = "Level";
                    state.InLevel = true;
                    state.Allowed = true;
                    state.Reason = gameplayReady ? "gameplay-active" : "generated-level-ready";
                    return state;
                }

                if (state.PlayerApiReady)
                {
                    state.Name = "Loading";
                    state.InLevel = true;
                    state.Reason = "level-not-generated";
                    return state;
                }

                if (config.EnableSpectatorMode.Value && state.Spectating && levelReady)
                {
                    state.Name = "Level";
                    state.InLevel = true;
                    state.Allowed = true;
                    state.Reason = "spectator-level-ready";
                    return state;
                }

                state.Name = "Menu";
                state.Reason = "startup-or-menu";
            }
            catch (Exception ex)
            {
                state.Name = "Unknown";
                state.Allowed = false;
                state.Reason = "run-state-failed";
                LogHelper.RateLimitedWarning("run-state", "RunState=Unknown/Menu because game state API failed: " + ex.Message, 10f);
            }

            return state;
        }

        public static PlayerAvatar GetLocalPlayer()
        {
            if (_playerAvatarLocalFunc == null)
                return null;

            try
            {
                return _playerAvatarLocalFunc();
            }
            catch (Exception ex)
            {
                LogHelper.RateLimitedWarning("api-PlayerAvatarLocal", "PlayerAvatarLocal unavailable right now: " + ex.Message, 10f);
                return null;
            }
        }

        public static void FillPlayers(List<PlayerAvatar> output)
        {
            if (_playerGetListFunc == null)
                return;

            IList list = _playerGetListFunc();
            if (list == null)
                return;

            for (int i = 0; i < list.Count; i++)
            {
                PlayerAvatar player = list[i] as PlayerAvatar;
                if (player != null)
                    output.Add(player);
            }
        }

        public static void FillEnemies(List<Enemy> output, Vector3 center, EnemyDetectionMode mode, float radius, int max, bool debugTelemetry, ref EnemyScanStats stats)
        {
            stats.Reset();
            if (max <= 0)
            {
                output.Clear();
                return;
            }

            float sqrRadius = radius * radius;
            for (int i = output.Count - 1; i >= 0; i--)
            {
                Enemy cached = output[i];
                if (!IsEnemyValidForMinimap(cached, center, mode, sqrRadius))
                    output.RemoveAt(i);
            }

            StringBuilder telemetry = debugTelemetry ? new StringBuilder(1024) : null;
            IList directorEnemies = GetDirectorSpawnedEnemies();
            if (directorEnemies != null)
            {
                stats.DirectorParentsFound = directorEnemies.Count;
                for (int i = 0; i < directorEnemies.Count && output.Count < max; i++)
                {
                    EnemyParent parent = directorEnemies[i] as EnemyParent;
                    Enemy enemy = GetEnemyFromParent(parent);
                    TryAcceptEnemy(output, enemy, parent, center, mode, sqrRadius, max, true, debugTelemetry, telemetry, ref stats);
                }
            }

            Enemy[] enemies = UnityEngine.Object.FindObjectsOfType<Enemy>();
            stats.SceneEnemiesFound = enemies.Length;
            for (int i = 0; i < enemies.Length && output.Count < max; i++)
            {
                Enemy enemy = enemies[i];
                TryAcceptEnemy(output, enemy, GetEnemyParent(enemy), center, mode, sqrRadius, max, false, debugTelemetry, telemetry, ref stats);
            }

            stats.ValidEnemies = output.Count;
            stats.Telemetry = telemetry != null ? telemetry.ToString() : null;
        }

        private static bool ContainsEnemy(List<Enemy> enemies, Enemy enemy)
        {
            for (int i = 0; i < enemies.Count; i++)
            {
                if (enemies[i] == enemy)
                    return true;
            }

            return false;
        }

        private static bool IsEnemyValidForMinimap(Enemy enemy, Vector3 center, EnemyDetectionMode mode, float sqrRadius)
        {
            if (enemy == null || enemy.gameObject == null || !enemy.gameObject.activeInHierarchy)
                return false;

            if (!enemy.gameObject.scene.IsValid() || !enemy.gameObject.scene.isLoaded)
                return false;

            if (enemy.CurrentState == EnemyState.Despawn)
                return false;

            EnemyParent parent = GetEnemyParent(enemy);
            if (parent != null)
            {
                if (_enemyParentSpawnedField != null)
                {
                    object spawned = _enemyParentSpawnedField.GetValue(parent);
                    if (spawned is bool && !(bool)spawned)
                        return false;
                }

                if (_enemyParentEnemyField != null)
                {
                    Enemy parentEnemy = _enemyParentEnemyField.GetValue(parent) as Enemy;
                    if (parentEnemy != null && parentEnemy != enemy)
                        return false;
                }
            }

            if (IsEnemyDead(enemy))
                return false;

            if (!TryGetEnemyPosition(enemy, center, out Vector3 pos))
                return false;

            if (mode == EnemyDetectionMode.NearbyOnly && (pos - center).sqrMagnitude > sqrRadius)
                return false;

            return true;
        }

        private static bool IsEnemyDead(Enemy enemy)
        {
            if (enemy == null || _enemyHealthField == null)
                return false;

            if (_enemyHasHealthField != null)
            {
                object hasHealth = _enemyHasHealthField.GetValue(enemy);
                if (hasHealth is bool && !(bool)hasHealth)
                    return false;
            }

            EnemyHealth health = _enemyHealthField.GetValue(enemy) as EnemyHealth;
            if (health == null)
                return false;

            if (_enemyHealthDeadField != null)
            {
                object dead = _enemyHealthDeadField.GetValue(health);
                if (dead is bool && (bool)dead)
                    return true;
            }

            if (_enemyHealthCurrentField != null)
            {
                object current = _enemyHealthCurrentField.GetValue(health);
                if (current is int && (int)current <= 0)
                    return true;
            }

            return false;
        }

        private static void TryAcceptEnemy(List<Enemy> output, Enemy enemy, EnemyParent parent, Vector3 center, EnemyDetectionMode mode, float sqrRadius, int max, bool fromDirector, bool debugTelemetry, StringBuilder telemetry, ref EnemyScanStats stats)
        {
            stats.TotalEnemiesFound++;

            string reason;
            bool active;
            bool alive;
            bool rendererActive;
            bool accepted = EvaluateEnemyForMinimap(enemy, parent, center, mode, sqrRadius, out reason, out active, out alive, out rendererActive);
            if (active)
                stats.ActiveEnemies++;
            if (alive)
                stats.AliveEnemies++;

            if (!accepted)
            {
                CountReject(reason, ref stats);
                if (debugTelemetry)
                    AppendEnemyTelemetry(telemetry, enemy, parent, center, fromDirector, "rejected:" + reason, active, alive, rendererActive);
                return;
            }

            if (ContainsEnemy(output, enemy))
            {
                stats.RejectedDuplicate++;
                if (debugTelemetry)
                    AppendEnemyTelemetry(telemetry, enemy, parent, center, fromDirector, "rejected:duplicate", active, alive, rendererActive);
                return;
            }

            if (output.Count >= max)
            {
                if (debugTelemetry)
                    AppendEnemyTelemetry(telemetry, enemy, parent, center, fromDirector, "rejected:maxMarkers", active, alive, rendererActive);
                return;
            }

            output.Add(enemy);
            if (!rendererActive)
                stats.NoRendererButAccepted++;
            if (fromDirector)
                stats.AcceptedFromDirector++;
            else
                stats.AcceptedFromScene++;

            if (debugTelemetry)
                AppendEnemyTelemetry(telemetry, enemy, parent, center, fromDirector, "accepted", active, alive, rendererActive);
        }

        private static bool EvaluateEnemyForMinimap(Enemy enemy, EnemyParent parent, Vector3 center, EnemyDetectionMode mode, float sqrRadius, out string reason, out bool active, out bool alive, out bool rendererActive)
        {
            active = false;
            alive = false;
            rendererActive = false;

            if (enemy == null || enemy.gameObject == null)
            {
                reason = "null";
                return false;
            }

            active = enemy.gameObject.activeSelf && enemy.gameObject.activeInHierarchy;
            if (!active)
            {
                reason = "inactive";
                return false;
            }

            if (!enemy.gameObject.scene.IsValid() || !enemy.gameObject.scene.isLoaded)
            {
                reason = "invalidScene";
                return false;
            }

            if (enemy.CurrentState == EnemyState.Despawn)
            {
                reason = "despawn";
                return false;
            }

            if (parent != null)
            {
                if (_enemyParentSpawnedField != null)
                {
                    object spawned = _enemyParentSpawnedField.GetValue(parent);
                    if (spawned is bool && !(bool)spawned)
                    {
                        reason = "parentNotSpawned";
                        return false;
                    }
                }

                if (_enemyParentEnemyField != null)
                {
                    Enemy parentEnemy = _enemyParentEnemyField.GetValue(parent) as Enemy;
                    if (parentEnemy != null && parentEnemy != enemy)
                    {
                        reason = "parentMismatch";
                        return false;
                    }
                }
            }

            alive = !IsEnemyDead(enemy);
            if (!alive)
            {
                reason = "dead";
                return false;
            }

            rendererActive = HasActiveRenderer(enemy);

            Vector3 pos;
            if (!TryGetEnemyPosition(enemy, center, out pos))
            {
                reason = "noTransform";
                return false;
            }

            if (mode == EnemyDetectionMode.NearbyOnly && (pos - center).sqrMagnitude > sqrRadius)
            {
                reason = "outOfRange";
                return false;
            }

            reason = "accepted";
            return true;
        }

        private static void CountReject(string reason, ref EnemyScanStats stats)
        {
            if (reason == "null")
                stats.RejectedNull++;
            else if (reason == "inactive")
                stats.RejectedInactive++;
            else if (reason == "invalidScene")
                stats.RejectedInvalidScene++;
            else if (reason == "despawn")
                stats.RejectedDespawn++;
            else if (reason == "parentNotSpawned")
                stats.RejectedParentNotSpawned++;
            else if (reason == "parentMismatch")
                stats.RejectedParentMismatch++;
            else if (reason == "dead")
                stats.RejectedDead++;
            else if (reason == "noRenderer")
                stats.RejectedNoRenderer++;
            else if (reason == "noTransform")
                stats.RejectedNoTransform++;
            else if (reason == "outOfRange")
                stats.RejectedOutOfRange++;
        }

        private static IList GetDirectorSpawnedEnemies()
        {
            if (_enemyDirectorInstanceField == null || _enemyDirectorSpawnedField == null)
                return null;

            EnemyDirector director = _enemyDirectorInstanceField.GetValue(null) as EnemyDirector;
            if (director == null)
                return null;

            return _enemyDirectorSpawnedField.GetValue(director) as IList;
        }

        private static Enemy GetEnemyFromParent(EnemyParent parent)
        {
            if (parent == null || _enemyParentEnemyField == null)
                return null;

            return _enemyParentEnemyField.GetValue(parent) as Enemy;
        }

        private static void AppendEnemyTelemetry(StringBuilder builder, Enemy enemy, EnemyParent parent, Vector3 center, bool fromDirector, string reason, bool active, bool alive, bool rendererActive)
        {
            if (builder == null)
                return;

            if (builder.Length > 3500)
                return;

            Vector3 position = GetEnemyPosition(enemy, center);
            Vector3 mapPosition = GetNativeMapPosition(position);
            builder.Append("EnemyTelemetry: source=");
            builder.Append(fromDirector ? "director" : "scene");
            builder.Append(", reason=");
            builder.Append(reason);
            builder.Append(", name=");
            builder.Append(parent != null ? parent.enemyName : "unknown");
            builder.Append(", type=");
            builder.Append(enemy != null ? enemy.Type.ToString() : "null");
            builder.Append(", state=");
            builder.Append(enemy != null ? enemy.CurrentState.ToString() : "null");
            builder.Append(", world=");
            builder.Append(FormatVector(position));
            builder.Append(", map=");
            builder.Append(FormatVector(mapPosition));
            builder.Append(", distance=");
            builder.Append(Vector3.Distance(position, center).ToString("0.0"));
            builder.Append(", alive=");
            builder.Append(alive);
            builder.Append(", activeSelf=");
            builder.Append(enemy != null && enemy.gameObject != null && enemy.gameObject.activeSelf);
            builder.Append(", activeInHierarchy=");
            builder.Append(enemy != null && enemy.gameObject != null && enemy.gameObject.activeInHierarchy);
            builder.Append(", rendererActive=");
            builder.Append(rendererActive);
            builder.AppendLine();
        }

        private static Vector3 GetNativeMapPosition(Vector3 world)
        {
            Map map = Map.Instance;
            if (map == null || map.OverLayerParent == null)
                return Vector3.zero;

            return world * map.Scale + map.OverLayerParent.position;
        }

        private static string FormatVector(Vector3 value)
        {
            return "(" + value.x.ToString("0.0") + "," + value.y.ToString("0.0") + "," + value.z.ToString("0.0") + ")";
        }

        private static bool HasActiveRenderer(Enemy enemy)
        {
            return FirstActiveRenderer(enemy) != null;
        }

        public static bool TryRevealRooms(out int explored, out int total)
        {
            explored = 0;
            total = 0;

            try
            {
                RoomVolume[] rooms = UnityEngine.Object.FindObjectsOfType<RoomVolume>();
                total = rooms.Length;
                for (int i = 0; i < rooms.Length; i++)
                {
                    RoomVolume room = rooms[i];
                    if (room == null || room.gameObject == null)
                        continue;

                    if (IsRoomExplored(room))
                        continue;

                    if (room.SetExplored())
                        explored++;
                }

                return total > 0;
            }
            catch (Exception ex)
            {
                explored = 0;
                total = 0;
                LogHelper.RateLimitedWarning("reveal-rooms", "RevealRooms failed safely: " + ex.Message, 10f);
                return false;
            }
        }

        public static int GetLevelGeneratorId()
        {
            LevelGenerator generator = GetLevelGenerator();
            return generator != null ? generator.GetInstanceID() : 0;
        }

        private static bool IsRoomExplored(RoomVolume room)
        {
            if (_roomVolumeExplored == null || room == null)
                return false;

            object value = _roomVolumeExplored.GetValue(room);
            return value is bool && (bool)value;
        }

        internal static EnemyParent GetEnemyParent(Enemy enemy)
        {
            if (_enemyParentField == null || enemy == null)
                return null;

            return _enemyParentField.GetValue(enemy) as EnemyParent;
        }

        public static EnemyDangerTier GetEnemyDangerTier(Enemy enemy)
        {
            if (enemy == null)
                return EnemyDangerTier.Unknown;

            try
            {
                string stateName = enemy.CurrentState.ToString();
                if (ContainsAny(stateName, "Chase", "Attack", "Kill"))
                    return EnemyDangerTier.High;

                EnemyParent parent = GetEnemyParent(enemy);
                string enemyName = parent != null ? parent.enemyName : string.Empty;
                object difficultyObject = parent != null ? (object)parent.difficulty : null;
                string difficulty = difficultyObject != null ? difficultyObject.ToString() : string.Empty;
                int difficultyValue;
                if (int.TryParse(difficulty, out difficultyValue))
                {
                    if (difficultyValue >= 3)
                        return EnemyDangerTier.High;
                    if (difficultyValue == 2)
                        return EnemyDangerTier.Medium;
                    if (difficultyValue == 1)
                        return EnemyDangerTier.Low;
                }

                string combined = (enemyName + " " + difficulty + " " + stateName).ToLowerInvariant();
                if (ContainsAny(combined, "3", "hard", "high", "boss", "apex", "reaper", "hunter", "robe", "chef", "queen", "slow walker"))
                    return EnemyDangerTier.High;

                if (ContainsAny(combined, "2", "medium", "normal", "chase", "crawl", "duck", "clown", "animal"))
                    return EnemyDangerTier.Medium;

                if (ContainsAny(combined, "1", "easy", "low", "small", "slow", "stun", "spawn"))
                    return EnemyDangerTier.Low;
            }
            catch (Exception ex)
            {
                LogHelper.RateLimitedWarning("enemy-danger", "Enemy danger classification fallback: " + ex.Message, 10f);
            }

            return EnemyDangerTier.Unknown;
        }

        private static bool ContainsAny(string value, params string[] needles)
        {
            if (string.IsNullOrEmpty(value))
                return false;

            for (int i = 0; i < needles.Length; i++)
            {
                if (value.IndexOf(needles[i], StringComparison.OrdinalIgnoreCase) >= 0)
                    return true;
            }

            return false;
        }

        public static Vector3 GetPlayerPosition(PlayerAvatar player, Vector3 fallback)
        {
            if (player == null)
                return fallback;

            Transform source = player.playerTransform != null ? player.playerTransform : player.transform;
            return source != null ? source.position : fallback;
        }

        public static Vector3 GetEnemyPosition(Enemy enemy, Vector3 fallback)
        {
            Vector3 position;
            return TryGetEnemyPosition(enemy, fallback, out position) ? position : fallback;
        }

        public static Vector3 GetEnemyPositionWithSource(Enemy enemy, Vector3 fallback, out string source)
        {
            source = "fallback";
            if (enemy == null)
                return fallback;

            if (enemy.CenterTransform != null)
            {
                source = "CenterTransform";
                return enemy.CenterTransform.position;
            }

            Transform centerChild = FindNamedTransform(enemy.transform, "Center");
            if (centerChild != null)
            {
                source = "childCenter";
                return centerChild.position;
            }

            EnemyParent parent = GetEnemyParent(enemy);
            if (parent != null && parent.transform != null)
            {
                source = "EnemyParent";
                return parent.transform.position;
            }

            if (enemy.transform != null)
            {
                source = "enemy.transform";
                return enemy.transform.position;
            }

            Renderer renderer = FirstActiveRenderer(enemy);
            if (renderer != null)
            {
                source = "rendererBounds";
                return renderer.bounds.center;
            }

            return fallback;
        }

        private static bool TryGetEnemyPosition(Enemy enemy, Vector3 fallback, out Vector3 position)
        {
            position = fallback;
            if (enemy == null)
                return false;

            if (enemy.CenterTransform != null)
            {
                position = enemy.CenterTransform.position;
                return true;
            }

            Transform centerTransform = FindNamedTransform(enemy.transform, "Center");
            if (centerTransform != null)
            {
                position = centerTransform.position;
                return true;
            }

            EnemyParent parent = GetEnemyParent(enemy);
            if (parent != null && parent.transform != null)
            {
                position = parent.transform.position;
                return true;
            }

            if (enemy.transform != null)
            {
                position = enemy.transform.position;
                return true;
            }

            Renderer renderer = FirstActiveRenderer(enemy);
            if (renderer != null)
            {
                position = renderer.bounds.center;
                return true;
            }

            return false;
        }

        private static Transform FindNamedTransform(Transform root, string name)
        {
            if (root == null)
                return null;

            if (root.name.IndexOf(name, StringComparison.OrdinalIgnoreCase) >= 0)
                return root;

            for (int i = 0; i < root.childCount; i++)
            {
                Transform found = FindNamedTransform(root.GetChild(i), name);
                if (found != null)
                    return found;
            }

            return null;
        }

        private static Renderer FirstActiveRenderer(Enemy enemy)
        {
            if (enemy == null)
                return null;

            Renderer[] renderers = enemy.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer renderer = renderers[i];
                if (renderer != null && renderer.enabled && renderer.gameObject.activeInHierarchy)
                    return renderer;
            }

            return null;
        }

        public static Vector3 GetObservedPosition()
        {
            if (_playerGetObservedPositionFunc == null)
            {
                PlayerAvatar local = GetLocalPlayer();
                return GetPlayerPosition(local, Vector3.zero);
            }

            return _playerGetObservedPositionFunc();
        }

        public static bool IsSpectating()
        {
            return SafeBool(_isSpectatingFunc, "IsSpectating");
        }

        public static bool IsInventoryVisible(Type inventoryType)
        {
            if (inventoryType == null)
                return false;

            UnityEngine.Object obj = UnityEngine.Object.FindObjectOfType(inventoryType);
            Component component = obj as Component;
            return component != null && component.gameObject.activeInHierarchy;
        }

        public static Color GetPlayerColor(PlayerAvatar player, int index)
        {
            if (_playerGetColorMainFunc != null && player != null)
            {
                return _playerGetColorMainFunc(player);
            }

            return PlayerPalette[Mathf.Abs(index) % PlayerPalette.Length];
        }

        public static string GetPlayerName(PlayerAvatar player)
        {
            if (_playerGetNameFunc != null && player != null)
            {
                string result = _playerGetNameFunc(player);
                if (!string.IsNullOrEmpty(result))
                    return result;
            }

            return "Player";
        }

        private static bool SafeBool(Func<bool> method, string name)
        {
            if (method == null)
                return false;

            try
            {
                return method();
            }
            catch (Exception ex)
            {
                LogHelper.RateLimitedDebug("api-" + name, name + " unavailable right now: " + ex.Message, 10f);
                return false;
            }
        }

        private static int GetIntField(FieldInfo field, object instance)
        {
            if (field == null || instance == null)
                return 0;

            try
            {
                object value = field.GetValue(instance);
                return value != null ? Convert.ToInt32(value) : 0;
            }
            catch (Exception ex)
            {
                LogHelper.RateLimitedWarning("field-" + field.Name, field.Name + " unavailable right now: " + ex.Message, 10f);
                return 0;
            }
        }

        private static bool HasLevelObjects()
        {
            float now = Time.unscaledTime;
            if (now < _nextLevelObjectCheck)
                return _cachedHasLevelObjects;

            _nextLevelObjectCheck = now + LevelObjectCheckInterval;
            try
            {
                _cachedHasLevelObjects = UnityEngine.Object.FindObjectOfType<LevelGenerator>() != null ||
                                         UnityEngine.Object.FindObjectOfType<RoomVolume>() != null;
            }
            catch (Exception ex)
            {
                _cachedHasLevelObjects = false;
                LogHelper.RateLimitedWarning("api-level-objects", "Level object scan unavailable right now: " + ex.Message, 10f);
            }

            return _cachedHasLevelObjects;
        }

        private static LevelGenerator GetLevelGenerator()
        {
            try
            {
                if (LevelGenerator.Instance != null)
                    return LevelGenerator.Instance;

                return UnityEngine.Object.FindObjectOfType<LevelGenerator>();
            }
            catch (Exception ex)
            {
                LogHelper.RateLimitedWarning("api-level-generator", "LevelGenerator unavailable right now: " + ex.Message, 10f);
                return null;
            }
        }

        private static bool IsGameplayState()
        {
            try
            {
                if (_gameDirectorInstance == null || _gameDirectorCurrentState == null)
                    return false;

                object director = _gameDirectorInstance.GetValue(null);
                if (director == null)
                    return false;

                object state = _gameDirectorCurrentState.GetValue(director);
                return Convert.ToInt32(state) == 2;
            }
            catch (Exception ex)
            {
                LogHelper.RateLimitedWarning("api-gameplay-state", "GameDirector gameplay state unavailable right now: " + ex.Message, 10f);
                return false;
            }
        }

        private static T CreateDelegate<T>(MethodInfo method) where T : class
        {
            if (method == null)
                return null;

            try
            {
                return Delegate.CreateDelegate(typeof(T), method, false) as T;
            }
            catch (Exception ex)
            {
                LogHelper.RateLimitedWarning("delegate-" + method.Name, "API delegate unavailable for " + method.Name + ": " + ex.Message, 60f);
                return null;
            }
        }
    }

    internal static class LogHelper
    {
        private static LogLevelSetting _level = LogLevelSetting.Info;
        private static readonly Dictionary<string, float> LastLogTimes = new Dictionary<string, float>();

        public static void Configure(LogLevelSetting level)
        {
            _level = level;
        }

        public static void Info(string message)
        {
            if (_level >= LogLevelSetting.Info)
                SurveyorMapPlugin.Log.LogInfo("[SurveyorMap] " + message);
        }

        public static void Debug(string message)
        {
            if (_level >= LogLevelSetting.Debug)
                SurveyorMapPlugin.Log.LogDebug("[SurveyorMap] " + message);
        }

        public static void Warning(string message)
        {
            if (_level >= LogLevelSetting.Warning)
                SurveyorMapPlugin.Log.LogWarning("[SurveyorMap] " + message);
        }

        public static void Error(string message)
        {
            SurveyorMapPlugin.Log.LogError("[SurveyorMap] " + message);
        }

        public static void RateLimitedWarning(string key, string message, float seconds)
        {
            float now = Time.unscaledTime;
            float last;
            if (LastLogTimes.TryGetValue(key, out last) && now - last < seconds)
                return;

            LastLogTimes[key] = now;
            Warning(message);
        }

        public static void RateLimitedError(string key, string message, float seconds)
        {
            float now = Time.unscaledTime;
            float last;
            if (LastLogTimes.TryGetValue(key, out last) && now - last < seconds)
                return;

            LastLogTimes[key] = now;
            Error(message);
        }

        public static void RateLimitedDebug(string key, string message, float seconds)
        {
            float now = Time.unscaledTime;
            float last;
            if (LastLogTimes.TryGetValue(key, out last) && now - last < seconds)
                return;

            LastLogTimes[key] = now;
            Debug(message);
        }
    }

    // ── RuntimeProbeBehaviour ─────────────────────────────────────────────────────────────
    // Lives on its own "SurveyorMap.RuntimeProbe" GameObject with explicit DontDestroyOnLoad.
    // Completely independent of the main plugin GameObject — if the plugin GO is destroyed
    // on a scene transition, the probe STILL runs, confirming whether MonoBehaviours work.
    // Update/OnGUI here prove the engine loop is alive regardless of all other state.
    internal sealed class RuntimeProbeBehaviour : MonoBehaviour
    {
        internal static RuntimeProbeBehaviour Instance { get; private set; }

        private int _updateCount;
        private int _onGuiCount;
        private float _nextHeartbeat;
        private bool _screenshotTaken;
        private bool _onGuiFirstLog;
        private string _lastException;

        internal static void EnsureCreated()
        {
            if (Instance != null)
                return;
            try
            {
                GameObject go = new GameObject("SurveyorMap.RuntimeProbe");
                DontDestroyOnLoad(go);
                Instance = go.AddComponent<RuntimeProbeBehaviour>();
                LogHelper.Info("RuntimeProbe created. id=" + go.GetInstanceID() +
                               ", activeSelf=" + go.activeSelf +
                               ", activeInHierarchy=" + go.activeInHierarchy);
            }
            catch (Exception ex)
            {
                LogHelper.RateLimitedError("probe-create", "RuntimeProbe.EnsureCreated failed:\n" + ex, 10f);
            }
        }

        private void Update()
        {
            try
            {
                _updateCount++;
                float now = Time.unscaledTime;

                if (now >= _nextHeartbeat)
                {
                    _nextHeartbeat = now + 5f;
                    string scene = GetScene();
                    LogHelper.Info("RuntimeProbe.Update alive. count=" + _updateCount +
                                   ", frame=" + Time.frameCount +
                                   ", t=" + now.ToString("0.0") +
                                   ", scene=" + scene +
                                   ", activeSelf=" + gameObject.activeSelf +
                                   ", activeInHierarchy=" + gameObject.activeInHierarchy);
                    TryWriteStateJson(scene, now);
                }

                // Mark screenshot intent — actual capture happens in OnGUI where screen is ready
                if (!_screenshotTaken && now > 3f &&
                    SurveyorMapPlugin.Settings != null &&
                    SurveyorMapPlugin.Settings.ForceHudProofOfLife.Value)
                {
                    _screenshotTaken = true;
                    LogHelper.Info("RuntimeProbe screenshot flag set (will capture in OnGUI).");
                }
            }
            catch (Exception ex)
            {
                _lastException = ex.ToString();
                LogHelper.RateLimitedError("probe-update", "RuntimeProbe.Update exception:\n" + ex, 5f);
            }
        }

        private void OnGUI()
        {
            try
            {
                _onGuiCount++;
                if (!_onGuiFirstLog)
                {
                    _onGuiFirstLog = true;
                    LogHelper.Info("RuntimeProbe.OnGUI first call. frame=" + Time.frameCount +
                                   ", scene=" + GetScene());
                }

                if (SurveyorMapPlugin.Settings == null ||
                    !SurveyorMapPlugin.Settings.ForceHudProofOfLife.Value)
                    return;

                SurveyorMapPlugin plugin = SurveyorMapPlugin.Instance;
                bool visible = plugin == null || plugin.ProofHudVisible;  // default true if plugin dead
                if (!visible)
                    return;

                DrawProofBox();
            }
            catch (Exception ex)
            {
                _lastException = ex.ToString();
                LogHelper.RateLimitedError("probe-gui", "RuntimeProbe.OnGUI exception:\n" + ex, 5f);
            }
        }

        private void DrawProofBox()
        {
            float w = 260f, h = 260f, x = 24f, y = Screen.height - 120f - h;
            Color prev = GUI.color;

            GUI.color = new Color(0.02f, 0.03f, 0.05f, 0.88f);
            GUI.DrawTexture(new Rect(x, y, w, h), Texture2D.whiteTexture);

            GUI.color = new Color(0.18f, 0.92f, 0.98f, 0.95f);
            GUI.DrawTexture(new Rect(x,          y,          w,  4f), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(x,          y + h - 4f, w,  4f), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(x,          y,          4f, h),  Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(x + w - 4f, y,          4f, h),  Texture2D.whiteTexture);

            GUI.color = Color.white;
            string scene = GetScene();
            GUI.Label(new Rect(x + 8f, y + 8f,  w - 16f, 24f), "SurveyorMap LIVE  [Probe]");
            GUI.Label(new Rect(x + 8f, y + 30f, w - 16f, 20f), "frame=" + Time.frameCount);
            GUI.Label(new Rect(x + 8f, y + 48f, w - 16f, 20f), "scene=" + scene);
            GUI.Label(new Rect(x + 8f, y + 66f, w - 16f, 20f), "probeUpd=" + _updateCount);
            GUI.Label(new Rect(x + 8f, y + 84f, w - 16f, 20f), "M=toggle");
            GUI.color = prev;
        }

        private void TryWriteStateJson(string scene, float now)
        {
            try
            {
                SurveyorMapPlugin plugin = SurveyorMapPlugin.Instance;
                SurveyorMapConfig settings = SurveyorMapPlugin.Settings;
                string path = Path.Combine(DiagnosticsDir(), "surveyormap-runtime-state.json");
                var sb = new StringBuilder();
                sb.AppendLine("{");
                J(sb, "buildTag",                 plugin != null ? plugin.BuildTagShort : "?");
                J(sb, "scene",                    scene);
                Jn(sb, "frameCount",              Time.frameCount);
                Jn(sb, "timeUnscaled",            now);
                Jb(sb, "pluginAwakeCalled",       plugin != null);
                Jn(sb, "pluginUpdateCount",       plugin != null ? plugin.PluginUpdateCount : 0);
                Jn(sb, "pluginOnGuiCount",        plugin != null ? plugin.PluginOnGuiCount  : 0);
                Jb(sb, "runtimeProbeCreated",     true);
                Jn(sb, "runtimeProbeUpdateCount", _updateCount);
                Jn(sb, "runtimeProbeOnGuiCount",  _onGuiCount);
                Jb(sb, "controllerExists",        plugin != null && plugin.ControllerExists);
                Jb(sb, "hudExists",               plugin != null && plugin.HudExists);
                Jb(sb, "forceHudProofOfLife",     settings != null && settings.ForceHudProofOfLife.Value);
                Jb(sb, "proofHudVisible",         plugin == null || plugin.ProofHudVisible);
                Jb(sb, "screenshotTaken",         _screenshotTaken);
                Jlast(sb, "lastException",        _lastException);
                sb.AppendLine("}");
                File.WriteAllText(path, sb.ToString());
            }
            catch { }
        }

        private static void J(StringBuilder sb, string k, string v) =>
            sb.AppendLine("  \"" + k + "\": " + Js(v) + ",");
        private static void Jn(StringBuilder sb, string k, double v) =>
            sb.AppendLine("  \"" + k + "\": " + v.ToString("0.###") + ",");
        private static void Jb(StringBuilder sb, string k, bool v) =>
            sb.AppendLine("  \"" + k + "\": " + (v ? "true" : "false") + ",");
        private static void Jlast(StringBuilder sb, string k, string v) =>
            sb.AppendLine("  \"" + k + "\": " + Js(v));
        private static string Js(string v) =>
            v == null ? "null" :
            "\"" + v.Replace("\\", "\\\\").Replace("\"", "\\\"")
                    .Replace("\r", "").Replace("\n", "\\n") + "\"";

        private static string GetScene()
        {
            try { return SceneManager.GetActiveScene().name; }
            catch { return "?"; }
        }

        internal static string DiagnosticsDir()
        {
            try
            {
                string dllDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                string dir = Path.Combine(dllDir, "diagnostics");
                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);
                return dir;
            }
            catch
            {
                return Path.GetTempPath();
            }
        }
    }

    [HarmonyPatch]
    internal static class SurveyorMapPatches
    {
        // Reserved for minimal postfixes if a future R.E.P.O. update needs explicit state refresh.
    }
}
