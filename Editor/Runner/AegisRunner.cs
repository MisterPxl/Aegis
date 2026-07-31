using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace MisterPxl.Aegis
{
    public static class AegisRuleDiscovery
    {
        public static List<AegisRuleAsset> DiscoverRules()
        {
            List<AegisRuleAsset> rules = new List<AegisRuleAsset>();
            string[] guids = AssetDatabase.FindAssets("t:AegisRuleAsset", new[] { "Assets", "Packages" });
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                AegisRuleAsset rule = AssetDatabase.LoadAssetAtPath<AegisRuleAsset>(path);
                if (rule != null && !rules.Contains(rule))
                    rules.Add(rule);
            }

            AddBuiltInFallbackRules(rules);

            rules.Sort(CompareRules);
            return rules;
        }

        private static void AddBuiltInFallbackRules(List<AegisRuleAsset> rules)
        {
            EnsureRule<MissingMonoScriptRule>(rules);
            EnsureRule<MissingObjectReferenceRule>(rules);
            EnsureRule<NullCollectionEntryRule>(rules);
            EnsureRule<BuildSceneRule>(rules);
            EnsureRule<PrefabIntegrityRule>(rules);
            EnsureRule<DuplicateAegisKeyRule>(rules);
        }

        private static void EnsureRule<TRule>(List<AegisRuleAsset> rules)
            where TRule : AegisRuleAsset
        {
            for (int i = 0; i < rules.Count; i++)
            {
                if (rules[i] is TRule)
                    return;
            }

            TRule rule = ScriptableObject.CreateInstance<TRule>();
            rule.hideFlags = HideFlags.HideAndDontSave;
            rules.Add(rule);
        }

        private static int CompareRules(AegisRuleAsset left, AegisRuleAsset right)
        {
            int category = string.Compare(left.Category, right.Category, StringComparison.OrdinalIgnoreCase);
            if (category != 0)
                return category;

            int order = left.Order.CompareTo(right.Order);
            if (order != 0)
                return order;

            int name = string.Compare(left.DisplayName, right.DisplayName, StringComparison.OrdinalIgnoreCase);
            if (name != 0)
                return name;

            return string.Compare(left.RuleId, right.RuleId, StringComparison.OrdinalIgnoreCase);
        }
    }

    public sealed class AegisRunner
    {
        public AegisRunResult Run(AegisValidationProfile profile)
        {
            Stopwatch stopwatch = Stopwatch.StartNew();
            List<AegisFinding> findings = new List<AegisFinding>();
            List<AegisRuleExecutionRecord> records = new List<AegisRuleExecutionRecord>();
            AegisValidationContext context = new AegisValidationContext(profile);
            List<AegisRuleAsset> rules = AegisRuleDiscovery.DiscoverRules();

            for (int i = 0; i < rules.Count; i++)
                ExecuteRule(rules[i], context, findings, records);

            stopwatch.Stop();
            AegisValidationReport report = CreateReport(profile, stopwatch.Elapsed.TotalMilliseconds, findings, records);
            AegisReportStore.SaveLastReport(report);
            return AegisRunResult.Succeed(report);
        }

        public static void ExecuteRule(
            AegisRuleAsset rule,
            AegisValidationContext context,
            List<AegisFinding> findings,
            List<AegisRuleExecutionRecord> records)
        {
            if (rule == null)
                return;

            if (!context.Profile.IsRuleEnabled(rule))
            {
                records.Add(new AegisRuleExecutionRecord(
                    rule.RuleId,
                    rule.DisplayName,
                    AegisRuleExecutionStatus.Skipped,
                    0,
                    0d,
                    "Rule disabled by profile."));
                return;
            }

            int beforeCount = findings.Count;
            Stopwatch ruleWatch = Stopwatch.StartNew();
            try
            {
                rule.Evaluate(context, new AegisFindingSink(findings));
                ruleWatch.Stop();
                int findingCount = findings.Count - beforeCount;
                records.Add(new AegisRuleExecutionRecord(
                    rule.RuleId,
                    rule.DisplayName,
                    findingCount == 0 ? AegisRuleExecutionStatus.Passed : AegisRuleExecutionStatus.Findings,
                    findingCount,
                    ruleWatch.Elapsed.TotalMilliseconds,
                    string.Empty));
            }
            catch (Exception ex)
            {
                ruleWatch.Stop();
                findings.Add(new AegisFinding(
                    rule.RuleId,
                    rule.DisplayName,
                    AegisSeverity.Error,
                    $"Aegis rule '{rule.DisplayName}' failed.",
                    ex.ToString(),
                    code: "Aegis.RuleExecutionFailed"));
                records.Add(new AegisRuleExecutionRecord(
                    rule.RuleId,
                    rule.DisplayName,
                    AegisRuleExecutionStatus.Failed,
                    findings.Count - beforeCount,
                    ruleWatch.Elapsed.TotalMilliseconds,
                    ex.Message));
            }
        }

        private static AegisValidationReport CreateReport(
            AegisValidationProfile profile,
            double durationMs,
            List<AegisFinding> rawFindings,
            List<AegisRuleExecutionRecord> records)
        {
            List<AegisFinding> findings = new List<AegisFinding>(rawFindings.Count);
            AegisSettings settings = AegisSettings.instance;
            for (int i = 0; i < rawFindings.Count; i++)
            {
                AegisFinding finding = rawFindings[i];
                if (!settings.IsSuppressed(finding))
                    findings.Add(finding);
            }

            return new AegisValidationReport(profile.Name, durationMs, findings, records);
        }
    }

    public sealed class AegisInteractiveRun
    {
        private readonly List<AegisRuleAsset> _rules;
        private readonly List<AegisFinding> _findings = new List<AegisFinding>();
        private readonly List<AegisRuleExecutionRecord> _records = new List<AegisRuleExecutionRecord>();
        private readonly AegisValidationContext _context;
        private readonly Stopwatch _stopwatch = new Stopwatch();
        private readonly Action<AegisRunResult> _complete;
        private int _index;
        private bool _cancelRequested;

        public AegisInteractiveRun(AegisValidationProfile profile, Action<AegisRunResult> complete)
        {
            _rules = AegisRuleDiscovery.DiscoverRules();
            _context = new AegisValidationContext(profile, () => _cancelRequested);
            _complete = complete;
        }

        public AegisValidationProfile Profile => _context.Profile;
        public bool IsRunning { get; private set; }
        public int CompletedRuleCount => _index;
        public int TotalRuleCount => _rules.Count;

        public void Start()
        {
            if (IsRunning)
                return;

            _stopwatch.Restart();
            IsRunning = true;
            EditorApplication.update += Tick;
        }

        public void Cancel()
        {
            _cancelRequested = true;
        }

        private void Tick()
        {
            Stopwatch budget = Stopwatch.StartNew();
            while (_index < _rules.Count && budget.ElapsedMilliseconds < Profile.FrameBudgetMs)
            {
                AegisRunner.ExecuteRule(_rules[_index], _context, _findings, _records);
                _index++;
                if (_cancelRequested)
                    break;
            }

            if (_index >= _rules.Count || _cancelRequested)
                Finish();
        }

        private void Finish()
        {
            EditorApplication.update -= Tick;
            IsRunning = false;
            _stopwatch.Stop();
            AegisValidationReport report = new AegisValidationReport(Profile.Name, _stopwatch.Elapsed.TotalMilliseconds, _findings, _records);
            AegisReportStore.SaveLastReport(report);
            _complete?.Invoke(AegisRunResult.Succeed(report, _cancelRequested ? "Aegis validation cancelled." : null));
        }
    }

    public static class AegisReportStore
    {
        public static void SaveLastReport(AegisValidationReport report)
        {
            if (report == null)
                return;

            Directory.CreateDirectory(AegisPackageInfo.ReportFolder);
            AegisReportWriters.WriteJson(report, AegisPackageInfo.LastReportPath);
        }

        public static AegisValidationReport LoadLastReport()
        {
            if (!File.Exists(AegisPackageInfo.LastReportPath))
                return null;

            string json = File.ReadAllText(AegisPackageInfo.LastReportPath);
            return JsonUtility.FromJson<AegisValidationReport>(json);
        }
    }
}
