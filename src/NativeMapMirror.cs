using System;
using System.Reflection;
using UnityEngine;

namespace SurveyorMap
{
    /// <summary>
    /// Manages access to the native map camera and its activeTexture.
    /// No owned RenderTexture, no camera.Render(), no targetTexture hijack.
    /// </summary>
    public class NativeMapMirror
    {
        private const string MapCameraName = "Dirt Finder Map Camera";

        private Camera _mapCamera;
        private float  _nextCameraSearch;
        private float  _nextNullTexLog;
        private float  _capturedDefaultSize = -1f;
        private bool   _zoomApplied;

        // Reflection cache for MapToolController
        private static bool        _tabReflected;
        private static FieldInfo   _tabInstField;
        private static PropertyInfo _tabInstProp;
        private static FieldInfo   _tabActiveField;
        private static PropertyInfo _tabActiveProp;

        public bool    IsGameplayActive   { get; private set; }
        public bool    IsNativeTabActive  { get; private set; }
        public Texture NativeTexture      { get; private set; }

        // Tracked for validators
        private bool   _lastTabActive;
        public int     TabOpenCount       { get; private set; }
        public int     TabCloseCount      { get; private set; }

        public void Update(bool hudActive)
        {
            IsGameplayActive  = CheckGameplay();
            IsNativeTabActive = CheckTabActive();

            // Track TAB open/close transitions and log them for validators
            if (IsNativeTabActive != _lastTabActive)
            {
                _lastTabActive = IsNativeTabActive;
                if (IsNativeTabActive)
                {
                    TabOpenCount++;
                    SurveyorMapPlugin.Log.LogInfo($"[SurveyorMap] nativeMapTabOpen=True tabOpenCount={TabOpenCount}");
                }
                else
                {
                    TabCloseCount++;
                    SurveyorMapPlugin.Log.LogInfo($"[SurveyorMap] nativeMapTabOpen=False tabCloseCount={TabCloseCount}");
                }
                SurveyorMapDiagnostics.TabOpenCount  = TabOpenCount;
                SurveyorMapDiagnostics.TabCloseCount = TabCloseCount;
            }

            if (!IsGameplayActive)
            {
                NativeTexture = null;
                return;
            }

            // Keep native map live while HUD is enabled
            if (hudActive && Map.Instance != null && !Map.Instance.Active)
                Map.Instance.ActiveSet(true);

            RefreshCamera();
            if (_mapCamera == null)
            {
                NativeTexture = null;
                return;
            }

            // Apply zoom only when minimap is drawing; restore when TAB open or HUD off
            if (hudActive && !IsNativeTabActive)
            {
                float target = SurveyorMapPlugin.Settings.Zoom.Value;
                if (_mapCamera.orthographic)
                {
                    if (_capturedDefaultSize < 0f)
                        _capturedDefaultSize = _mapCamera.orthographicSize;
                    if (Mathf.Abs(_mapCamera.orthographicSize - target) > 0.01f)
                        _mapCamera.orthographicSize = target;
                    _zoomApplied = true;
                }
            }
            else if (_zoomApplied && _capturedDefaultSize > 0f && _mapCamera.orthographic)
            {
                _mapCamera.orthographicSize = _capturedDefaultSize;
                _zoomApplied = false;
            }

            var tex = _mapCamera.activeTexture;
            if (tex == null)
            {
                if (Time.unscaledTime >= _nextNullTexLog)
                {
                    _nextNullTexLog = Time.unscaledTime + 5f;
                    SurveyorMapPlugin.Log.LogDebug("[SurveyorMap] Map camera activeTexture null — waiting for native render.");
                }
                NativeTexture = null;
            }
            else
            {
                NativeTexture = tex;
                SurveyorMapDiagnostics.NativeTextureReady = true;
            }
        }

        private static bool CheckGameplay()
        {
            try
            {
                if (!SemiFunc.RunIsLevel())              return false;
                if (GameDirector.instance == null)       return false;
                if (Convert.ToInt32(GameDirector.instance.currentState) != 2) return false;
                return true;
            }
            catch { return false; }
        }

        private static void EnsureTabReflection()
        {
            if (_tabReflected) return;
            _tabReflected = true;
            var t = typeof(MapToolController);
            var bf = BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            _tabInstField  = t.GetField("instance",   bf);
            _tabInstProp   = t.GetProperty("instance", bf);
            _tabActiveField = t.GetField("Active",    bf);
            _tabActiveProp  = t.GetProperty("Active", bf);
        }

        private static bool CheckTabActive()
        {
            try
            {
                EnsureTabReflection();
                object ctrl = null;
                if (_tabInstField  != null) ctrl = _tabInstField.GetValue(null);
                if (ctrl == null && _tabInstProp != null) ctrl = _tabInstProp.GetValue(null);
                if (ctrl == null) return false;

                object active = null;
                if (_tabActiveField != null) active = _tabActiveField.GetValue(ctrl);
                if (active == null && _tabActiveProp != null) active = _tabActiveProp.GetValue(ctrl);
                return active is bool b && b;
            }
            catch { return false; }
        }

        private void RefreshCamera()
        {
            // Re-use cached camera while it's alive
            if (_mapCamera != null && _mapCamera.gameObject != null)
                return;

            _mapCamera = null;
            if (Time.unscaledTime < _nextCameraSearch) return;
            _nextCameraSearch = Time.unscaledTime + 2f;

            // Search ALL cameras including inactive (camera may be on inactive object)
            foreach (var cam in Resources.FindObjectsOfTypeAll<Camera>())
            {
                if (cam != null && cam.name == MapCameraName)
                {
                    _mapCamera = cam;
                    _capturedDefaultSize = -1f; // reset on camera (re)discovery
                    SurveyorMapPlugin.Log.LogInfo($"[SurveyorMap] Map camera found: {cam.name} orthoSize={cam.orthographicSize}");
                    break;
                }
            }
        }
    }
}
