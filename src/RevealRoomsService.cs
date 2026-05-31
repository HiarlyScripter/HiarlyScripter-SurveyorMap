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
            if (!SurveyorMapPlugin.Settings.RevealRooms.Value) return;
            SurveyorMapPlugin.Instance.StartCoroutine(RevealDelayed());
        }

        private static IEnumerator RevealDelayed()
        {
            // Brief delay — GenerateDone fires before all RoomVolumes may be fully initialized
            yield return new WaitForSeconds(0.5f);
            ApplyRevealRooms();
        }

        private static void ApplyRevealRooms()
        {
            try
            {
                var rooms = UnityEngine.Object.FindObjectsOfType<RoomVolume>();
                int total       = rooms.Length;
                int explored    = 0;
                int skipped     = 0;

                foreach (var room in rooms)
                {
                    if (room == null) { skipped++; continue; }
                    try
                    {
                        bool result = room.SetExplored();
                        if (result) explored++;
                    }
                    catch (Exception ex)
                    {
                        skipped++;
                        SurveyorMapPlugin.Log.LogWarning($"[SurveyorMap] SetExplored failed: {ex.Message}");
                    }
                }

                SurveyorMapDiagnostics.RevealRoomsTotal    = total;
                SurveyorMapDiagnostics.RevealRoomsExplored = explored;
                SurveyorMapPlugin.Log.LogInfo(
                    $"[SurveyorMap] RevealRooms: explored={explored}/{total} skipped={skipped}");
            }
            catch (Exception ex)
            {
                SurveyorMapPlugin.Log.LogError($"[SurveyorMap] RevealRooms exception: {ex}");
            }
        }
    }
}
