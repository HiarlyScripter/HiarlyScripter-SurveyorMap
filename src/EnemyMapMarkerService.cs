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

    // Observes every SetExplored() call (native game or NativeGlobal) to track explored rooms.
    // We NEVER call SetExplored() ourselves here — we just watch when the game does.
    [HarmonyPatch(typeof(RoomVolume), "SetExplored")]
    internal static class RoomVolumeExploredPatch
    {
        [HarmonyPostfix]
        private static void Postfix(RoomVolume __instance)
        {
            EnemyMapMarkerService.OnRoomExplored(__instance);
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

    // ── Data types ────────────────────────────────────────────────────────────

    // Difficulty tier → controls COLOUR
    internal enum MarkerCategory { Easy = 0, Medium = 1, Hard = 2, Elite = 3 }

    // Enemy family/type → controls SHAPE
    internal enum MarkerShape    { Circle = 0, Triangle = 1, Diamond = 2, Star = 3 }

    internal struct MarkerEntry
    {
        public EnemyParent    Parent;
        public Enemy          Enemy;
        public GameObject     Host;
        public MapCustom      Mc;
        public EnemyHealth    Health; // cached — may be null
        public MarkerCategory Tier;   // colour source (difficulty)
        public MarkerShape    Shape;  // shape source  (type/name)
    }

    // ── Service ───────────────────────────────────────────────────────────────

    internal static class EnemyMapMarkerService
    {
        // Registry keyed by EnemyParent.GetInstanceID()
        private static readonly Dictionary<int, MarkerEntry> _registry = new Dictionary<int, MarkerEntry>();

        // Sprite cache per shape (4 shapes × 1 sprite each — colour is applied via MapCustom.color)
        private static readonly Sprite[] _sprites = new Sprite[4];

        // Room-exploration tracking — populated by RoomVolumeExploredPatch observing SetExplored() calls.
        // Key = RoomVolume.GetInstanceID(). Fail-open: if no level rooms ever explored, show all markers.
        private static readonly HashSet<int> _exploredRoomIds = new HashSet<int>();
        private static bool _anyRoomExplored;       // true once any SetExplored() call observed (incl. truck)
        private static int  _exploredLevelRoomCount; // non-truck rooms explored; 0 = level tracking inactive

        // ── Colours — colour = danger/difficulty ─────────────────────────────
        // Easy  #DFFFE8  verde-gelo quase branco  (low danger)
        // Med   #3DA5FF  azul/ciano               (medium danger)
        // Hard  #9B5CFF  roxo/violeta             (high danger)
        // Elite #FF3B30  vermelho/coral forte     (critical danger)
        private static readonly Color[] _colors = new Color[]
        {
            new Color(0.875f, 1.000f, 0.910f, 1f), // Easy  — #DFFFE8
            new Color(0.239f, 0.647f, 1.000f, 1f), // Medium— #3DA5FF
            new Color(0.608f, 0.361f, 1.000f, 1f), // Hard  — #9B5CFF
            new Color(1.000f, 0.231f, 0.188f, 1f), // Elite — #FF3B30
        };

        // ── Reflection cache ─────────────────────────────────────────────────
        private static bool         _reflected;
        private static FieldInfo    _enemyField;
        private static PropertyInfo _enemyProp;
        private static FieldInfo    _rbField;
        private static FieldInfo    _hasRbField;
        private static FieldInfo    _mceField;           // MapCustom.mapCustomEntity
        private static FieldInfo    _diffField;          // EnemyParent.difficulty
        private static FieldInfo    _spawnedField;       // EnemyParent.Spawned
        private static FieldInfo    _deadField;          // EnemyHealth.dead
        private static FieldInfo    _hpField;            // EnemyHealth.healthCurrent
        private static FieldInfo    _autoAddField;       // MapCustom.autoAdd
        private static FieldInfo    _currentStateField;  // Enemy.CurrentState
        private static PropertyInfo _currentStateProp;
        private static FieldInfo    _enemyTypeField;     // Enemy.Type / EnemyType
        private static PropertyInfo _enemyTypeProp;
        private static FieldInfo    _enemyNameField;     // EnemyParent.enemyName
        private static PropertyInfo _enemyNameProp;
        // Note: RoomVolume.explored is no longer accessed via reflection.
        // Room tracking now uses RoomVolumeExploredPatch + _exploredRoomIds HashSet.

        public static int ActiveMarkerCount => _registry.Count;

        // ── Reflection ───────────────────────────────────────────────────────

        private static void EnsureReflection()
        {
            if (_reflected) return;
            _reflected = true;
            var bf = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

            var ep = typeof(EnemyParent);
            _enemyField   = ep.GetField("Enemy",     bf);
            if (_enemyField == null)
                _enemyProp = ep.GetProperty("Enemy", bf);
            _diffField    = ep.GetField("difficulty", bf) ?? ep.GetField("Difficulty", bf);
            _spawnedField = ep.GetField("Spawned",    bf) ?? ep.GetField("spawned",    bf);
            _enemyNameField = ep.GetField("enemyName", bf) ?? ep.GetField("EnemyName", bf);
            if (_enemyNameField == null)
                _enemyNameProp = ep.GetProperty("enemyName", bf) ?? ep.GetProperty("EnemyName", bf);

            var en = typeof(Enemy);
            _rbField           = en.GetField("Rigidbody",    bf);
            _hasRbField        = en.GetField("HasRigidbody", bf);
            _currentStateField = en.GetField("CurrentState", bf) ?? en.GetField("currentState", bf);
            if (_currentStateField == null)
                _currentStateProp = en.GetProperty("CurrentState", bf) ?? en.GetProperty("currentState", bf);
            _enemyTypeField   = en.GetField("Type",      bf) ?? en.GetField("type",      bf)
                             ?? en.GetField("EnemyType", bf);
            if (_enemyTypeField == null)
                _enemyTypeProp = en.GetProperty("Type", bf) ?? en.GetProperty("EnemyType", bf);

            _mceField = typeof(MapCustom).GetField("mapCustomEntity",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            _autoAddField = typeof(MapCustom).GetField("autoAdd", bf)
                         ?? typeof(MapCustom).GetField("AutoAdd", bf);

            var eh = typeof(EnemyHealth);
            _deadField = eh.GetField("dead",          bf) ?? eh.GetField("Dead",          bf);
            _hpField   = eh.GetField("healthCurrent", bf) ?? eh.GetField("HealthCurrent", bf)
                      ?? eh.GetField("HP",            bf) ?? eh.GetField("hp",            bf);

            // RoomVolume.explored is NOT reflected — room tracking uses RoomVolumeExploredPatch.
        }

        // ── Accessors ────────────────────────────────────────────────────────

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

        // ── Colour: derived from EnemyParent.difficulty ───────────────────────

        private static MarkerCategory GetTier(EnemyParent parent)
        {
            try
            {
                if (_diffField != null)
                {
                    var raw = _diffField.GetValue(parent);
                    if (raw != null)
                    {
                        int d = Convert.ToInt32(raw);
                        if (d == 1) return MarkerCategory.Easy;
                        if (d == 2) return MarkerCategory.Medium;
                        if (d == 3) return MarkerCategory.Hard;
                        return MarkerCategory.Elite; // 0 / 4+ / unknown → critical
                    }
                }
            }
            catch { }
            return MarkerCategory.Elite;
        }

        // ── Shape: derived from Enemy.Type / enemyName / C# type name / GO names ──
        // NOTE: host is intentionally EXCLUDED — it is always the physics root "Controller",
        //       which is generic and actively misleads classification.

        private static MarkerShape GetShape(EnemyParent parent, Enemy enemy, GameObject host)
        {
            var name = "";
            try
            {
                // 1. Enemy.Type enum — most reliable semantic tag if the game populates it
                if (_enemyTypeField != null || _enemyTypeProp != null)
                {
                    var raw = _enemyTypeField != null
                        ? _enemyTypeField.GetValue(enemy)
                        : _enemyTypeProp.GetValue(enemy);
                    if (raw != null) name += " " + raw.ToString();
                }

                // 2. EnemyParent.enemyName — designer-assigned name, e.g. "Huntsman", "Bang"
                if (_enemyNameField != null || _enemyNameProp != null)
                {
                    var raw = _enemyNameField != null
                        ? _enemyNameField.GetValue(parent)
                        : _enemyNameProp.GetValue(parent);
                    if (raw != null) name += " " + raw.ToString();
                }

                // 3. C# class name of the Enemy component — reliable even without reflection data
                //    e.g. "EnemyBang" → "bang", "EnemyHunter" → "hunt"
                if (enemy != null)
                {
                    var csharpType = enemy.GetType().Name;
                    if (!string.IsNullOrEmpty(csharpType) && csharpType != "Enemy")
                        name += " " + csharpType;
                }

                // 4. Enemy gameObject name (prefab instance name, not physics root)
                if (enemy != null && enemy.gameObject != null)
                    name += " " + enemy.gameObject.name;

                // 5. EnemyParent gameObject name (highest-level name, often most descriptive)
                if (parent != null && parent.gameObject != null)
                    name += " " + parent.gameObject.name;

                // Host intentionally omitted — it is named "Controller" (physics rigidbody root)
                // and provides no useful semantic signal for enemy classification.
            }
            catch { }

            name = name.ToLower();
            SurveyorMapPlugin.Log.LogDebug($"[SurveyorMap] GetShape names: [{name.Trim()}]");

            // Star — boss / elite / extreme threat
            if (ContainsAny(name, "boss", "elite", "apex", "king", "titan", "lord", "chief",
                                  "master", "giant", "mega", "alpha", "omega", "reaper"))
                return MarkerShape.Star;

            // Triangle — hunter / aggressive / pursuer / melee
            if (ContainsAny(name, "hunt", "bang", "attack", "rush", "charge", "charg",
                                  "chase", "stalk", "crawl", "bowtie", "runner", "raider",
                                  "robe", "upscream", "screamer", "smasher"))
                return MarkerShape.Triangle;

            // Diamond — special / support / ranged / strange / stealth
            if (ContainsAny(name, "shadow", "duck", "child", "baby", "clown", "ghost",
                                  "float", "eye", "mouth", "pet", "creep", "mentalist",
                                  "support", "flower", "reap", "special", "weird", "krild",
                                  "hidden", "janitor", "spider", "slug", "trap", "turret"))
                return MarkerShape.Diamond;

            // Circle — common / basic / neutral fallback
            return MarkerShape.Circle;
        }

        private static bool ContainsAny(string haystack, params string[] needles)
        {
            foreach (var n in needles)
                if (haystack.Contains(n)) return true;
            return false;
        }

        // ── Sprite generation (shape only — colour applied via mc.color) ──────

        private static Sprite GetOrCreateSprite(MarkerShape shape)
        {
            int idx = (int)shape;
            if (_sprites[idx] != null && _sprites[idx]) return _sprites[idx];

            var tex = new Texture2D(16, 16, TextureFormat.RGBA32, false);
            tex.hideFlags = HideFlags.HideAndDontSave;
            var px = new Color[256];
            for (int i = 0; i < 256; i++) px[i] = Color.clear;

            switch (shape)
            {
                case MarkerShape.Circle:
                    for (int py = 0; py < 16; py++)
                    for (int pxc = 0; pxc < 16; pxc++)
                        if (Vector2.Distance(new Vector2(pxc, py), new Vector2(7.5f, 7.5f)) <= 6.5f)
                            px[py * 16 + pxc] = Color.white;
                    break;

                case MarkerShape.Diamond:
                    for (int py = 0; py < 16; py++)
                    for (int pxc = 0; pxc < 16; pxc++)
                        if (Mathf.Abs(pxc - 7.5f) + Mathf.Abs(py - 7.5f) <= 6.5f)
                            px[py * 16 + pxc] = Color.white;
                    break;

                case MarkerShape.Triangle:
                    // Upward-pointing — apex at top, base at bottom
                    for (int py = 2; py <= 14; py++)
                    {
                        float halfW = (py - 2f) / 12f * 6.5f;
                        for (int pxc = 0; pxc < 16; pxc++)
                            if (Mathf.Abs(pxc - 7.5f) <= halfW)
                                px[py * 16 + pxc] = Color.white;
                    }
                    break;

                case MarkerShape.Star:
                    // 6-point star: upward triangle ∪ downward triangle
                    for (int py = 2; py <= 12; py++)
                    {
                        float halfW = (py - 2f) / 10f * 5.5f;
                        for (int pxc = 0; pxc < 16; pxc++)
                            if (Mathf.Abs(pxc - 7.5f) <= halfW)
                                px[py * 16 + pxc] = Color.white;
                    }
                    for (int py = 4; py <= 14; py++)
                    {
                        float halfW = (14f - py) / 10f * 5.5f;
                        for (int pxc = 0; pxc < 16; pxc++)
                            if (Mathf.Abs(pxc - 7.5f) <= halfW)
                                px[py * 16 + pxc] = Color.white;
                    }
                    break;
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
            // No early-return for size=1.0 — always enforce current config value so
            // changes made in REPOConfig take effect within the next sweep cycle (~2s).
            try
            {
                if (mc == null || _mceField == null) return;
                var entity = _mceField.GetValue(mc) as MapCustomEntity;
                if (entity == null || entity.gameObject == null) return;
                float size = Mathf.Clamp(SurveyorMapPlugin.Settings.EnemyMarkerSize.Value, 0.30f, 2.00f);
                entity.transform.localScale = Vector3.one * size;
            }
            catch { }
        }

        // ── Pre-add filter — refuse to create a marker for dead/inactive enemies

        private static bool IsAddAllowed(EnemyParent parent, Enemy enemy)
        {
            try
            {
                if (parent == null || enemy == null)               return false;
                if (!parent.gameObject.activeInHierarchy)          return false;
                if (!enemy.gameObject.activeInHierarchy)           return false;

                // EnemyParent.Spawned
                if (_spawnedField != null)
                {
                    var raw = _spawnedField.GetValue(parent);
                    if (raw is bool b && !b)
                    {
                        SurveyorMapPlugin.Log.LogDebug("[SurveyorMap] AddMarker skipped: Spawned=false");
                        return false;
                    }
                }

                // Enemy.CurrentState == Despawn
                if (_currentStateField != null || _currentStateProp != null)
                {
                    var state = _currentStateField != null
                        ? _currentStateField.GetValue(enemy)
                        : _currentStateProp.GetValue(enemy);
                    if (state != null && state.ToString() == "Despawn")
                    {
                        SurveyorMapPlugin.Log.LogDebug("[SurveyorMap] AddMarker skipped: CurrentState=Despawn");
                        return false;
                    }
                }

                // EnemyHealth.dead / healthCurrent
                var health = enemy.GetComponentInChildren<EnemyHealth>(true);
                if (health != null)
                {
                    if (_deadField != null)
                    {
                        var raw = _deadField.GetValue(health);
                        if (raw is bool dead && dead)
                        {
                            SurveyorMapPlugin.Log.LogDebug("[SurveyorMap] AddMarker skipped: dead=true");
                            return false;
                        }
                    }
                    if (_hpField != null)
                    {
                        var raw = _hpField.GetValue(health);
                        if (raw != null && Convert.ToSingle(raw) <= 0f)
                        {
                            SurveyorMapPlugin.Log.LogDebug("[SurveyorMap] AddMarker skipped: healthCurrent<=0");
                            return false;
                        }
                    }
                }
            }
            catch { return true; } // fail-safe: allow
            return true;
        }

        // ── Room-exploration visibility ───────────────────────────────────────

        // Returns true (show) if the room is explored, or if exploration state cannot be determined (fail-open).
        //
        // Fail-open rules (in order):
        //   1. No SetExplored ever observed → show (exploration system completely inactive).
        //   2. Only truck/lobby rooms explored (_exploredLevelRoomCount == 0) → show
        //      (game calls SetExplored on truck every frame, but NOT on actual level rooms in Vanilla mode;
        //       truck-only tracking is not useful for level-room filtering).
        //   3. Level rooms tracked and enemy's room is in the explored set → show.
        //   4. Level rooms tracked and enemy's room is NOT in explored set → hide (room unexplored).
        //   5. No room found at enemy position → show (fail-open).
        private static bool IsEnemyRoomExplored(Enemy enemy)
        {
            if (enemy == null || !enemy.gameObject) return true;

            // Guard 1: no SetExplored calls at all
            if (!_anyRoomExplored) return true;

            // Guard 2: only lobby/truck explored — level exploration not tracked in Vanilla mode → fail-open
            if (_exploredLevelRoomCount == 0) return true;

            try
            {
                var cols = Physics.OverlapSphere(
                    enemy.transform.position, 2f, ~0, QueryTriggerInteraction.Collide);
                foreach (var col in cols)
                {
                    var rv = col.GetComponent<RoomVolume>();
                    if (rv == null) continue;
                    bool explored = _exploredRoomIds.Contains(rv.GetInstanceID());
                    SurveyorMapPlugin.Log.LogDebug(
                        $"[SurveyorMap] RoomCheck: {rv.gameObject.name} explored={explored}");
                    return explored;
                }
            }
            catch { }
            return true; // no room found at enemy position → show (fail-open)
        }

        // Called by RoomVolumeExploredPatch when the game or NativeGlobal calls SetExplored().
        // We observe without mutating anything. Log only on FIRST observation of each room (dedup).
        public static void OnRoomExplored(RoomVolume room)
        {
            if (room == null) return;
            bool isNew = _exploredRoomIds.Add(room.GetInstanceID()); // HashSet.Add returns false if already present
            if (!_anyRoomExplored) _anyRoomExplored = true;

            if (isNew)
            {
                // "Truck" rooms are the lobby/staging area — not actual level rooms.
                // We only count non-truck rooms for "level exploration active" determination.
                bool isTruck = room.gameObject.name.IndexOf("Truck", StringComparison.OrdinalIgnoreCase) >= 0;
                if (!isTruck) _exploredLevelRoomCount++;
                SurveyorMapPlugin.Log.LogInfo(
                    $"[SurveyorMap] Room explored (new): {room.gameObject.name}" +
                    $" isTruck={isTruck} levelRooms={_exploredLevelRoomCount} total={_exploredRoomIds.Count}");
            }
            // Duplicate SetExplored calls (same room) are silently discarded to avoid log spam.
        }

        private static void SetMarkerEntityActive(MapCustom mc, bool active)
        {
            try
            {
                if (mc == null || _mceField == null) return;
                var entity = _mceField.GetValue(mc) as MapCustomEntity;
                if (entity != null && entity.gameObject != null)
                    entity.gameObject.SetActive(active);
            }
            catch { }
        }

        // ── Public API ───────────────────────────────────────────────────────

        public static void AddMarker(EnemyParent parent)
        {
            try
            {
                EnsureReflection();
                int id = parent.GetInstanceID();
                if (_registry.ContainsKey(id)) return;

                var enemy = GetEnemy(parent);
                if (enemy == null || !enemy.gameObject) return;

                // Pre-filter: refuse to add marker for dead / inactive / despawned enemy
                if (!IsAddAllowed(parent, enemy)) return;

                // Host: prefer rigidbody child
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

                var tier   = GetTier(parent);
                var shape  = GetShape(parent, enemy, host);
                var sprite = GetOrCreateSprite(shape);
                var color  = _colors[(int)tier];

                var mc = host.GetComponent<MapCustom>() ?? host.AddComponent<MapCustom>();
                // Prevent auto-registration before our explicit AddCustom call
                if (_autoAddField != null)
                    try { _autoAddField.SetValue(mc, false); } catch { }
                mc.sprite = sprite;
                mc.color  = color;

                try
                {
                    if (Map.Instance != null)
                    {
                        Map.Instance.AddCustom(mc, sprite, color);
                        SurveyorMapPlugin.Log.LogDebug($"[SurveyorMap] Map.AddCustom OK: host={host.name}");
                    }
                    else
                    {
                        SurveyorMapPlugin.Log.LogWarning("[SurveyorMap] Map.AddCustom skipped: Map.Instance is null");
                    }
                }
                catch (Exception ex)
                {
                    SurveyorMapPlugin.Log.LogWarning($"[SurveyorMap] Map.AddCustom FAILED: {ex.Message}");
                }

                ApplyScale(mc);

                // Diagnostic: warn if the map entity was not created (field name mismatch or AddCustom failed)
                if (_mceField != null && _mceField.GetValue(mc) == null)
                    SurveyorMapPlugin.Log.LogWarning(
                        $"[SurveyorMap] mapCustomEntity is null after AddCustom — marker may be invisible. host={host.name}");

                var health = enemy.GetComponentInChildren<EnemyHealth>(true)
                          ?? host.GetComponentInChildren<EnemyHealth>(true);

                _registry[id] = new MarkerEntry
                {
                    Parent = parent,
                    Enemy  = enemy,
                    Host   = host,
                    Mc     = mc,
                    Health = health,
                    Tier   = tier,
                    Shape  = shape,
                };

                SurveyorMapPlugin.Log.LogDebug(
                    $"[SurveyorMap] Enemy marker added: id={id} tier={tier} shape={shape} host={host.name} total={_registry.Count}");
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

            // 1. Remove stale (dead/inactive/despawned)
            var toRemove = new List<int>();
            foreach (var kv in _registry)
                if (IsEntryStale(kv.Value)) toRemove.Add(kv.Key);
            foreach (int id in toRemove)
                if (_registry.TryGetValue(id, out var entry))
                    CleanupEntry(id, entry);
            if (toRemove.Count > 0)
                SurveyorMapPlugin.Log.LogDebug(
                    $"[SurveyorMap] Sweep removed {toRemove.Count} stale markers. Active={_registry.Count}");

            // 2. Update room-exploration visibility + scale for live markers
            bool showUnexplored = SurveyorMapPlugin.Settings.ShowEnemiesInUnexploredRooms.Value;
            int nVisible = 0, nHidden = 0;
            foreach (var kv in _registry)
            {
                bool visible = showUnexplored || IsEnemyRoomExplored(kv.Value.Enemy);
                SetMarkerEntityActive(kv.Value.Mc, visible);
                ApplyScale(kv.Value.Mc); // re-apply scale every sweep so config changes take effect
                if (visible) nVisible++; else nHidden++;
            }
            if (_registry.Count > 0)
                SurveyorMapPlugin.Log.LogDebug(
                    $"[SurveyorMap] SweepVisibility: showUnexplored={showUnexplored}" +
                    $" anyRoom={_anyRoomExplored} levelRooms={_exploredLevelRoomCount}" +
                    $" visible={nVisible} hidden={nHidden}");
        }

        // Called on GenerateDone to reset state between levels
        public static void ClearAll()
        {
            var ids = new List<int>(_registry.Keys);
            foreach (int id in ids)
                if (_registry.TryGetValue(id, out var entry))
                    CleanupEntry(id, entry);
            // Reset room-exploration tracking for new level
            _exploredRoomIds.Clear();
            _anyRoomExplored = false;
            _exploredLevelRoomCount = 0;
        }

        // ── Internal ─────────────────────────────────────────────────────────

        private static bool IsEntryStale(MarkerEntry e)
        {
            try
            {
                if (e.Parent == null)              return true;
                if (e.Enemy  == null)              return true;
                if (e.Host   == null)              return true;
                if (!e.Host.activeInHierarchy)     return true;
                if (e.Mc     == null)              return true;

                // EnemyParent.Spawned == false
                if (_spawnedField != null)
                {
                    var raw = _spawnedField.GetValue(e.Parent);
                    if (raw is bool b && !b) return true;
                }

                // Enemy.CurrentState == Despawn
                if (_currentStateField != null || _currentStateProp != null)
                {
                    try
                    {
                        var state = _currentStateField != null
                            ? _currentStateField.GetValue(e.Enemy)
                            : _currentStateProp.GetValue(e.Enemy);
                        if (state != null && state.ToString() == "Despawn") return true;
                    }
                    catch { }
                }

                // EnemyHealth.dead / healthCurrent
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
