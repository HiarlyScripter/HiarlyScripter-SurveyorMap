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

        internal static ManualLogSource   Log      { get; private set; }
        internal static SurveyorMapConfig Settings { get; private set; }
        internal static SurveyorMapPlugin Instance { get; private set; }

        private Harmony         _harmony;
        private NativeMapMirror _mirror;
        private bool            _hudVisible     = true;
        private float           _toggleCooldown;
        private float           _nextDiagWrite;
        private float           _nextEnemySweep;
        private int             _updateCount;
        private int             _onGuiCount;

        // Edit mode state
        private bool    _editMode;
        private bool    _editDragActive;
        private bool    _editResizeActive;
        private Vector2 _editDragOrigin;
        private float   _editStartPosX;
        private float   _editStartPosY;
        private float   _editStartW;
        private float   _editStartH;

        private const float ToggleCooldown      = 0.25f;
        private const float DiagWriteInterval   = 5f;
        private const float EnemySweepInterval  = 2f;

        private void Awake()
        {
            Instance = this;

            if (transform.parent != null)
                transform.parent = null;
            gameObject.hideFlags = HideFlags.HideAndDontSave;
            DontDestroyOnLoad(gameObject);

            Log      = Logger;
            Settings = new SurveyorMapConfig(Config);
            _mirror  = new NativeMapMirror();

            _harmony = new Harmony(PluginGuid);
            _harmony.PatchAll(typeof(SurveyorMapPlugin).Assembly);
            TryPatchDeathMethods(_harmony);

            var asmPath = Assembly.GetExecutingAssembly().Location;
            SurveyorMapDiagnostics.AssemblyPath   = asmPath;
            SurveyorMapDiagnostics.BuildTimestamp = File.GetLastWriteTimeUtc(asmPath).ToString("yyyy-MM-ddTHH:mm:ssZ");
            SurveyorMapDiagnostics.DiagnosticsDir = Path.Combine(Path.GetDirectoryName(asmPath), "diagnostics");
            SurveyorMapDiagnostics.RevealRoomsMode = Settings.RevealRoomsMode.Value;

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
                        $" RevealRoomsMode={Settings.RevealRoomsMode.Value}" +
                        $" ShowEnemies={Settings.ShowEnemies.Value}" +
                        $" EnemyMarkerShapeMode={Settings.EnemyMarkerShapeMode.Value}" +
                        $" EnemyMarkerSize={Settings.EnemyMarkerSize.Value}" +
                        $" EditModeEnabled={Settings.EditModeEnabled.Value}");
        }

        private static void TryPatchDeathMethods(Harmony harmony)
        {
            var bf = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            var postfixInfo = typeof(EnemyDeathHelper).GetMethod(
                "OnDeathRPC", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
            if (postfixInfo == null) return;
            var postfix = new HarmonyMethod(postfixInfo);

            foreach (var mName in new[] { "DeathRPC", "DeathImpulseRPC" })
            {
                var m = typeof(EnemyHealth).GetMethod(mName, bf);
                if (m == null) continue;
                try
                {
                    harmony.Patch(m, postfix: postfix);
                    Log.LogInfo($"[SurveyorMap] Patched EnemyHealth.{mName}");
                }
                catch (Exception ex)
                {
                    Log.LogDebug($"[SurveyorMap] Could not patch EnemyHealth.{mName}: {ex.Message}");
                }
            }
        }

        private void Update()
        {
            _updateCount++;

            // M toggle
            if (_toggleCooldown > 0f)
                _toggleCooldown -= Time.unscaledDeltaTime;

            if (_toggleCooldown <= 0f &&
                Settings.EnableMinimap.Value &&
                Input.GetKeyDown(Settings.ToggleKey.Value))
            {
                _hudVisible     = !_hudVisible;
                _toggleCooldown = ToggleCooldown;
                SurveyorMapDiagnostics.ToggleKeyDetectedCount++;
                Log.LogInfo($"[SurveyorMap] M toggle -> HUD={_hudVisible} count={SurveyorMapDiagnostics.ToggleKeyDetectedCount}");
            }

            // F8 edit mode toggle (not during TAB)
            if (Settings.EditModeEnabled.Value &&
                !_mirror.IsNativeTabActive &&
                Input.GetKeyDown(Settings.EditModeKey.Value))
            {
                _editMode = !_editMode;
                if (!_editMode)
                {
                    Config.Save();
                    Log.LogInfo("[SurveyorMap] Edit mode OFF — config saved.");
                }
                else
                {
                    Log.LogInfo("[SurveyorMap] Edit mode ON.");
                }
            }

            // Cancel drag/resize if TAB opens while editing
            if (_mirror.IsNativeTabActive)
            {
                _editDragActive   = false;
                _editResizeActive = false;
            }

            _mirror.Update(_hudVisible && Settings.EnableMinimap.Value);

            // Enemy sweep every 2 seconds
            if (Time.unscaledTime >= _nextEnemySweep)
            {
                _nextEnemySweep = Time.unscaledTime + EnemySweepInterval;
                EnemyMapMarkerService.SweepDeadMarkers();
            }

            // Diagnostics write every 5 seconds
            if (Time.unscaledTime >= _nextDiagWrite)
            {
                _nextDiagWrite = Time.unscaledTime + DiagWriteInterval;
                SurveyorMapDiagnostics.HudVisible          = _hudVisible;
                SurveyorMapDiagnostics.TabActive           = _mirror.IsNativeTabActive;
                SurveyorMapDiagnostics.GameplayActive      = _mirror.IsGameplayActive;
                SurveyorMapDiagnostics.TabOpenCount        = _mirror.TabOpenCount;
                SurveyorMapDiagnostics.TabCloseCount       = _mirror.TabCloseCount;
                SurveyorMapDiagnostics.UpdateCount         = _updateCount;
                SurveyorMapDiagnostics.OnGuiCount          = _onGuiCount;
                SurveyorMapDiagnostics.EnemyMarkerCount    = EnemyMapMarkerService.ActiveMarkerCount;
                SurveyorMapDiagnostics.RevealRoomsMode     = Settings.RevealRoomsMode.Value;
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

            if (_editMode)
                DrawEditOverlay(x, y, w, h);
        }

        private void DrawEditOverlay(float x, float y, float w, float h)
        {
            var evt         = Event.current;
            var minimapRect = new Rect(x, y, w, h);
            var resizeRect  = new Rect(x + w - 16f, y + h - 16f, 16f, 16f);

            // Yellow border
            GUI.color = new Color(1f, 0.85f, 0f, 0.9f);
            GUI.Box(minimapRect, GUIContent.none);

            // Resize handle (bottom-right corner)
            GUI.DrawTexture(resizeRect, Texture2D.whiteTexture);
            GUI.color = Color.white;

            // Info label above minimap
            var info = $"X:{Settings.PosX.Value:0}  Y:{Settings.PosY.Value:0}  W:{Settings.Width.Value:0}  H:{Settings.Height.Value:0}  Z:{Settings.Zoom.Value:0.00}  [F8 to exit]";
            var labelRect = new Rect(x, y - 20f, w + 80f, 18f);
            GUI.color = new Color(1f, 0.85f, 0f, 1f);
            GUI.Label(labelRect, info);
            GUI.color = Color.white;

            // Mouse events
            if (evt.type == EventType.MouseDown && evt.button == 0)
            {
                if (resizeRect.Contains(evt.mousePosition))
                {
                    _editResizeActive = true;
                    _editDragOrigin   = evt.mousePosition;
                    _editStartW       = Settings.Width.Value;
                    _editStartH       = Settings.Height.Value;
                    evt.Use();
                }
                else if (minimapRect.Contains(evt.mousePosition))
                {
                    _editDragActive = true;
                    _editDragOrigin = evt.mousePosition;
                    _editStartPosX  = Settings.PosX.Value;
                    _editStartPosY  = Settings.PosY.Value;
                    evt.Use();
                }
            }
            else if (evt.type == EventType.MouseDrag && evt.button == 0)
            {
                var delta = evt.mousePosition - _editDragOrigin;
                if (_editDragActive)
                {
                    // PosY is offset from bottom — mouse Y is inverted
                    float newX = Mathf.Clamp(_editStartPosX + delta.x, 0f,
                                             Screen.width  - Settings.Width.Value);
                    float newY = Mathf.Clamp(_editStartPosY - delta.y, 0f,
                                             Screen.height - Settings.Height.Value);
                    Settings.PosX.Value = newX;
                    Settings.PosY.Value = newY;
                    evt.Use();
                }
                else if (_editResizeActive)
                {
                    float newW = Mathf.Clamp(_editStartW + delta.x, 64f,
                                             Screen.width  - Settings.PosX.Value);
                    float newH = Mathf.Clamp(_editStartH + delta.y, 64f,
                                             Screen.height - Settings.PosY.Value);
                    Settings.Width.Value  = newW;
                    Settings.Height.Value = newH;
                    evt.Use();
                }
            }
            else if (evt.type == EventType.MouseUp && evt.button == 0)
            {
                _editDragActive   = false;
                _editResizeActive = false;
            }
            else if (evt.type == EventType.ScrollWheel &&
                     minimapRect.Contains(evt.mousePosition))
            {
                float newZoom = Settings.Zoom.Value + evt.delta.y * 0.1f;
                Settings.Zoom.Value = Mathf.Clamp(newZoom, 0.5f, 10f);
                evt.Use();
            }
        }

        private void OnDestroy()
        {
            try { _harmony?.UnpatchSelf(); } catch { }
            _mirror = null;
        }
    }
}
