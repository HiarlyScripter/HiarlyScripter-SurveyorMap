using System;
using System.IO;
using System.Text;

namespace SurveyorMap
{
    /// <summary>
    /// Lightweight shared diagnostic state, written to JSON for validators.
    /// Keep this separate from rendering logic.
    /// </summary>
    internal static class SurveyorMapDiagnostics
    {
        public static string AssemblyPath       = "";
        public static string BuildTimestamp     = "";
        public static string BuildMd5Short      = "";
        public static string DiagnosticsDir     = "";
        public static bool   NativeTextureReady = false;
        public static bool   HudVisible         = true;
        public static bool   TabActive          = false;
        public static bool   GameplayActive     = false;
        public static int    TabOpenCount       = 0;
        public static int    TabCloseCount      = 0;
        public static int    ToggleKeyDetectedCount = 0;
        public static int    UpdateCount        = 0;
        public static int    OnGuiCount         = 0;
        public static int    RevealRoomsTotal   = 0;
        public static int    RevealRoomsExplored = 0;
        public static int    EnemyMarkerCount   = 0;
        public static string LastException      = "";

        public static void WriteDiagJson()
        {
            try
            {
                if (string.IsNullOrEmpty(DiagnosticsDir)) return;
                Directory.CreateDirectory(DiagnosticsDir);
                var path = Path.Combine(DiagnosticsDir, "surveyormap-runtime-state.json");

                var sb = new StringBuilder();
                sb.AppendLine("{");
                // Identity
                sb.AppendLine($"  \"assemblyPath\": {J(AssemblyPath)},");
                sb.AppendLine($"  \"buildTimestamp\": {J(BuildTimestamp)},");
                sb.AppendLine($"  \"buildMd5Short\": {J(BuildMd5Short)},");
                sb.AppendLine($"  \"buildTag\": {J(BuildMd5Short)},");
                // Plugin state
                sb.AppendLine($"  \"hudVisible\": {B(HudVisible)},");
                sb.AppendLine($"  \"nativeTextureReady\": {B(NativeTextureReady)},");
                sb.AppendLine($"  \"tabActive\": {B(TabActive)},");
                sb.AppendLine($"  \"nativeTabOpen\": {B(TabActive)},");
                sb.AppendLine($"  \"gameplayActive\": {B(GameplayActive)},");
                sb.AppendLine($"  \"isLevel\": {B(GameplayActive)},");
                sb.AppendLine($"  \"currentRunState\": {J(GameplayActive ? "Level" : "None")},");
                // TAB tracking
                sb.AppendLine($"  \"tabOpenCount\": {TabOpenCount},");
                sb.AppendLine($"  \"tabCloseCount\": {TabCloseCount},");
                // Toggle tracking
                sb.AppendLine($"  \"toggleKeyDetectedCount\": {ToggleKeyDetectedCount},");
                // Feature results
                sb.AppendLine($"  \"revealRoomsTotal\": {RevealRoomsTotal},");
                sb.AppendLine($"  \"revealRoomsExplored\": {RevealRoomsExplored},");
                sb.AppendLine($"  \"enemyMarkerCount\": {EnemyMarkerCount},");
                // Counters
                sb.AppendLine($"  \"pluginUpdateCount\": {UpdateCount},");
                sb.AppendLine($"  \"pluginOnGuiCount\": {OnGuiCount},");
                // Compatibility shims for old validators
                sb.AppendLine($"  \"pluginAwakeCalled\": true,");
                sb.AppendLine($"  \"runtimeProbeCreated\": {B(UpdateCount > 0)},");
                sb.AppendLine($"  \"runtimeProbeUpdateCount\": {UpdateCount},");
                sb.AppendLine($"  \"runtimeProbeOnGuiCount\": {OnGuiCount},");
                sb.AppendLine($"  \"forceHudProofOfLife\": false,");
                sb.AppendLine($"  \"centerOnPlayer\": false,");
                sb.AppendLine($"  \"centerOnPlayerApplied\": false,");
                sb.AppendLine($"  \"capturePaused\": {B(TabActive)},");
                // Error state
                sb.AppendLine($"  \"lastException\": {J(LastException)},");
                sb.AppendLine($"  \"lastErrorStack\": \"\",");
                sb.AppendLine($"  \"writeTimeUtc\": {J(DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"))}");
                sb.Append("}");

                File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
            }
            catch { }
        }

        private static string J(string s) => "\"" + (s ?? "").Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"";
        private static string B(bool v)   => v ? "true" : "false";
    }
}
