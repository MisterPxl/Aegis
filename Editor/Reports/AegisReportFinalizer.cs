using System;
using System.Collections.Generic;

namespace Astra.Aegis
{
    [UnityEngine.Scripting.APIUpdating.MovedFrom(false, "MisterPxl.Aegis", "Aegis.Editor", "AegisReportFinalizer")]
    public static class AegisReportFinalizer
    {
        public static AegisValidationReport ApplySuppressions(
            AegisValidationReport report, Predicate<AegisFinding> isSuppressed = null)
        {
            if (report == null)
                return null;

            if (isSuppressed == null)
                isSuppressed = AegisSettings.instance.IsSuppressed;

            List<AegisFinding> findings = new List<AegisFinding>();
            Dictionary<string, int> counts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            foreach (AegisFinding finding in report.Findings)
            {
                if (isSuppressed(finding))
                    continue;

                findings.Add(finding);
                counts.TryGetValue(finding.RuleId, out int count);
                counts[finding.RuleId] = count + 1;
            }

            List<AegisRuleExecutionRecord> records = new List<AegisRuleExecutionRecord>();
            foreach (AegisRuleExecutionRecord record in report.Rules)
            {
                counts.TryGetValue(record.RuleId, out int count);
                AegisRuleExecutionStatus status = record.Status;
                if (status == AegisRuleExecutionStatus.Passed || status == AegisRuleExecutionStatus.Findings)
                    status = count == 0 ? AegisRuleExecutionStatus.Passed : AegisRuleExecutionStatus.Findings;

                records.Add(new AegisRuleExecutionRecord(record.RuleId, record.RuleName, status,
                    count, record.DurationMs, record.Message));
            }

            return report.WithResults(findings, records);
        }
    }
}
