using System;
using System.Collections;
using HarmonyLib;
using UnityEngine;

namespace SurveyorMap
{
    [HarmonyPatch(typeof(LevelGenerator), "GenerateDone")]
    internal static class RevealRoomsPatch
    {
        [HarmonyPostfix]
        private static void Postfix()
        {
            // Clear stale enemy markers from previous level before new spawns begin
            EnemyMapMarkerService.ClearAll();

            var mode = SurveyorMapPlugin.Settings.RevealRoomsMode.Value;
            if (!string.Equals(mode, "NativeGlobal", StringComparison.OrdinalIgnoreCase)) return;

            // NativeGlobal: calls RoomVolume.SetExplored() — also reveals in native TAB map (opt-in)
            SurveyorMapPlugin.Instance.StartCoroutine(RevealDelayed());
        }

        private static IEnumerator RevealDelayed()
        {
            // Brief delay — GenerateDone fires before all RoomVolumes are fully initialized
            yield return new WaitForSeconds(0.5f);
            ApplyRevealRooms();
        }

        private static void ApplyRevealRooms()
        {
            try
            {
                var rooms = UnityEngine.Object.FindObjectsOfType<RoomVolume>();
                int total = rooms.Length, explored = 0, skipped = 0;

                foreach (var room in rooms)
                {
                    if (room == null) { skipped++; continue; }
                    try   { if (room.SetExplored()) explored++; }
                    catch (Exception ex)
                    {
                        skipped++;
                        SurveyorMapPlugin.Log.LogWarning($"[SurveyorMap] SetExplored failed: {ex.Message}");
                    }
                }

                SurveyorMapDiagnostics.RevealRoomsTotal    = total;
                SurveyorMapDiagnostics.RevealRoomsExplored = explored;
                SurveyorMapPlugin.Log.LogInfo(
                    $"[SurveyorMap] RevealRooms NativeGlobal: explored={explored}/{total} skipped={skipped}");
            }
            catch (Exception ex)
            {
                SurveyorMapPlugin.Log.LogError($"[SurveyorMap] RevealRooms exception: {ex}");
            }
        }
    }
}
