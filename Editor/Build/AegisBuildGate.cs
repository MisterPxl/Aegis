using System.IO;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace MisterPxl.Aegis
{
    public sealed class AegisBuildGate : IPreprocessBuildWithReport
    {
        public int callbackOrder => -5000;

        public void OnPreprocessBuild(BuildReport buildReport)
        {
            AegisSettings settings = AegisSettings.instance;
            if (!settings.BuildGateEnabled)
                return;

            AegisValidationProfile profile = settings.GetProfile("Build");
            AegisRunResult result = new AegisRunner().Run(profile);
            if (!result.Success || result.Report == null)
                throw new BuildFailedException(result.Message);

            string reportPath = Path.GetFullPath(AegisPackageInfo.LastReportPath);
            if (result.Report.HasBlockingFindings(profile.FailureThreshold))
            {
                string summary = AegisReportWriters.FormatSummary(result.Report);
                throw new BuildFailedException($"{summary}\nFull report: {reportPath}");
            }
        }
    }
}
