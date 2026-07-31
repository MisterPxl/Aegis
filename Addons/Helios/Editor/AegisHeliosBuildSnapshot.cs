using System.IO;
using MisterPxl.Aegis;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace MisterPxl.Aegis.HeliosIntegration.Editor
{
    public sealed class AegisHeliosBuildSnapshot : IPreprocessBuildWithReport, IPostprocessBuildWithReport
    {
        private const string ResourceFolder = "Assets/Resources";
        private const string SnapshotPath = "Assets/Resources/AegisValidationSnapshot.json";
        private static bool _createdSnapshot;

        public int callbackOrder => -4900;

        public void OnPreprocessBuild(BuildReport report)
        {
            AegisValidationProfile profile = AegisSettings.instance.GetProfile("Build");
            AegisRunResult result = new AegisRunner().Run(profile);
            if (!result.Success || result.Report == null)
                return;

            AegisValidationSnapshot snapshot = new AegisValidationSnapshot
            {
                profile = result.Report.ProfileName,
                generatedUtc = result.Report.GeneratedUtc,
                aegisVersion = result.Report.AegisVersion,
                status = result.Report.HasBlockingFindings(profile.FailureThreshold) ? "Failed" : "Passed",
                errors = result.Report.ErrorCount,
                warnings = result.Report.WarningCount,
                infos = result.Report.InfoCount
            };

            Directory.CreateDirectory(ResourceFolder);
            File.WriteAllText(SnapshotPath, JsonUtility.ToJson(snapshot, true));
            AssetDatabase.ImportAsset(SnapshotPath);
            _createdSnapshot = true;
        }

        public void OnPostprocessBuild(BuildReport report)
        {
            if (!_createdSnapshot)
                return;

            AssetDatabase.DeleteAsset(SnapshotPath);
            _createdSnapshot = false;
        }
    }
}
