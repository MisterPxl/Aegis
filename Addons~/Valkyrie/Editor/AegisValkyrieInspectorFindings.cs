using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Astra.Aegis.Integrations.Valkyrie
{
    /// <summary>Findings known for one inspected object, with their provenance and age.</summary>
    public sealed class AegisInspectorFindings
    {
        public AegisInspectorFindings(string globalObjectId, IReadOnlyList<AegisFinding> findings, DateTime capturedUtc, string source)
        {
            GlobalObjectId = globalObjectId ?? string.Empty;
            Findings = findings ?? Array.Empty<AegisFinding>();
            CapturedUtc = capturedUtc;
            Source = source ?? string.Empty;
        }

        public string GlobalObjectId { get; }
        public IReadOnlyList<AegisFinding> Findings { get; }
        public DateTime CapturedUtc { get; }
        /// <summary>"report" when read from the last saved Aegis report, "targeted" after an inspector validation.</summary>
        public string Source { get; }
        public int ErrorCount => Count(AegisSeverity.Error);
        public int WarningCount => Count(AegisSeverity.Warning);
        public int InfoCount => Count(AegisSeverity.Info);

        private int Count(AegisSeverity severity)
        {
            int count = 0;
            for (int i = 0; i < Findings.Count; i++) if (Findings[i].Severity == severity) count++;
            return count;
        }

        /// <summary>Findings whose property path is the field itself or nested under it.</summary>
        public void CollectForProperty(string propertyPath, List<AegisFinding> destination)
        {
            if (string.IsNullOrEmpty(propertyPath)) return;
            for (int i = 0; i < Findings.Count; i++)
                if (MatchesProperty(Findings[i].PropertyPath, propertyPath)) destination.Add(Findings[i]);
        }

        /// <summary>Findings without a property path, or whose path targets no drawn field.</summary>
        public void CollectUnattached(ICollection<string> drawnPropertyPaths, List<AegisFinding> destination)
        {
            for (int i = 0; i < Findings.Count; i++)
            {
                string path = Findings[i].PropertyPath;
                bool attached = false;
                if (!string.IsNullOrEmpty(path))
                    foreach (string drawn in drawnPropertyPaths) if (MatchesProperty(path, drawn)) { attached = true; break; }
                if (!attached) destination.Add(Findings[i]);
            }
        }

        public static bool MatchesProperty(string findingPath, string fieldPath)
        {
            if (string.IsNullOrEmpty(findingPath) || string.IsNullOrEmpty(fieldPath)) return false;
            if (!findingPath.StartsWith(fieldPath, StringComparison.Ordinal)) return false;
            if (findingPath.Length == fieldPath.Length) return true;
            char next = findingPath[fieldPath.Length];
            return next == '.' || next == '[';
        }

        public static string FormatAge(DateTime capturedUtc, DateTime nowUtc)
        {
            TimeSpan age = nowUtc - capturedUtc;
            if (age.TotalSeconds < 60) return "just now";
            if (age.TotalMinutes < 60) return ((int)age.TotalMinutes).ToString(CultureInfo.InvariantCulture) + " min ago";
            if (age.TotalHours < 48) return ((int)age.TotalHours).ToString(CultureInfo.InvariantCulture) + " h ago";
            return ((int)age.TotalDays).ToString(CultureInfo.InvariantCulture) + " d ago";
        }
    }

    /// <summary>
    /// Caches inspector findings per object. Entries come from the last saved Aegis report or from a
    /// targeted validation of the inspected object's asset or scene. The cache is invalidated when an
    /// object is modified through Undo, when assets are imported, when the last report changes and on
    /// play mode transitions, so the inspector never scans the project on repaint.
    /// </summary>
    public static class AegisValkyrieInspectorCache
    {
        private static readonly Dictionary<string, AegisInspectorFindings> Entries = new Dictionary<string, AegisInspectorFindings>(StringComparer.Ordinal);
        private static AegisValidationReport _lastReport;
        private static DateTime _lastReportGeneratedUtc;
        private static DateTime _lastReportFileWriteUtc = DateTime.MinValue;
        private static bool _hooked;

        public static int Count => Entries.Count;

        public static void EnsureHooks()
        {
            if (_hooked) return;
            _hooked = true;
            Undo.postprocessModifications += OnModifications;
            EditorApplication.playModeStateChanged += _ => InvalidateAll();
        }

        public static bool TryGet(UnityEngine.Object target, out AegisInspectorFindings findings)
        {
            findings = null;
            string id = AegisObjectId.TryGet(target);
            if (string.IsNullOrEmpty(id)) return false;
            if (Entries.TryGetValue(id, out findings) && findings.Source == "targeted") return true;
            AegisValidationReport report = LoadLastReport();
            if (report == null) { Entries.Remove(id); findings = null; return false; }
            if (findings != null && findings.Source == "report" && findings.CapturedUtc == _lastReportGeneratedUtc) return true;
            findings = new AegisInspectorFindings(id, Filter(report.Findings, new[] { id }), _lastReportGeneratedUtc, "report");
            Entries[id] = findings;
            return true;
        }

        public static void Store(AegisInspectorFindings findings)
        {
            if (findings != null && !string.IsNullOrEmpty(findings.GlobalObjectId)) Entries[findings.GlobalObjectId] = findings;
        }

        public static void Invalidate(UnityEngine.Object target)
        {
            string id = AegisObjectId.TryGet(target);
            if (!string.IsNullOrEmpty(id)) Entries.Remove(id);
        }

        public static void InvalidateAll()
        {
            Entries.Clear();
        }

        /// <summary>
        /// Runs every discovered rule on the asset or scene that owns the targets, applies the project's
        /// suppressions and caches the findings that belong to each target. Does not touch the saved
        /// project report. Returns <c>false</c> with a reason when a target has no saved location.
        /// </summary>
        public static bool RunTargeted(UnityEngine.Object[] targets, out string message)
        {
            message = null;
            if (targets == null || targets.Length == 0) { message = "Nothing selected."; return false; }
            var paths = new List<string>();
            var ids = new List<string>();
            for (int i = 0; i < targets.Length; i++)
            {
                if (targets[i] == null) continue;
                string path = GetOwningAssetPath(targets[i]);
                if (string.IsNullOrEmpty(path)) { message = "Save the scene or prefab before validating it."; return false; }
                if (!paths.Contains(path)) paths.Add(path);
                string id = AegisObjectId.TryGet(targets[i]);
                if (!string.IsNullOrEmpty(id)) ids.Add(id);
            }
            var profile = new AegisValidationProfile { Name = "Inspector" };
            profile.IncludedFolders.AddRange(paths);
            var findings = new List<AegisFinding>();
            var records = new List<AegisRuleExecutionRecord>();
            var context = new AegisValidationContext(profile);
            Stopwatch stopwatch = Stopwatch.StartNew();
            List<AegisRuleAsset> rules = AegisRuleDiscovery.DiscoverRules();
            for (int i = 0; i < rules.Count; i++)
                AegisRunner.ExecuteRule(rules[i], context, findings, records);
            stopwatch.Stop();
            AegisValidationReport report = AegisReportFinalizer.ApplySuppressions(new AegisValidationReport(profile.Name, stopwatch.Elapsed.TotalMilliseconds, findings, records));
            DateTime now = DateTime.UtcNow;
            for (int i = 0; i < ids.Count; i++)
                Store(new AegisInspectorFindings(ids[i], Filter(report.Findings, new[] { ids[i] }), now, "targeted"));
            message = rules.Count + " rule(s) evaluated on " + string.Join(", ", paths) + " in " + report.DurationMs.ToString("0", CultureInfo.InvariantCulture) + " ms.";
            return true;
        }

        public static string GetOwningAssetPath(UnityEngine.Object target)
        {
            if (target == null) return null;
            string assetPath = AssetDatabase.GetAssetPath(target);
            if (!string.IsNullOrEmpty(assetPath)) return assetPath;
            GameObject gameObject = target as GameObject ?? (target as Component)?.gameObject;
            if (gameObject == null) return null;
            var stage = PrefabStageUtility.GetPrefabStage(gameObject);
            if (stage != null) return stage.assetPath;
            return gameObject.scene.IsValid() && !string.IsNullOrEmpty(gameObject.scene.path) ? gameObject.scene.path : null;
        }

        public static List<AegisFinding> Filter(IReadOnlyList<AegisFinding> findings, IReadOnlyList<string> globalObjectIds)
        {
            var result = new List<AegisFinding>();
            if (findings == null) return result;
            for (int i = 0; i < findings.Count; i++)
                for (int j = 0; j < globalObjectIds.Count; j++)
                    if (string.Equals(findings[i].GlobalObjectId, globalObjectIds[j], StringComparison.Ordinal)) { result.Add(findings[i]); break; }
            return result;
        }

        private static AegisValidationReport LoadLastReport()
        {
            string path = AegisPackageInfo.LastReportPath;
            if (!File.Exists(path)) { _lastReport = null; return null; }
            DateTime writeUtc = File.GetLastWriteTimeUtc(path);
            if (_lastReport == null || writeUtc != _lastReportFileWriteUtc)
            {
                _lastReport = AegisReportStore.LoadLastReport();
                _lastReportFileWriteUtc = writeUtc;
                _lastReportGeneratedUtc = _lastReport != null && DateTime.TryParse(_lastReport.GeneratedUtc, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out DateTime generated)
                    ? generated.ToUniversalTime() : writeUtc;
                // A newer project report supersedes cached report-sourced entries.
                var stale = new List<string>();
                foreach (var pair in Entries) if (pair.Value.Source == "report") stale.Add(pair.Key);
                for (int i = 0; i < stale.Count; i++) Entries.Remove(stale[i]);
            }
            return _lastReport;
        }

        private static UndoPropertyModification[] OnModifications(UndoPropertyModification[] modifications)
        {
            for (int i = 0; i < modifications.Length; i++)
            {
                UnityEngine.Object target = modifications[i].currentValue?.target;
                if (target != null) Invalidate(target);
            }
            return modifications;
        }
    }

    internal sealed class AegisValkyrieInspectorAssetWatcher : AssetPostprocessor
    {
        private static void OnPostprocessAllAssets(string[] imported, string[] deleted, string[] moved, string[] movedFrom)
        {
            if (imported.Length > 0 || deleted.Length > 0 || moved.Length > 0)
                AegisValkyrieInspectorCache.InvalidateAll();
        }
    }
}
