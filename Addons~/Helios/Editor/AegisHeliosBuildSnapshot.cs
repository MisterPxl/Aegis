using System.IO;
using Astra.Aegis;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Astra.Aegis.Integrations.Helios.Editor
{
    [UnityEngine.Scripting.APIUpdating.MovedFrom(false, "MisterPxl.Aegis.HeliosIntegration.Editor", "Aegis.Helios.Editor", "AegisHeliosBuildSnapshot")]
    public sealed class AegisHeliosBuildSnapshot : IPreprocessBuildWithReport, IPostprocessBuildWithReport
    {
        private const string ResourceFolder = "Assets/Resources";
        private const string SnapshotPath = "Assets/Resources/AegisValidationSnapshot.json";

        public int callbackOrder => -4900;

        [InitializeOnLoadMethod]
        private static void CleanUpLeftoverSnapshot()
        {
            // A failed build never reaches OnPostprocessBuild, so a stale snapshot can
            // remain in Assets/ (and end up committed). Remove it on the next domain reload.
            if (!BuildPipeline.isBuildingPlayer)
                DeleteSnapshot();
        }

        public void OnPreprocessBuild(BuildReport report)
        {
            AegisSettings settings = AegisSettings.instance;
            AegisValidationProfile profile = settings.GetProfile("Build");
            AegisValidationReport validationReport = GetValidationReport(settings, profile);
            if (validationReport == null)
                return;

            AegisValidationSnapshot snapshot = new AegisValidationSnapshot
            {
                profile = validationReport.ProfileName,
                generatedUtc = validationReport.GeneratedUtc,
                aegisVersion = validationReport.AegisVersion,
                status = validationReport.HasBlockingFindings(profile.FailureThreshold) ? "Failed" : "Passed",
                errors = validationReport.ErrorCount,
                warnings = validationReport.WarningCount,
                infos = validationReport.InfoCount
            };

            Directory.CreateDirectory(ResourceFolder);
            File.WriteAllText(SnapshotPath, JsonUtility.ToJson(snapshot, true));
            AssetDatabase.ImportAsset(SnapshotPath);
        }

        public void OnPostprocessBuild(BuildReport report)
        {
            DeleteSnapshot();
        }

        private static AegisValidationReport GetValidationReport(AegisSettings settings, AegisValidationProfile profile)
        {
            // When the build gate is enabled it just ran at callbackOrder -5000 and saved
            // the report; reuse it instead of scanning the whole project a second time.
            if (settings.BuildGateEnabled)
                return AegisReportStore.LoadLastReport();

            AegisRunResult result = new AegisRunner().Run(profile);
            return result.Success ? result.Report : null;
        }

        private static void DeleteSnapshot()
        {
            if (File.Exists(SnapshotPath))
                AssetDatabase.DeleteAsset(SnapshotPath);
        }
    }
}
