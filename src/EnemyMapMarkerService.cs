using System;
using System.Collections.Generic;
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

    // Postfix used by the manual DeathRPC / DeathImpulseRPC patches in Core.cs
    internal static class EnemyDeathHelper
    {
        internal static void OnDeathRPC(EnemyHealth __instance)
        {
            try
            {
                var ep = __instance.GetComponentInParent<EnemyParent>(true);
                if (ep != null) EnemyMapMarkerService.RemoveMarker(ep);
            }
            catch { }
        }
    }

    internal enum MarkerCategory { Easy = 0, Medium = 1, Hard = 2, Elite = 3 }

    internal struct MarkerEntry
    {
        public EnemyParent Parent;
        public Enemy       Enemy;
        public GameObject  Host;
        public MapCustom   Mc;
        public EnemyHealth Health; // cached — may be null
    }

    internal static class EnemyMapMarkerService
    {
        // Registry keyed by EnemyParent.GetInstanceID()
        private static readonly Dictionary<int, MarkerEntry> _registry = new Dictionary<int, MarkerEntry>();

        // Sprite cache per category
        private static readonly Sprite[] _sprites = new Sprite[4];

        // Reflection cache
        private static bool         _reflected;
        private static FieldInfo    _enemyField;
        private static PropertyInfo _enemyProp;
        private static FieldInfo    _rbField;
        private static FieldInfo    _hasRbField;
        private static FieldInfo    _mceField;      // MapCustom.mapCustomEntity
        private static FieldInfo    _diffField;     // EnemyParent.difficulty
        private static FieldInfo    _spawnedField;  // EnemyParent.Spawned
        private static FieldInfo    _deadField;     // EnemyHealth.dead
        private static FieldInfo    _hpField;       // EnemyHealth.healthCurrent

        // Colors per difficulty category (white-tinted via MapCustom.color)
        private static readonly Color[] _colors = new Color[]
        {
            new Color(0.30f, 0.92f, 0.30f, 1f),  // Easy  — green
            new Color(1.00f, 0.85f, 0.15f, 1f),  // Medium — yellow
            new Color(1.00f, 0.45f, 0.10f, 1f),  // Hard  — orange
            new Color(1.00f, 0.15f, 0.15f, 1f),  // Elite — red
        };

        public static int ActiveMarkerCount => _registry.Count;

        // ── Reflection ──────────────────────────────────────────────────────────

        private static void EnsureReflection()
        {
            if (_reflected) return;
            _reflected = true;
            var bf = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

            var ep = typeof(EnemyParent);
            _enemyField   = ep.GetField("Enemy",      bf);
            if (_enemyField == null)
                _enemyProp  = ep.GetProperty("Enemy", bf);
            _diffField    = ep.GetField("difficulty",  bf) ?? ep.GetField("Difficulty",  bf);
            _spawnedField = ep.GetField("Spawned",     bf) ?? ep.GetField("spawned",     bf);

            var en = typeof(Enemy);
            _rbField    = en.GetField("Rigidbody",    bf);
            _hasRbField = en.GetField("HasRigidbody", bf);

            _mceField = typeof(MapCustom).GetField("mapCustomEntity",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            var eh = typeof(EnemyHealth);
            _deadField = eh.GetField("dead",          bf) ?? eh.GetField("Dead",          bf);
            _hpField   = eh.GetField("healthCurrent", bf) ?? eh.GetField("HealthCurrent", bf)
                      ?? eh.GetField("HP",            bf) ?? eh.GetField("hp",            bf);
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

        private static MarkerCategory GetCategory(EnemyParent parent)
        {
            try
            {
                if (_diffField != null)
                {
                    var raw = _diffField.GetValue(parent);
                    if (raw != null)
                    {
                        int d = Convert.ToInt32(raw);
                        if (d <= 1) return MarkerCategory.Easy;
                        if (d == 2) return MarkerCategory.Medium;
                        if (d == 3) return MarkerCategory.Hard;
                        return MarkerCategory.Elite;
                    }
                }
            }
            catch { }
            return MarkerCategory.Elite;
        }

        // ── Sprite generation ────────────────────────────────────────────────────

        private static Sprite GetOrCreateSprite(MarkerCategory cat)
        {
            int idx = (int)cat;
            if (_sprites[idx] != null && _sprites[idx]) return _sprites[idx];

            bool shapeMode = string.Equals(
                SurveyorMapPlugin.Settings.EnemyMarkerShapeMode.Value,
                "DifficultyShape", StringComparison.OrdinalIgnoreCase);

            var tex = new Texture2D(16, 16, TextureFormat.RGBA32, false);
            tex.hideFlags = HideFlags.HideAndDontSave;
            var px = new Color[256];
            for (int i = 0; i < 256; i++) px[i] = Color.clear;

            if (!shapeMode || cat == MarkerCategory.Easy)
            {
                // Circle
                for (int py = 0; py < 16; py++)
                for (int pxc = 0; pxc < 16; pxc++)
                    if (Vector2.Distance(new Vector2(pxc, py), new Vector2(7.5f, 7.5f)) <= 6.5f)
                        px[py * 16 + pxc] = Color.white;
            }
            else if (cat == MarkerCategory.Medium)
            {
                // Diamond: |dx|+|dy| <= 6.5
                for (int py = 0; py < 16; py++)
                for (int pxc = 0; pxc < 16; pxc++)
                    if (Mathf.Abs(pxc - 7.5f) + Mathf.Abs(py - 7.5f) <= 6.5f)
                        px[py * 16 + pxc] = Color.white;
            }
            else if (cat == MarkerCategory.Hard)
            {
                // Upward triangle: base at bottom, apex at top
                for (int py = 2; py <= 14; py++)
                {
                    float halfW = (py - 2f) / 12f * 6.5f;
                    for (int pxc = 0; pxc < 16; pxc++)
                        if (Mathf.Abs(pxc - 7.5f) <= halfW)
                            px[py * 16 + pxc] = Color.white;
                }
            }
            else
            {
                // Elite — 6-point star (two overlapping triangles)
                // Upward triangle
                for (int py = 2; py <= 12; py++)
                {
                    float halfW = (py - 2f) / 10f * 5.5f;
                    for (int pxc = 0; pxc < 16; pxc++)
                        if (Mathf.Abs(pxc - 7.5f) <= halfW)
                            px[py * 16 + pxc] = Color.white;
                }
                // Downward triangle
                for (int py = 4; py <= 14; py++)
                {
                    float halfW = (14f - py) / 10f * 5.5f;
                    for (int pxc = 0; pxc < 16; pxc++)
                        if (Mathf.Abs(pxc - 7.5f) <= halfW)
                            px[py * 16 + pxc] = Color.white;
                }
            }

            tex.SetPixels(px);
            tex.Apply();
            var sprite = Sprite.Create(tex, new Rect(0, 0, 16, 16), new Vector2(0.5f, 0.5f));
            sprite.hideFlags = HideFlags.HideAndDontSave;
            _sprites[idx] = sprite;
            return sprite;
        }

        private static void ApplyScale(MapCustom mc)
        {
            float size = SurveyorMapPlugin.Settings.EnemyMarkerSize.Value;
            if (Mathf.Abs(size - 1f) < 0.01f) return;
            try
            {
                if (_mceField == null) return;
                var entity = _mceField.GetValue(mc) as MapCustomEntity;
                if (entity != null && entity.gameObject != null)
                    entity.transform.localScale = Vector3.one * size;
            }
            catch { }
        }

        // ── Public API ───────────────────────────────────────────────────────────

        public static void AddMarker(EnemyParent parent)
        {
            try
            {
                EnsureReflection();
                int id = parent.GetInstanceID();
                if (_registry.ContainsKey(id)) return;

                var enemy = GetEnemy(parent);
                if (enemy == null || !enemy.gameObject) return;

                // Prefer rigidbody child as host
                GameObject host = enemy.gameObject;
                try
                {
                    if (_hasRbField != null && _rbField != null &&
                        (bool)_hasRbField.GetValue(enemy))
                    {
                        var rb = _rbField.GetValue(enemy) as Rigidbody;
                        if (rb != null) host = rb.gameObject;
                    }
                }
                catch { }

                var cat    = GetCategory(parent);
                var sprite = GetOrCreateSprite(cat);
                var color  = _colors[(int)cat];

                var mc = host.GetComponent<MapCustom>() ?? host.AddComponent<MapCustom>();
                mc.sprite = sprite;
                mc.color  = color;

                try
                {
                    if (Map.Instance != null)
                        Map.Instance.AddCustom(mc, sprite, color);
                }
                catch (Exception ex)
                {
                    SurveyorMapPlugin.Log.LogDebug($"[SurveyorMap] Map.AddCustom: {ex.Message}");
                }

                ApplyScale(mc);

                var health = enemy.GetComponentInChildren<EnemyHealth>(true)
                          ?? host.GetComponentInChildren<EnemyHealth>(true);

                _registry[id] = new MarkerEntry
                {
                    Parent = parent,
                    Enemy  = enemy,
                    Host   = host,
                    Mc     = mc,
                    Health = health,
                };

                SurveyorMapPlugin.Log.LogDebug(
                    $"[SurveyorMap] Enemy marker added: id={id} cat={cat} host={host.name} total={_registry.Count}");
            }
            catch (Exception ex)
            {
                SurveyorMapPlugin.Log.LogWarning($"[SurveyorMap] AddMarker failed: {ex.Message}");
            }
        }

        public static void RemoveMarker(EnemyParent parent)
        {
            int id = parent.GetInstanceID();
            if (_registry.TryGetValue(id, out var entry))
                CleanupEntry(id, entry);
        }

        // Called from Core.cs Update() every 2 seconds
        public static void SweepDeadMarkers()
        {
            if (_registry.Count == 0) return;
            var toRemove = new List<int>();
            foreach (var kv in _registry)
                if (IsEntryStale(kv.Value)) toRemove.Add(kv.Key);

            foreach (int id in toRemove)
                if (_registry.TryGetValue(id, out var entry))
                    CleanupEntry(id, entry);

            if (toRemove.Count > 0)
                SurveyorMapPlugin.Log.LogDebug(
                    $"[SurveyorMap] Sweep removed {toRemove.Count} stale markers. Active={_registry.Count}");
        }

        // Called on level load (GenerateDone) to reset state between runs
        public static void ClearAll()
        {
            var ids = new List<int>(_registry.Keys);
            foreach (int id in ids)
                if (_registry.TryGetValue(id, out var entry))
                    CleanupEntry(id, entry);
        }

        // ── Internal ─────────────────────────────────────────────────────────────

        private static bool IsEntryStale(MarkerEntry e)
        {
            try
            {
                if (e.Parent == null)                          return true;
                if (e.Enemy  == null)                          return true;
                if (e.Host   == null)                          return true;
                if (!e.Host.activeInHierarchy)                 return true;
                if (e.Mc     == null)                          return true;

                // EnemyParent.Spawned == false
                if (_spawnedField != null)
                {
                    var raw = _spawnedField.GetValue(e.Parent);
                    if (raw is bool b && !b) return true;
                }

                // EnemyHealth checks (cached component)
                var health = e.Health;
                if (health != null)
                {
                    if (_deadField != null)
                    {
                        var raw = _deadField.GetValue(health);
                        if (raw is bool dead && dead) return true;
                    }
                    if (_hpField != null)
                    {
                        var raw = _hpField.GetValue(health);
                        if (raw != null && Convert.ToSingle(raw) <= 0f) return true;
                    }
                }
            }
            catch { return true; }
            return false;
        }

        private static void CleanupEntry(int id, MarkerEntry e)
        {
            try
            {
                if (e.Mc != null)
                {
                    if (_mceField != null)
                    {
                        var entity = _mceField.GetValue(e.Mc) as MapCustomEntity;
                        if (entity != null && entity.gameObject != null)
                            UnityEngine.Object.Destroy(entity.gameObject);
                    }
                    UnityEngine.Object.Destroy(e.Mc);
                }
            }
            catch { }
            _registry.Remove(id);
        }
    }
}
