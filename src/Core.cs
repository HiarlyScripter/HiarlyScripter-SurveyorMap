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

        internal static ManualLogSource   Log          { get; private set; }
        internal static SurveyorMapConfig Settings     { get; private set; }
        internal static SurveyorMapPlugin Instance     { get; private set; }
        // True while F8 edit mode is active — read by CameraAimEditModePatch
        internal static bool              EditModeActive { get; private set; }

        // Conditional debug helper — writes to BepInEx log only when DebugLogging=true.
        // Use for all verbose/repetitive diagnostics; warnings/errors bypass this.
        internal static void LogDbg(string msg)
        {
            if (Settings?.DebugLogging?.Value == true)
                Log.LogDebug(msg);
        }

        private Harmony         _harmony;
        private NativeMapMirror _mirror;
        private bool            _hudVisible     = true;
        private float           _toggleCooldown;
        private float           _nextDiagWrite;
        private float           _nextEnemySweep;
        private int             _updateCount;
        private int             _onGuiCount;

        // Edit mode state
        private bool            _editMode;
        private bool            _editDragActive;
        private bool            _editResizeActive;
        private Vector2         _editDragOrigin;
        private float           _editStartPosX;
        private float           _editStartPosY;
        private float           _editStartW;
        private float           _editStartH;

        // Cursor state captured on edit mode entry — restored on exit
        private bool            _savedCursorVisible;
        private CursorLockMode  _savedCursorLockState;

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
                        $" EnemyMarkerSize={Settings.EnemyMarkerSize.Value}" +
                        $" EditModeEnabled={Settings.EditModeEnabled.Value}" +
                        $" UnlockCursor={Settings.UnlockCursorInEditMode.Value}" +
                        $" FreezeCamera={Settings.FreezeCameraInEditMode.Value}" +
                        $" DebugLogging={Settings.DebugLogging.Value}");
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
                    LogDbg($"[SurveyorMap] Patched EnemyHealth.{mName}");
                }
                catch (Exception ex)
                {
                    LogDbg($"[SurveyorMap] Could not patch EnemyHealth.{mName}: {ex.Message}");
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
                LogDbg($"[SurveyorMap] M toggle -> HUD={_hudVisible} count={SurveyorMapDiagnostics.ToggleKeyDetectedCount}");
            }

            // F8 edit mode toggle (not during TAB)
            if (Settings.EditModeEnabled.Value &&
                !_mirror.IsNativeTabActive &&
                Input.GetKeyDown(Settings.EditModeKey.Value))
            {
                _editMode = !_editMode;
                EditModeActive = _editMode; // read by CameraAimEditModePatch
                if (!_editMode)
                {
                    // Restore cursor state that was captured on entry
                    if (Settings.UnlockCursorInEditMode.Value)
                    {
                        Cursor.lockState = _savedCursorLockState;
                        Cursor.visible   = _savedCursorVisible;
                    }
                    // Clear accumulated mouse delta from edit-mode UI interaction
                    Input.ResetInputAxes();
                    Config.Save();
                    Log.LogInfo("[SurveyorMap] Edit mode OFF — config saved.");
                }
                else
                {
                    // Capture current cursor state before unlocking
                    if (Settings.UnlockCursorInEditMode.Value)
                    {
                        _savedCursorLockState = Cursor.lockState;
                        _savedCursorVisible   = Cursor.visible;
                    }
                    Log.LogInfo("[SurveyorMap] Edit mode ON.");
                }
            }

            // Re-apply cursor unlock every frame while edit mode is active.
            // The game may re-lock the cursor each frame; we override it while editing.
            if (_editMode && Settings.UnlockCursorInEditMode.Value)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible   = true;
            }

            // Per-frame mouse-look freeze via InputManager.DisableAiming().
            // InputManager.GetMouseX/Y returns 0 while disableAimingTimer > 0.
            // InputManager.FixedUpdate() decrements the timer each physics step —
            // we re-set it every Update frame to keep it active throughout edit mode.
            // CameraAimEditModePatch (below) additionally skips CameraAim.Update()
            // as a second layer of protection.
            if (_editMode && Settings.FreezeCameraInEditMode.Value)
            {
                try
                {
                    if (InputManager.instance != null)
                        InputManager.instance.DisableAiming();
                }
                catch (Exception ex)
                {
                    LogDbg($"[SurveyorMap] DisableAiming failed: {ex.Message}");
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
            // 24x24 resize handle at bottom-right for easier grabbing
            var resizeRect  = new Rect(x + w - 24f, y + h - 24f, 24f, 24f);
            int ctrlId      = GUIUtility.GetControlID(FocusType.Passive);

            // Yellow border
            GUI.color = new Color(1f, 0.85f, 0f, 0.9f);
            GUI.Box(minimapRect, GUIContent.none);

            // Resize handle indicator
            GUI.color = new Color(1f, 0.85f, 0f, 1f);
            GUI.DrawTexture(resizeRect, Texture2D.whiteTexture);
            GUI.color = Color.white;

            // Info label above minimap
            var info = $"X:{Settings.PosX.Value:0}  Y:{Settings.PosY.Value:0}" +
                       $"  W:{Settings.Width.Value:0}  H:{Settings.Height.Value:0}" +
                       $"  Z:{Settings.Zoom.Value:0.00}" +
                       $"  [drag=move] [corner=resize] [+/-=size] [wheel=zoom] [Shift+wheel=size] [R=reset] [F8=exit]";
            var labelRect = new Rect(x, y - 20f, w + 200f, 18f);
            GUI.color = new Color(1f, 0.85f, 0f, 1f);
            GUI.Label(labelRect, info);
            GUI.color = Color.white;

            // Use GetTypeForControl for reliable drag event delivery
            switch (evt.GetTypeForControl(ctrlId))
            {
                case EventType.MouseDown:
                    if (evt.button == 0)
                    {
                        if (resizeRect.Contains(evt.mousePosition))
                        {
                            GUIUtility.hotControl = ctrlId;
                            _editResizeActive     = true;
                            _editDragActive       = false;
                            _editDragOrigin       = evt.mousePosition;
                            _editStartW           = Settings.Width.Value;
                            _editStartH           = Settings.Height.Value;
                            evt.Use();
                        }
                        else if (minimapRect.Contains(evt.mousePosition))
                        {
                            GUIUtility.hotControl = ctrlId;
                            _editDragActive       = true;
                            _editResizeActive     = false;
                            _editDragOrigin       = evt.mousePosition;
                            _editStartPosX        = Settings.PosX.Value;
                            _editStartPosY        = Settings.PosY.Value;
                            evt.Use();
                        }
                    }
                    break;

                case EventType.MouseDrag:
                    if (GUIUtility.hotControl == ctrlId)
                    {
                        var delta = evt.mousePosition - _editDragOrigin;
                        if (_editDragActive)
                        {
                            // PosY is offset from bottom — GUI Y grows downward, so invert delta.y
                            Settings.PosX.Value = Mathf.Clamp(_editStartPosX + delta.x, 0f,
                                                               Screen.width  - Settings.Width.Value);
                            Settings.PosY.Value = Mathf.Clamp(_editStartPosY - delta.y, 0f,
                                                               Screen.height - Settings.Height.Value);
                        }
                        else if (_editResizeActive)
                        {
                            Settings.Width.Value  = Mathf.Clamp(_editStartW + delta.x, 64f, Screen.width);
                            Settings.Height.Value = Mathf.Clamp(_editStartH + delta.y, 64f, Screen.height);
                        }
                        evt.Use();
                    }
                    break;

                case EventType.MouseUp:
                    if (GUIUtility.hotControl == ctrlId)
                    {
                        GUIUtility.hotControl = 0;
                        _editDragActive   = false;
                        _editResizeActive = false;
                        evt.Use();
                    }
                    break;

                case EventType.ScrollWheel:
                    if (minimapRect.Contains(evt.mousePosition))
                    {
                        if (evt.shift)
                        {
                            // Shift+wheel → resize (scroll up = bigger)
                            float step = -evt.delta.y * 8f;
                            Settings.Width.Value  = Mathf.Clamp(Settings.Width.Value  + step, 64f, Screen.width);
                            Settings.Height.Value = Mathf.Clamp(Settings.Height.Value + step, 64f, Screen.height);
                        }
                        else
                        {
                            // Plain wheel → zoom
                            Settings.Zoom.Value = Mathf.Clamp(
                                Settings.Zoom.Value + evt.delta.y * 0.1f, 0.5f, 10f);
                        }
                        evt.Use();
                    }
                    break;

                case EventType.KeyDown:
                    // +/= keys → increase size
                    if (evt.keyCode == KeyCode.Plus || evt.keyCode == KeyCode.Equals ||
                        evt.keyCode == KeyCode.KeypadPlus)
                    {
                        Settings.Width.Value  = Mathf.Clamp(Settings.Width.Value  + 10f, 64f, Screen.width);
                        Settings.Height.Value = Mathf.Clamp(Settings.Height.Value + 10f, 64f, Screen.height);
                        evt.Use();
                    }
                    // - key → decrease size
                    else if (evt.keyCode == KeyCode.Minus || evt.keyCode == KeyCode.KeypadMinus)
                    {
                        Settings.Width.Value  = Mathf.Clamp(Settings.Width.Value  - 10f, 64f, Screen.width);
                        Settings.Height.Value = Mathf.Clamp(Settings.Height.Value - 10f, 64f, Screen.height);
                        evt.Use();
                    }
                    // R → reset minimap to defaults (only active in edit mode)
                    else if (evt.keyCode == KeyCode.R)
                    {
                        Settings.PosX.Value    = 24f;
                        Settings.PosY.Value    = 120f;
                        Settings.Width.Value   = 260f;
                        Settings.Height.Value  = 260f;
                        Settings.Zoom.Value    = 2.25f;
                        Settings.Opacity.Value = 0.85f;
                        Log.LogInfo("[SurveyorMap] Edit mode: reset to defaults.");
                        evt.Use();
                    }
                    break;
            }
        }

        private void OnDestroy()
        {
            // Ensure edit-mode state is cleared so CameraAimEditModePatch stops suppressing
            EditModeActive = false;
            _editMode      = false;

            // Restore cursor if plugin is destroyed while edit mode was active
            if (Settings?.UnlockCursorInEditMode?.Value == true)
            {
                try { Cursor.lockState = _savedCursorLockState; Cursor.visible = _savedCursorVisible; }
                catch { }
            }
            try { _harmony?.UnpatchSelf(); } catch { }
            _mirror = null;
        }
    }

    // ── CameraAim suppression patch ─────────────────────────────────────────
    // While F8 edit mode is active AND FreezeCameraInEditMode=true, skip
    // CameraAim.Update() entirely so mouse movement does not rotate the camera.
    // This is the only place camera state is affected — no API calls, no Windows hooks.
    // CameraAim.Update() is restored to normal execution as soon as edit mode exits.
    [HarmonyPatch(typeof(CameraAim), "Update")]
    internal static class CameraAimEditModePatch
    {
        [HarmonyPrefix]
        private static bool Prefix()
        {
            if (SurveyorMapPlugin.EditModeActive &&
                SurveyorMapPlugin.Settings?.FreezeCameraInEditMode?.Value == true)
                return false; // skip CameraAim.Update() — camera stays frozen
            return true;      // normal execution outside edit mode
        }
    }
}
