using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Astra.Aegis
{
    [UnityEngine.Scripting.APIUpdating.MovedFrom(false, "MisterPxl.Aegis", "Aegis.Editor", "AegisCli")]
    public static class AegisCli
    {
        public static void Run()
        {
            AegisExitCode exitCode = AegisExitCode.InternalError;
            try
            {
                string[] args = Environment.GetCommandLineArgs();
                string profileName = GetArg(args, "-aegisProfile", "CI");
                string jsonPath = GetArg(args, "-aegisJson", "Library/Aegis/aegis-report.json");
                string junitPath = GetArg(args, "-aegisJUnit", "Library/Aegis/aegis-report.xml");

                if (!AegisSettings.instance.TryGetProfile(profileName, out AegisValidationProfile profile))
                {
                    Debug.LogError($"Aegis: unknown profile '{profileName}'. Expected 'Interactive', 'Build' or 'CI'.");
                    exitCode = AegisExitCode.ConfigurationError;
                    return;
                }

                AegisRunResult result = new AegisRunner().Run(profile);
                if (result.Report == null)
                {
                    Debug.LogError(result.Message);
                    exitCode = AegisExitCode.InternalError;
                }
                else
                {
                    AegisReportWriters.WriteJson(result.Report, jsonPath);
                    AegisReportWriters.WriteJUnit(result.Report, junitPath, profile.FailureThreshold);
                    string summary = AegisReportWriters.FormatSummary(result.Report);
                    Debug.Log(summary);
                    Debug.Log($"Aegis JSON: {Path.GetFullPath(jsonPath)}");
                    Debug.Log($"Aegis JUnit: {Path.GetFullPath(junitPath)}");
                    exitCode = !result.Success
                        ? AegisExitCode.InternalError
                        : result.Report.HasBlockingFindings(profile.FailureThreshold)
                        ? AegisExitCode.BlockingFindings
                        : AegisExitCode.Success;
                }
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                exitCode = AegisExitCode.InternalError;
            }
            finally
            {
                if (Application.isBatchMode)
                    EditorApplication.Exit((int)exitCode);
            }
        }

        private static string GetArg(string[] args, string name, string defaultValue)
        {
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (string.Equals(args[i], name, StringComparison.OrdinalIgnoreCase))
                    return args[i + 1];
            }

            return defaultValue;
        }
    }
}
