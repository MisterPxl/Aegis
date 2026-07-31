using System;
using System.IO;
using System.Text;
using UnityEngine;

namespace MisterPxl.Aegis
{
    public static class AegisReportWriters
    {
        public static void WriteJson(AegisValidationReport report, string path)
        {
            if (report == null || string.IsNullOrWhiteSpace(path))
                return;

            string directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory))
                Directory.CreateDirectory(directory);

            string json = JsonUtility.ToJson(report, true);
            File.WriteAllText(path, json, Encoding.UTF8);
        }

        public static void WriteJUnit(AegisValidationReport report, string path)
        {
            if (report == null || string.IsNullOrWhiteSpace(path))
                return;

            string directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory))
                Directory.CreateDirectory(directory);

            StringBuilder builder = new StringBuilder(4096);
            int tests = report.Rules.Count;
            int failures = report.ErrorCount + report.WarningCount;
            builder.Append("<?xml version=\"1.0\" encoding=\"utf-8\"?>").AppendLine();
            builder.Append("<testsuite name=\"Aegis\" tests=\"").Append(tests).Append("\" failures=\"").Append(failures).Append("\">").AppendLine();

            for (int i = 0; i < report.Rules.Count; i++)
            {
                AegisRuleExecutionRecord record = report.Rules[i];
                builder.Append("  <testcase classname=\"Aegis\" name=\"")
                    .Append(Escape(record.RuleName))
                    .Append("\" time=\"")
                    .Append((record.DurationMs / 1000d).ToString("F3", System.Globalization.CultureInfo.InvariantCulture))
                    .Append("\">")
                    .AppendLine();

                if (record.Status == AegisRuleExecutionStatus.Failed || record.FindingCount > 0)
                {
                    builder.Append("    <failure message=\"")
                        .Append(Escape(record.Message.Length > 0 ? record.Message : $"{record.FindingCount} finding(s)."))
                        .Append("\" />")
                        .AppendLine();
                }

                builder.Append("  </testcase>").AppendLine();
            }

            builder.Append("</testsuite>").AppendLine();
            File.WriteAllText(path, builder.ToString(), Encoding.UTF8);
        }

        public static string FormatSummary(AegisValidationReport report)
        {
            if (report == null)
                return "Aegis did not produce a report.";

            return $"Aegis {report.ProfileName}: {report.ErrorCount} error(s), {report.WarningCount} warning(s), {report.InfoCount} info in {report.DurationMs:F1} ms.";
        }

        private static string Escape(string value)
        {
            return (value ?? string.Empty)
                .Replace("&", "&amp;")
                .Replace("\"", "&quot;")
                .Replace("<", "&lt;")
                .Replace(">", "&gt;");
        }
    }
}
