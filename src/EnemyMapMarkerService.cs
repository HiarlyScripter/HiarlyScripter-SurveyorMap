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

    // Threat tier — determines SHAPE (how immediately dangerous this enemy is).
    // Derived from EnemyParent.difficulty + keyword elevation (never downgraded).
    internal enum ThreatTier { Low = 0, Medium = 1, High = 2, Elite = 3 }

    // Colour family — determines COLOUR (what kind/behaviour of enemy this is).
    // Derived from enemy name/type/class keywords.
    internal enum MarkerFamily { Common = 0, Small = 1, Special = 2, Brute = 3 }

    // Marker shape glyph (one-to-one with ThreatTier)
    internal enum MarkerShape { Circle = 0, Square = 1, Triangle = 2, Star = 3 }

    internal struct MarkerEntry
    {
        public EnemyParent  Parent;
        public Enemy        Enemy;
        public GameObject   Host;
        public MapCustom    Mc;
        public EnemyHealth  Health; // cached — may be null
        public MarkerFamily Family; // colour source (family/behaviour)
        public MarkerShape  Shape;  // shape source (threat tier → glyph)
    }

    // ── Service ───────────────────────────────────────────────────────────────

    internal static class EnemyMapMarkerService
    {
        // Registry keyed by EnemyParent.GetInstanceID()
        private static readonly Dictionary<int, MarkerEntry> _registry = new Dictionary<int, MarkerEntry>();

        // Sprite cache per shape (4 shapes × 1 sprite each — colour applied via MapCustom.color)
        private static readonly Sprite[] _sprites = new Sprite[4];

        // Room-exploration tracking — populated by RoomVolumeExploredPatch observing SetExplored() calls.
        // Used for diagnostics only; room-based marker filtering is DEPRECATED in v1.0.
        private static readonly HashSet<int> _exploredRoomIds    = new HashSet<int>();
        private static bool _anyRoomExplored;        // true once any SetExplored() call observed
        private static int  _exploredLevelRoomCount; // non-truck rooms explored (diagnostic)

        // ── Colours — colour = family/behaviour ──────────────────────────────
        // Common  #1E6BFF  dark blue      (generic humanoid, fallback)
        // Small   #DFFFE8  ice-white green (small/swarm/grabber/utility critters)
        // Special #C084FC  lilac           (weird/supernatural/ranged/odd)
        // Brute   #FF3B30  red/coral       (aggressive/hunter/melee/heavy threat)
        private static readonly Color[] _colors = new Color[]
        {
            new Color(0.118f, 0.420f, 1.000f, 1f), // Common  — #1E6BFF
            new Color(0.875f, 1.000f, 0.910f, 1f), // Small   — #DFFFE8
            new Color(0.753f, 0.518f, 0.988f, 1f), // Special — #C084FC
            new Color(1.000f, 0.231f, 0.188f, 1f), // Brute   — #FF3B30
        };

        // Hex strings for classification log (aligned to _colors / MarkerFamily)
        private static readonly string[] _colorHex = { "#1E6BFF", "#DFFFE8", "#C084FC", "#FF3B30" };

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
        private static FieldInfo    _enemyTypeField;     // Enemy.Type ("light"/"heavy"/"veryheavy")
        private static PropertyInfo _enemyTypeProp;
        private static FieldInfo    _enemyNameField;     // EnemyParent.enemyName (internal tag)
        private static PropertyInfo _enemyNameProp;
        // RoomVolume.explored: NOT reflected — room tracking uses RoomVolumeExploredPatch.

        public static int ActiveMarkerCount => _registry.Count;

        // ── Reflection ───────────────────────────────────────────────────────

        private static void EnsureReflection()
        {
            if (_reflected) return;
            _reflected = true;
            var bf = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

            var ep = typeof(EnemyParent);
            _enemyField     = ep.GetField("Enemy",     bf);
            if (_enemyField == null)
                _enemyProp  = ep.GetProperty("Enemy",  bf);
            _diffField      = ep.GetField("difficulty", bf) ?? ep.GetField("Difficulty", bf);
            _spawnedField   = ep.GetField("Spawned",    bf) ?? ep.GetField("spawned",    bf);
            _enemyNameField = ep.GetField("enemyName",  bf) ?? ep.GetField("EnemyName",  bf);
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

        // ── Name collection (shared by threat + family classifiers) ──────────

        // Collects all meaningful name tokens from the enemy into a single lowercase string.
        // Sources (in order): Enemy.Type enum, EnemyParent.enemyName, C# class name,
        //   enemy gameObject name (skipping "Controller"), EnemyParent gameObject name.
        private static string CollectNames(EnemyParent parent, Enemy enemy)
        {
            var name = "";
            try
            {
                // 1. Enemy.Type — weight/mobility class: "light", "heavy", "veryheavy"
                if (_enemyTypeField != null || _enemyTypeProp != null)
                {
                    var raw = _enemyTypeField != null
                        ? _enemyTypeField.GetValue(enemy)
                        : _enemyTypeProp.GetValue(enemy);
                    if (raw != null) name += " " + raw.ToString();
                }

                // 2. EnemyParent.enemyName — internal tag: "huntsman", "trudge", "clown"
                if (_enemyNameField != null || _enemyNameProp != null)
                {
                    var raw = _enemyNameField != null
                        ? _enemyNameField.GetValue(parent)
                        : _enemyNameProp.GetValue(parent);
                    if (raw != null) name += " " + raw.ToString();
                }

                // 3. C# class name of the Enemy component — e.g. "EnemyBang" → contains "bang"
                if (enemy != null)
                {
                    var csType = enemy.GetType().Name;
                    if (!string.IsNullOrEmpty(csType) && csType != "Enemy")
                        name += " " + csType;
                }

                // 4. Enemy gameObject name — skip "Controller" (generic physics root, not semantic)
                if (enemy != null && enemy.gameObject != null)
                {
                    var egn = enemy.gameObject.name;
                    if (!string.IsNullOrEmpty(egn) &&
                        !string.Equals(egn, "Controller", StringComparison.OrdinalIgnoreCase))
                        name += " " + egn;
                }

                // 5. EnemyParent gameObject name — usually the most descriptive: "Enemy - Hunter(Clone)"
                if (parent != null && parent.gameObject != null)
                    name += " " + parent.gameObject.name;
            }
            catch { }
            return name.ToLower();
        }

        // ── Threat tier: determines SHAPE ────────────────────────────────────
        //
        // Base tier from difficulty int:  1=Low, 2=Medium, 3=High, 0/4+=Elite
        // Keywords can only ELEVATE (never reduce) the tier.
        //
        // Elevation rules:
        //   veryheavy  → at least High  (weight class alone)
        //   trudge / slow walker / boss / elite / apex / king / titan / giant / mega / master
        //              → Elite
        //   hunt / huntsman / hunter / bang / rush / charge / attack / screamer / smasher / bowtie / robe
        //              → at least High

        private static ThreatTier GetThreatTier(EnemyParent parent, string names,
                                                out ThreatTier baseTier, out string elevatedBy)
        {
            baseTier   = ThreatTier.Elite; // safe fallback if reflection fails
            elevatedBy = "none";

            try
            {
                if (_diffField != null)
                {
                    var raw = _diffField.GetValue(parent);
                    if (raw != null)
                    {
                        int d = Convert.ToInt32(raw);
                        baseTier = d == 1 ? ThreatTier.Low
                                 : d == 2 ? ThreatTier.Medium
                                 : d == 3 ? ThreatTier.High
                                 : ThreatTier.Elite;
                    }
                }
            }
            catch { }

            var elevated = baseTier;

            // veryheavy weight class → at least High
            if (names.Contains("veryheavy") && elevated < ThreatTier.High)
            {
                elevated = ThreatTier.High;
                elevatedBy = "veryheavy";
            }

            // Named critical threats → Elite
            if (elevated < ThreatTier.Elite &&
                ContainsAny(names, "trudge", "slow walker", "boss", "elite", "apex",
                                   "king", "titan", "giant", "mega", "master"))
            {
                elevated = ThreatTier.Elite;
                elevatedBy = FirstMatch(names, "trudge", "slow walker", "boss", "elite",
                                               "apex", "king", "titan", "giant", "mega", "master");
            }

            // Named high threats → at least High
            if (elevated < ThreatTier.High &&
                ContainsAny(names, "hunt", "huntsman", "hunter", "bang", "rush", "charge",
                                   "attack", "screamer", "smasher", "bowtie", "robe"))
            {
                elevated = ThreatTier.High;
                if (elevatedBy == "none")
                    elevatedBy = FirstMatch(names, "hunt", "huntsman", "hunter", "bang", "rush",
                                                   "charge", "attack", "screamer", "smasher", "bowtie", "robe");
            }

            return elevated;
        }

        // Shape glyph from threat tier (direct one-to-one mapping)
        private static MarkerShape TierToShape(ThreatTier tier)
        {
            switch (tier)
            {
                case ThreatTier.Low:    return MarkerShape.Circle;
                case ThreatTier.Medium: return MarkerShape.Square;
                case ThreatTier.High:   return MarkerShape.Triangle;
                case ThreatTier.Elite:  return MarkerShape.Star;
                default:                return MarkerShape.Circle;
            }
        }

        // ── Colour family: determines COLOUR ─────────────────────────────────
        //
        // Brute   (red/coral)      — aggressive hunters, melee, heavy threats
        // Special (lilac)          — weird, supernatural, ranged, odd behaviour
        // Small   (ice-green)      — small, swarm, grabbers, throwers, critters
        // Common  (dark blue)      — fallback, generic humanoids, unknown types

        private static MarkerFamily GetColorFamily(string names)
        {
            // Brute — aggressive/melee/heavy
            if (ContainsAny(names, "hunt", "huntsman", "hunter", "trudge", "slow walker",
                                   "bang", "rush", "charge", "attack", "smasher", "bowtie",
                                   "screamer", "robe", "raider"))
                return MarkerFamily.Brute;

            // Special — weird/supernatural/ranged/utility
            if (ContainsAny(names, "clown", "beamer", "elsa", "birthday", "oogly", "mentalist",
                                   "ghost", "float", "eye", "shadow", "duck", "weird",
                                   "flower", "reap", "spider", "turret", "hidden", "janitor"))
                return MarkerFamily.Special;

            // Small — small critters, grabbers, throwers
            if (ContainsAny(names, "headgrab", "rugrat", "thrower", "gnome", "slug", "grabber",
                                   "baby", "child", "pet", "swarm", "small", "creep", "krild"))
                return MarkerFamily.Small;

            return MarkerFamily.Common;
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private static bool ContainsAny(string haystack, params string[] needles)
        {
            foreach (var n in needles)
                if (haystack.Contains(n)) return true;
            return false;
        }

        private static string FirstMatch(string haystack, params string[] needles)
        {
            foreach (var n in needles)
                if (haystack.Contains(n)) return n;
            return "?";
        }

        // ── Sprite generation ─────────────────────────────────────────────────
        //
        // Circle   = Low threat  (filled disc)
        // Square   = Med threat  (filled square — replaces old Diamond/losango)
        // Triangle = High threat (upward-pointing, narrow apex / wide base)
        // Star     = Elite threat (6-point star: two overlapping triangles)

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

                case MarkerShape.Square:
                    // Filled square with 2-px margin on each side
                    for (int py = 2; py <= 13; py++)
                    for (int pxc = 2; pxc <= 13; pxc++)
                        px[py * 16 + pxc] = Color.white;
                    break;

                case MarkerShape.Triangle:
                    // Upward-pointing — narrow apex at top (py=2), wide base at py=14
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
            // Always enforce current config value — no early-return — so REPOConfig changes
            // take effect within the next sweep cycle (~2 seconds), no restart required.
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

        // ── Pre-add filter — refuse to create a marker for dead/inactive enemies ──

        private static bool IsAddAllowed(EnemyParent parent, Enemy enemy)
        {
            try
            {
                if (parent == null || enemy == null)               return false;
                if (!parent.gameObject.activeInHierarchy)          return false;
                if (!enemy.gameObject.activeInHierarchy)           return false;

                if (_spawnedField != null)
                {
                    var raw = _spawnedField.GetValue(parent);
                    if (raw is bool b && !b)
                    {
                        SurveyorMapPlugin.Log.LogDebug("[SurveyorMap] AddMarker skipped: Spawned=false");
                        return false;
                    }
                }

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
            catch { return true; } // fail-safe: allow on any reflection error
            return true;
        }

        // ── Room-exploration (diagnostics only — filtering deprecated in v1.0) ──────

        // NOTE: ShowEnemiesInUnexploredRooms is DEPRECATED in v1.0.
        // Room-based marker filtering was unreliable in Vanilla mode (game calls SetExplored
        // on the truck room but not on level rooms), causing regressions. All live markers
        // are now shown unconditionally. This method is retained for future use.
        private static bool IsEnemyRoomExplored(Enemy enemy)
        {
            if (enemy == null || !enemy.gameObject) return true;
            if (!_anyRoomExplored || _exploredLevelRoomCount == 0) return true;
            try
            {
                var cols = Physics.OverlapSphere(
                    enemy.transform.position, 2f, ~0, QueryTriggerInteraction.Collide);
                foreach (var col in cols)
                {
                    var rv = col.GetComponent<RoomVolume>();
                    if (rv == null) continue;
                    return _exploredRoomIds.Contains(rv.GetInstanceID());
                }
            }
            catch { }
            return true;
        }

        // Called by RoomVolumeExploredPatch — logs unique rooms only (deduplicates spam).
        public static void OnRoomExplored(RoomVolume room)
        {
            if (room == null) return;
            bool isNew = _exploredRoomIds.Add(room.GetInstanceID());
            if (!_anyRoomExplored) _anyRoomExplored = true;
            if (isNew)
            {
                bool isTruck = room.gameObject.name.IndexOf("Truck", StringComparison.OrdinalIgnoreCase) >= 0;
                if (!isTruck) _exploredLevelRoomCount++;
                SurveyorMapPlugin.Log.LogInfo(
                    $"[SurveyorMap] Room explored (new): {room.gameObject.name}" +
                    $" isTruck={isTruck} levelRooms={_exploredLevelRoomCount} total={_exploredRoomIds.Count}");
            }
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

                if (!IsAddAllowed(parent, enemy)) return;

                // Host: prefer rigidbody child for accurate world-space position on the map
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

                // ── Classify: collect names, derive threat tier + colour family ──
                string names = CollectNames(parent, enemy);
                ThreatTier baseTier;
                string     elevatedBy;
                ThreatTier threatTier = GetThreatTier(parent, names, out baseTier, out elevatedBy);
                MarkerFamily family   = GetColorFamily(names);
                MarkerShape  shape    = TierToShape(threatTier);
                Color        color    = _colors[(int)family];
                Sprite       sprite   = GetOrCreateSprite(shape);

                var mc = host.GetComponent<MapCustom>() ?? host.AddComponent<MapCustom>();
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

                if (_mceField != null && _mceField.GetValue(mc) == null)
                    SurveyorMapPlugin.Log.LogWarning(
                        $"[SurveyorMap] mapCustomEntity null after AddCustom — marker may be invisible. host={host.name}");

                var health = enemy.GetComponentInChildren<EnemyHealth>(true)
                          ?? host.GetComponentInChildren<EnemyHealth>(true);

                _registry[id] = new MarkerEntry
                {
                    Parent = parent,
                    Enemy  = enemy,
                    Host   = host,
                    Mc     = mc,
                    Health = health,
                    Family = family,
                    Shape  = shape,
                };

                // Detailed classification log — use to tune keywords post-gameplay
                SurveyorMapPlugin.Log.LogDebug(
                    $"[SurveyorMap] Enemy marker: id={id}" +
                    $" diff={baseTier} threat={threatTier} shape={shape}" +
                    $" family={family} color={_colorHex[(int)family]}" +
                    $" elevated={elevatedBy} names=[{names.Trim()}] total={_registry.Count}");
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

            // 2. Ensure all live markers are active + scale is current.
            // ShowEnemiesInUnexploredRooms is DEPRECATED — room filtering removed entirely.
            // All valid (non-stale) markers are always shown.
            foreach (var kv in _registry)
            {
                SetMarkerEntityActive(kv.Value.Mc, true);
                ApplyScale(kv.Value.Mc);
            }
            SurveyorMapPlugin.Log.LogDebug(
                $"[SurveyorMap] Sweep: active={_registry.Count}");
        }

        // Called on GenerateDone to reset state between levels
        public static void ClearAll()
        {
            var ids = new List<int>(_registry.Keys);
            foreach (int id in ids)
                if (_registry.TryGetValue(id, out var entry))
                    CleanupEntry(id, entry);
            _exploredRoomIds.Clear();
            _anyRoomExplored = false;
            _exploredLevelRoomCount = 0;
        }

        // ── Internal ─────────────────────────────────────────────────────────

        private static bool IsEntryStale(MarkerEntry e)
        {
            try
            {
                if (e.Parent == null)          return true;
                if (e.Enemy  == null)          return true;
                if (e.Host   == null)          return true;
                if (!e.Host.activeInHierarchy) return true;
                if (e.Mc     == null)          return true;

                if (_spawnedField != null)
                {
                    var raw = _spawnedField.GetValue(e.Parent);
                    if (raw is bool b && !b) return true;
                }

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
