// SurveyorMap v2 - Clean rebuild. See CLAUDE_FULL_IMPLEMENTATION_HANDOFF.md for context.
// Replaces the 5000+ line Frankenstein with a thin native-map mirror.
using System;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace SurveyorMap
{
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    public sealed class SurveyorMapPlugin : BaseUnityPlugin
    {
        public const string PluginGuid    = "com.hiarlyscripter.surveyormap";
        public const string PluginName    = "SurveyorMap";
        public const string PluginVersion = "1.0.0";

        internal static ManualLogSource  Log      { get; private set; }
        internal static SurveyorMapConfig Settings { get; private set; }
        internal static SurveyorMapPlugin Instance { get; private set; }

        private Harmony         _harmony;
        private NativeMapMirror _mirror;
        private bool            _hudVisible    = true;
        private float           _toggleCooldown;
        private float           _nextDiagWrite;
        private int             _updateCount;
        private int             _onGuiCount;

        private const float ToggleCooldown = 0.25f;
        private const float DiagWriteInterval = 5f;

        private void Awake()
        {
            Instance = this;

            // Detach from BepInEx parent and make persistent across scene loads
            if (transform.parent != null)
                transform.parent = null;
            gameObject.hideFlags = HideFlags.HideAndDontSave;
            DontDestroyOnLoad(gameObject);

            Log      = Logger;
            Settings = new SurveyorMapConfig(Config);
            _mirror  = new NativeMapMirror();

            _harmony = new Harmony(PluginGuid);
            _harmony.PatchAll(typeof(SurveyorMapPlugin).Assembly);

            var asmPath = Assembly.GetExecutingAssembly().Location;
            SurveyorMapDiagnostics.AssemblyPath    = asmPath;
            SurveyorMapDiagnostics.BuildTimestamp  = File.GetLastWriteTimeUtc(asmPath).ToString("yyyy-MM-ddTHH:mm:ssZ");
            SurveyorMapDiagnostics.DiagnosticsDir  = Path.Combine(Path.GetDirectoryName(asmPath), "diagnostics");

            // Compute MD5 short hash so validators can confirm exact build identity
            try
            {
                using (var md5 = MD5.Create())
                using (var stream = File.OpenRead(asmPath))
                {
                    var hash = BitConverter.ToString(md5.ComputeHash(stream)).Replace("-", "").ToLower();
                    SurveyorMapDiagnostics.BuildMd5Short = hash.Substring(0, 8);
                    Log.LogInfo($"[SurveyorMap] BuildTag md5={SurveyorMapDiagnostics.BuildMd5Short}");
                }
            }
            catch (Exception ex)
            {
                Log.LogWarning($"[SurveyorMap] MD5 compute failed: {ex.Message}");
            }

            Log.LogInfo($"[SurveyorMap] v{PluginVersion} loaded. asm={asmPath}");
            Log.LogInfo($"[SurveyorMap] EnableMinimap={Settings.EnableMinimap.Value}" +
                        $" RevealRooms={Settings.RevealRooms.Value}" +
                        $" ShowEnemies={Settings.ShowEnemies.Value}");
        }

        private void Update()
        {
            _updateCount++;
            // M toggle: skip if text/chat input would intercept
            if (_toggleCooldown > 0f)
                _toggleCooldown -= Time.unscaledDeltaTime;

            if (_toggleCooldown <= 0f &&
                Settings.EnableMinimap.Value &&
                Input.GetKeyDown(Settings.ToggleKey.Value))
            {
                _hudVisible     = !_hudVisible;
                _toggleCooldown = ToggleCooldown;
                SurveyorMapDiagnostics.ToggleKeyDetectedCount++;
                Log.LogInfo($"[SurveyorMap] M toggle -> HUD={_hudVisible} Toggle key detected count={SurveyorMapDiagnostics.ToggleKeyDetectedCount}");
            }

            _mirror.Update(_hudVisible && Settings.EnableMinimap.Value);

            // Write diagnostics JSON periodically for validators
            if (Time.unscaledTime >= _nextDiagWrite)
            {
                _nextDiagWrite = Time.unscaledTime + DiagWriteInterval;
                SurveyorMapDiagnostics.HudVisible     = _hudVisible;
                SurveyorMapDiagnostics.TabActive      = _mirror.IsNativeTabActive;
                SurveyorMapDiagnostics.GameplayActive = _mirror.IsGameplayActive;
                SurveyorMapDiagnostics.TabOpenCount   = _mirror.TabOpenCount;
                SurveyorMapDiagnostics.TabCloseCount  = _mirror.TabCloseCount;
                SurveyorMapDiagnostics.UpdateCount    = _updateCount;
                SurveyorMapDiagnostics.OnGuiCount     = _onGuiCount;
                SurveyorMapDiagnostics.WriteDiagJson();
            }
        }

        private void OnGUI()
        {
            _onGuiCount++;
            if (!Settings.EnableMinimap.Value || !_hudVisible) return;
            if (!_mirror.IsGameplayActive)                      return;
            if (_mirror.IsNativeTabActive)                      return;

            var tex = _mirror.NativeTexture;
            if (tex == null) return;

            float w = Settings.Width.Value;
            float h = Settings.Height.Value;
            float x = Settings.PosX.Value;
            float y = Screen.height - Settings.PosY.Value - h;

            var prev = GUI.color;
            GUI.color = new Color(1f, 1f, 1f, Settings.Opacity.Value);
            GUI.DrawTexture(new Rect(x, y, w, h), tex, ScaleMode.StretchToFill, false);
            GUI.color = prev;
        }

        private void OnDestroy()
        {
            try { _harmony?.UnpatchSelf(); } catch { }
            _mirror = null;
        }
    }
}
