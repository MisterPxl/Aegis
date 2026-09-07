using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Xml;
using UnityEngine;

namespace Astra.Aegis
{
    [UnityEngine.Scripting.APIUpdating.MovedFrom(false, "MisterPxl.Aegis", "Aegis.Editor", "AegisReportWriters")]
    public static class AegisReportWriters
    {
        public static void WriteJson(AegisValidationReport report, string path)
        {
            if (report == null || string.IsNullOrWhiteSpace(path))
                return;

            EnsureDirectory(path);
            File.WriteAllText(path, JsonUtility.ToJson(report, true), Encoding.UTF8);
        }

        public static void WriteJUnit(AegisValidationReport report, string path,
            AegisSeverity failureThreshold = AegisSeverity.Error)
        {
            if (report == null || string.IsNullOrWhiteSpace(path))
                return;

            EnsureDirectory(path);
            Dictionary<string, List<AegisFinding>> blocking = new Dictionary<string, List<AegisFinding>>(StringComparer.OrdinalIgnoreCase);
            foreach (AegisFinding finding in report.Findings)
            {
                if (finding.Severity < failureThreshold)
                    continue;

                if (!blocking.TryGetValue(finding.RuleId, out List<AegisFinding> findings))
                    blocking[finding.RuleId] = findings = new List<AegisFinding>();
                findings.Add(finding);
            }

            int failures = 0;
            int errors = report.IsCancelled ? 1 : 0;
            int skipped = 0;
            foreach (AegisRuleExecutionRecord record in report.Rules)
            {
                if (record.Status == AegisRuleExecutionStatus.Failed)
                    errors++;
                else if (record.Status == AegisRuleExecutionStatus.Skipped)
                    skipped++;
                else if (blocking.ContainsKey(record.RuleId))
                    failures++;
            }

            XmlWriterSettings settings = new XmlWriterSettings { Indent = true, Encoding = Encoding.UTF8 };
            using (XmlWriter writer = XmlWriter.Create(path, settings))
            {
                writer.WriteStartDocument();
                writer.WriteStartElement("testsuite");
                writer.WriteAttributeString("name", "Aegis");
                writer.WriteAttributeString("tests", (report.Rules.Count + (report.IsCancelled ? 1 : 0)).ToString(CultureInfo.InvariantCulture));
                writer.WriteAttributeString("failures", failures.ToString(CultureInfo.InvariantCulture));
                writer.WriteAttributeString("errors", errors.ToString(CultureInfo.InvariantCulture));
                writer.WriteAttributeString("skipped", skipped.ToString(CultureInfo.InvariantCulture));

                foreach (AegisRuleExecutionRecord record in report.Rules)
                {
                    writer.WriteStartElement("testcase");
                    writer.WriteAttributeString("classname", "Aegis");
                    writer.WriteAttributeString("name", record.RuleName);
                    writer.WriteAttributeString("time", (record.DurationMs / 1000d).ToString("F3", CultureInfo.InvariantCulture));
                    if (record.Status == AegisRuleExecutionStatus.Failed)
                    {
                        WriteOutcome(writer, "error", record.Message, "Aegis.RuleExecutionFailed");
                    }
                    else if (record.Status == AegisRuleExecutionStatus.Skipped)
                    {
                        WriteOutcome(writer, "skipped", record.Message);
                    }
                    else if (blocking.TryGetValue(record.RuleId, out List<AegisFinding> findings))
                    {
                        writer.WriteStartElement("failure");
                        writer.WriteAttributeString("message", $"{findings.Count} blocking finding(s).");
                        foreach (AegisFinding finding in findings)
                            writer.WriteString($"{finding.Code}: {finding.Message} [{finding.AssetPath} {finding.PropertyPath}]\n");
                        writer.WriteEndElement();
                    }
                    writer.WriteEndElement();
                }

                if (report.IsCancelled)
                {
                    writer.WriteStartElement("testcase");
                    writer.WriteAttributeString("classname", "Aegis");
                    writer.WriteAttributeString("name", "Validation completion");
                    WriteOutcome(writer, "error", "Validation cancelled; results are incomplete.", "Aegis.Cancelled");
                    writer.WriteEndElement();
                }
                writer.WriteEndElement();
            }
        }

        public static string FormatSummary(AegisValidationReport report)
        {
            if (report == null)
                return "Aegis did not produce a report.";

            string status = report.IsCancelled ? "cancelled (incomplete), "
                : report.HasRuleFailures ? "rule execution failed, " : string.Empty;
            return $"Aegis {report.ProfileName}: {status}{report.ErrorCount} error(s), {report.WarningCount} warning(s), {report.InfoCount} info in {report.DurationMs:F1} ms.";
        }

        private static void WriteOutcome(XmlWriter writer, string element, string message, string type = null)
        {
            writer.WriteStartElement(element);
            writer.WriteAttributeString("message", message);
            if (type != null)
                writer.WriteAttributeString("type", type);
            writer.WriteEndElement();
        }

        private static void EnsureDirectory(string path)
        {
            string directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory))
                Directory.CreateDirectory(directory);
        }
    }
}
