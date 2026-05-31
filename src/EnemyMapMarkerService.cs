using System;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace SurveyorMap
{
    [HarmonyPatch(typeof(EnemyParent), "SpawnRPC")]
    internal static class EnemySpawnPatch
    {
        [HarmonyPostfix]
        private static void Postfix(EnemyParent __instance)
        {
            if (!SurveyorMapPlugin.Settings.ShowEnemies.Value) return;
            EnemyMapMarkerService.AddMarker(__instance);
        }
    }

    [HarmonyPatch(typeof(EnemyParent), "DespawnRPC")]
    internal static class EnemyDespawnPatch
    {
        [HarmonyPostfix]
        private static void Postfix(EnemyParent __instance)
        {
            EnemyMapMarkerService.RemoveMarker(__instance);
        }
    }

    internal static class EnemyMapMarkerService
    {
        // Reflection cache for EnemyParent internals
        private static bool         _reflected;
        private static FieldInfo    _enemyField;
        private static PropertyInfo _enemyProp;
        private static FieldInfo    _rbField;
        private static FieldInfo    _hasRbField;
        private static FieldInfo    _mceField;    // MapCustom.mapCustomEntity

        // Shared sprite for all enemy markers
        private static Sprite _enemySprite;

        private static void EnsureReflection()
        {
            if (_reflected) return;
            _reflected = true;

            var bf = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

            var ep = typeof(EnemyParent);
            _enemyField = ep.GetField("Enemy", bf);
            if (_enemyField == null)
                _enemyProp = ep.GetProperty("Enemy", bf);

            var en = typeof(Enemy);
            _rbField    = en.GetField("Rigidbody",    bf);
            _hasRbField = en.GetField("HasRigidbody", bf);

            _mceField = typeof(MapCustom).GetField("mapCustomEntity",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        }

        private static Enemy GetEnemy(EnemyParent parent)
        {
            try
            {
                if (_enemyField != null) return _enemyField.GetValue(parent) as Enemy;
                if (_enemyProp  != null) return _enemyProp.GetValue(parent)  as Enemy;
            }
            catch { }
            return null;
        }

        private static Sprite GetOrCreateSprite()
        {
            if (_enemySprite != null && _enemySprite) return _enemySprite;

            // 16x16 filled circle, white — tinted red via MapCustom.color
            var tex = new Texture2D(16, 16, TextureFormat.RGBA32, false);
            tex.hideFlags = HideFlags.HideAndDontSave;
            var center = new Vector2(7.5f, 7.5f);
            for (int py = 0; py < 16; py++)
            for (int px = 0; px < 16; px++)
                tex.SetPixel(px, py, Vector2.Distance(new Vector2(px, py), center) <= 7f ? Color.white : Color.clear);
            tex.Apply();

            _enemySprite = Sprite.Create(tex, new Rect(0, 0, 16, 16), new Vector2(0.5f, 0.5f));
            _enemySprite.hideFlags = HideFlags.HideAndDontSave;
            return _enemySprite;
        }

        public static void AddMarker(EnemyParent parent)
        {
            try
            {
                EnsureReflection();
                var enemy = GetEnemy(parent);
                if (enemy == null || !enemy.gameObject) return;

                // Choose host: prefer rigidbody object, fall back to enemy root
                GameObject host = enemy.gameObject;
                try
                {
                    if (_hasRbField != null && _rbField != null)
                    {
                        bool hasRb = (bool)_hasRbField.GetValue(enemy);
                        if (hasRb)
                        {
                            var rb = _rbField.GetValue(enemy) as Rigidbody;
                            if (rb != null) host = rb.gameObject;
                        }
                    }
                }
                catch { }

                // Add or reuse MapCustom component
                var mc = host.GetComponent<MapCustom>() ?? host.AddComponent<MapCustom>();
                var sprite = GetOrCreateSprite();
                var color  = new Color(1f, 0.15f, 0.15f, 1f); // vivid red

                mc.sprite = sprite;
                mc.color  = color;

                // Explicit registration with native map
                try
                {
                    if (Map.Instance != null)
                        Map.Instance.AddCustom(mc, sprite, color);
                }
                catch (Exception ex)
                {
                    SurveyorMapPlugin.Log.LogDebug($"[SurveyorMap] Map.AddCustom: {ex.Message}");
                }

                SurveyorMapDiagnostics.EnemyMarkerCount++;
                SurveyorMapPlugin.Log.LogDebug($"[SurveyorMap] Enemy marker added on {host.name}");
            }
            catch (Exception ex)
            {
                SurveyorMapPlugin.Log.LogWarning($"[SurveyorMap] AddMarker failed: {ex.Message}");
            }
        }

        public static void RemoveMarker(EnemyParent parent)
        {
            try
            {
                EnsureReflection();
                var enemy = GetEnemy(parent);
                if (enemy == null) return;
                CleanupMarker(enemy.gameObject);
            }
            catch (Exception ex)
            {
                SurveyorMapPlugin.Log.LogWarning($"[SurveyorMap] RemoveMarker failed: {ex.Message}");
            }
        }

        private static void CleanupMarker(GameObject go)
        {
            if (go == null) return;

            // Check children too (marker might be on the rigidbody child)
            foreach (var mc in go.GetComponentsInChildren<MapCustom>(true))
            {
                if (mc == null) continue;
                try
                {
                    if (_mceField != null)
                    {
                        var entity = _mceField.GetValue(mc) as MapCustomEntity;
                        if (entity != null && entity.gameObject != null)
                            UnityEngine.Object.Destroy(entity.gameObject);
                    }
                }
                catch { }
                UnityEngine.Object.Destroy(mc);
                SurveyorMapDiagnostics.EnemyMarkerCount = Mathf.Max(0, SurveyorMapDiagnostics.EnemyMarkerCount - 1);
            }
        }
    }
}
