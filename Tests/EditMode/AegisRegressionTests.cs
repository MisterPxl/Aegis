using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace MisterPxl.Aegis.Tests
{
    public sealed class AegisRegressionTests
    {
        private string _folder;
        private string _settingsJson;
        private byte[] _settingsFile;
        private byte[] _lastReport;
        private const string SettingsPath = "ProjectSettings/AegisSettings.asset";

        [SetUp]
        public void SetUp()
        {
            _settingsFile = ReadIfPresent(SettingsPath);
            _lastReport = ReadIfPresent(AegisPackageInfo.LastReportPath);
            _settingsJson = EditorJsonUtility.ToJson(AegisSettings.instance);
            _folder = "Assets/AegisRegression_" + Guid.NewGuid().ToString("N");
            AssetDatabase.CreateFolder("Assets", Path.GetFileName(_folder));
        }

        [TearDown]
        public void TearDown()
        {
            AssetDatabase.DeleteAsset(_folder);
            EditorJsonUtility.FromJsonOverwrite(_settingsJson, AegisSettings.instance);
            RestoreFile(SettingsPath, _settingsFile);
            RestoreFile(AegisPackageInfo.LastReportPath, _lastReport);
        }

        [TestCase(typeof(MissingMonoScriptRule))]
        [TestCase(typeof(MissingObjectReferenceRule))]
        [TestCase(typeof(NullCollectionEntryRule))]
        [TestCase(typeof(BuildSceneRule))]
        [TestCase(typeof(PrefabIntegrityRule))]
        [TestCase(typeof(DuplicateAegisKeyRule))]
        public void RuleAsset_PreservesTypeAndConfigurationAfterReimport(Type type)
        {
            string path = _folder + "/Rule.asset";
            AegisRuleAsset rule = (AegisRuleAsset)ScriptableObject.CreateInstance(type);
            Assert.AreEqual(type, MonoScript.FromScriptableObject(rule)?.GetClass());
            using (SerializedObject serialized = new SerializedObject(rule))
            {
                serialized.FindProperty("_displayName").stringValue = "Saved rule";
                serialized.FindProperty("_order").intValue = 17;
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }
            AssetDatabase.CreateAsset(rule, path);
            AssetDatabase.SaveAssets();
            string id = rule.RuleId;
            Resources.UnloadAsset(rule);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            rule = AssetDatabase.LoadAssetAtPath<AegisRuleAsset>(path);
            Assert.IsNotNull(rule);
            Assert.AreEqual(type, rule.GetType());
            Assert.AreEqual("Saved rule", rule.DisplayName);
            Assert.AreEqual(17, rule.Order);
            Assert.AreEqual(id, rule.RuleId);
            Assert.Contains(rule, AegisRuleDiscovery.DiscoverRules());
        }

        [Test]
        public void Scanners_TerminateOnCyclesAndIncludeHiddenSerializedFields()
        {
            string path = _folder + "/Fixture.asset";
            AegisRegressionFixture fixture = ScriptableObject.CreateInstance<AegisRegressionFixture>();
            fixture.Node = new AegisRegressionNode();
            fixture.Node.Next = fixture.Node;
            AssetDatabase.CreateAsset(fixture, path);
            AssetDatabase.SaveAssets();
            Resources.UnloadAsset(fixture);
            string missing = "{fileID: 11400000, guid: aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa, type: 2}";
            string yaml = File.ReadAllText(path)
                .Replace("HiddenReference: {fileID: 0}", "HiddenReference: " + missing)
                .Replace("VisibleReference: {fileID: 0}", "VisibleReference: " + missing);
            File.WriteAllText(path, yaml);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            fixture = AssetDatabase.LoadAssetAtPath<AegisRegressionFixture>(path);

            using (SerializedObject serialized = new SerializedObject(fixture))
            {
                int visited = 0;
                foreach (SerializedProperty property in AegisSerializedProperties.Enumerate(serialized))
                    Assert.Less(++visited, 100, "Cycle traversal must terminate before evaluating the scanners.");
            }

            AegisValidationProfile profile = new AegisValidationProfile();
            profile.IncludedFolders.Add(_folder);
            MissingObjectReferenceRule references = ScriptableObject.CreateInstance<MissingObjectReferenceRule>();
            NullCollectionEntryRule collections = ScriptableObject.CreateInstance<NullCollectionEntryRule>();
            try
            {
                List<AegisFinding> findings = new List<AegisFinding>();
                references.Evaluate(new AegisValidationContext(profile), new AegisFindingSink(findings));
                Assert.IsTrue(findings.Any(f => f.PropertyPath == "VisibleReference"), "Visible reference is the fixture control.");
                Assert.IsTrue(findings.Any(f => f.PropertyPath == "HiddenReference"));
                findings.Clear();
                collections.Evaluate(new AegisValidationContext(profile), new AegisFindingSink(findings));
                Assert.AreEqual(1, findings.Count(f => f.PropertyPath == "HiddenCollection.Array.data[0]"));
                Assert.AreEqual(1, findings.Count(f => f.PropertyPath == "Node.Items.Array.data[0]"));
            }
            finally
            {
                Object.DestroyImmediate(references);
                Object.DestroyImmediate(collections);
            }
        }

        [Test]
        public void Settings_SaveInitializesAllProfilesBeforeSerialization()
        {
            AegisSettings settings = AegisSettings.instance;
            SetField(settings, "_interactiveProfile", null);
            SetField(settings, "_buildProfile", null);
            SetField(settings, "_ciProfile", null);
            settings.Save();
            Assert.AreEqual("Interactive", settings.InteractiveProfile.Name);
            Assert.AreEqual("Build", settings.BuildProfile.Name);
            Assert.AreEqual("CI", settings.CiProfile.Name);
            Assert.AreEqual(1, settings.BuildProfile.FrameBudgetMs);
            Assert.AreEqual(1, settings.CiProfile.FrameBudgetMs);
            StringAssert.Contains("_name: Build", File.ReadAllText(SettingsPath));
            StringAssert.Contains("_name: CI", File.ReadAllText(SettingsPath));
        }

        [Test]
        public void Settings_MigratesLegacyProfilesWithoutLosingConfiguration()
        {
            AegisSettings settings = AegisSettings.instance;
            AegisValidationProfile legacy = new AegisValidationProfile { FailureThreshold = AegisSeverity.Warning };
            legacy.IncludedFolders.Add("Assets/Game");
            legacy.ExcludedFolders.Add("Assets/Game/Generated");
            legacy.IncludedCategories.Add("Gameplay");
            legacy.DisabledRuleIds.Add("disabled-rule");
            SetField(settings, "_buildProfile", legacy);
            SetField(settings, "_ciProfile", new AegisValidationProfile { FrameBudgetMs = 23 });
            SetField(settings, "_profileSchemaVersion", 0);
            settings.Save();
            AegisValidationProfile migrated = settings.GetProfile("Build");
            Assert.AreEqual("Build", migrated.Name);
            Assert.AreEqual(1, migrated.FrameBudgetMs);
            Assert.AreEqual(AegisSeverity.Warning, migrated.FailureThreshold);
            CollectionAssert.AreEqual(new[] { "Assets/Game" }, migrated.IncludedFolders);
            CollectionAssert.AreEqual(new[] { "Assets/Game/Generated" }, migrated.ExcludedFolders);
            CollectionAssert.AreEqual(new[] { "Gameplay" }, migrated.IncludedCategories);
            CollectionAssert.AreEqual(new[] { "disabled-rule" }, migrated.DisabledRuleIds);
            Assert.AreEqual("CI", settings.CiProfile.Name);
            Assert.AreEqual(23, settings.CiProfile.FrameBudgetMs);
        }

        [UnityTest]
        public IEnumerator InteractiveAndSynchronousRuns_ApplyTheSameSuppressions()
        {
            AegisRegressionRule rule = CreateRule();
            AegisValidationProfile profile = OnlyRule(rule);
            AegisValidationReport before = new AegisRunner().Run(profile).Report;
            Assert.AreEqual(1, before.Findings.Count);
            AegisSettings.instance.AddSuppression(before.Findings[0].Fingerprint, "Regression test", "Tests");
            AegisRunResult completed = null;
            AegisInteractiveRun run = new AegisInteractiveRun(profile, result => completed = result);
            run.Start();
            while (run.IsRunning) yield return null;
            Assert.IsTrue(completed.Success);
            Assert.AreEqual(0, completed.Report.Findings.Count);
            Assert.AreEqual(0, completed.Report.Rules.Single(r => r.RuleId == rule.RuleId).FindingCount);
            Assert.AreEqual(AegisRuleExecutionStatus.Passed, completed.Report.Rules.Single(r => r.RuleId == rule.RuleId).Status);
            Assert.AreEqual(0, AegisReportStore.LoadLastReport().Findings.Count);
            Assert.AreEqual(0, new AegisRunner().Run(profile).Report.Findings.Count);
        }

        [Test]
        public void Dashboard_RefreshRemovesSuppressedFindingAndStaleSelection()
        {
            AegisRegressionRule rule = CreateRule();
            AegisValidationReport report = new AegisRunner().Run(OnlyRule(rule)).Report;
            AegisDashboardWindow window = ScriptableObject.CreateInstance<AegisDashboardWindow>();
            try
            {
                SetField(window, "_report", report);
                SetField(window, "_selectedFinding", report.Findings[0]);
                AegisSettings.instance.AddSuppression(report.Findings[0].Fingerprint, "Regression test", "Tests");
                typeof(AegisDashboardWindow).GetMethod("RefreshFilter", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(window, null);
                Assert.AreEqual(0, ((AegisValidationReport)GetField(window, "_report")).Findings.Count);
                Assert.IsNull(GetField(window, "_selectedFinding"));
            }
            finally { Object.DestroyImmediate(window); }
        }

        [UnityTest]
        public IEnumerator CancellationBeforeFirstTick_DoesNotExecuteRulesOrOverwriteCompletedReport()
        {
            AegisRegressionRule rule = CreateRule();
            AegisValidationReport prior = new AegisRunner().Run(OnlyRule(rule)).Report;
            string previousJson = File.ReadAllText(AegisPackageInfo.LastReportPath);
            AegisRunResult completed = null;
            AegisInteractiveRun run = new AegisInteractiveRun(OnlyRule(rule), result => completed = result);
            run.Start();
            run.Cancel();
            while (run.IsRunning) yield return null;
            Assert.IsFalse(completed.Success);
            Assert.IsTrue(completed.IsCancelled);
            Assert.AreEqual(0, run.CompletedRuleCount);
            Assert.IsTrue(completed.Report.Rules.All(r => r.Status == AegisRuleExecutionStatus.Skipped));
            Assert.AreEqual(previousJson, File.ReadAllText(AegisPackageInfo.LastReportPath));
            AegisValidationReport reloaded = JsonUtility.FromJson<AegisValidationReport>(JsonUtility.ToJson(completed.Report));
            Assert.IsTrue(reloaded.IsCancelled);
            StringAssert.Contains("incomplete", AegisReportWriters.FormatSummary(reloaded));
            AegisReportStore.SaveLastReport(reloaded);
            Assert.AreEqual(previousJson, File.ReadAllText(AegisPackageInfo.LastReportPath));
        }

        [UnityTest]
        public IEnumerator CancellationDuringRule_KeepsPartialFindingsMarkedIncomplete()
        {
            AegisRegressionRule rule = CreateRule();
            AegisRunResult completed = null;
            AegisInteractiveRun run = new AegisInteractiveRun(OnlyRule(rule), result => completed = result);
            rule.OnEvaluate = run.Cancel;
            run.Start();
            while (run.IsRunning) yield return null;
            Assert.IsFalse(completed.Success);
            Assert.IsTrue(completed.Report.IsCancelled);
            Assert.AreEqual(1, completed.Report.Findings.Count);
        }

        [Test]
        public void FailedRule_RemainsAnInternalFailureEvenIfItsFindingIsSuppressed()
        {
            AegisRegressionRule rule = CreateRule();
            rule.ThrowOnEvaluate = true;
            AegisValidationProfile profile = OnlyRule(rule);
            AegisRunResult result = new AegisRunner().Run(profile);
            Assert.IsFalse(result.Success);
            Assert.IsTrue(result.Report.HasRuleFailures);
            AegisSettings.instance.AddSuppression(result.Report.Findings[0].Fingerprint, "Regression test", "Tests");
            result = new AegisRunner().Run(profile);
            Assert.AreEqual(0, result.Report.Findings.Count);
            Assert.IsFalse(result.Success);
            Assert.IsTrue(result.Report.HasRuleFailures);
        }

        [TestCase(AegisSeverity.Error, 0)]
        [TestCase(AegisSeverity.Warning, 1)]
        [TestCase(AegisSeverity.Info, 2)]
        public void JUnit_UsesActiveFindingsThresholdAndExecutionStates(AegisSeverity threshold, int expectedFailures)
        {
            List<AegisFinding> findings = new List<AegisFinding>
            {
                new AegisFinding("warning", "Warning", AegisSeverity.Warning, "A < B & C"),
                new AegisFinding("info", "Info", AegisSeverity.Info, "Information")
            };
            List<AegisRuleExecutionRecord> records = new List<AegisRuleExecutionRecord>
            {
                Record("suppressed", AegisRuleExecutionStatus.Findings, 1),
                Record("warning", AegisRuleExecutionStatus.Findings, 1),
                Record("info", AegisRuleExecutionStatus.Findings, 1),
                Record("disabled", AegisRuleExecutionStatus.Skipped, 0),
                Record("exception", AegisRuleExecutionStatus.Failed, 1)
            };
            AegisValidationReport report = new AegisValidationReport("CI", 0, findings, records);
            XmlDocument xml = WriteJUnit(report, threshold);
            Assert.AreEqual(expectedFailures, xml.SelectNodes("//failure").Count);
            Assert.AreEqual(expectedFailures.ToString(), xml.DocumentElement.GetAttribute("failures"));
            Assert.AreEqual(1, xml.SelectNodes("//error").Count);
            Assert.AreEqual("1", xml.DocumentElement.GetAttribute("errors"));
            Assert.AreEqual(1, xml.SelectNodes("//skipped").Count);
            Assert.AreEqual("1", xml.DocumentElement.GetAttribute("skipped"));
            Assert.AreEqual("5", xml.DocumentElement.GetAttribute("tests"));
            Assert.AreEqual(0, xml.SelectNodes("//testcase[@name='suppressed']/*").Count);
        }

        [Test]
        public void JUnit_CancelledReportCannotAppearSuccessful()
        {
            AegisValidationReport report = new AegisValidationReport("Interactive", 0,
                new List<AegisFinding>(), new List<AegisRuleExecutionRecord>(), isCancelled: true);
            XmlDocument xml = WriteJUnit(report, AegisSeverity.Error);
            Assert.AreEqual("1", xml.DocumentElement.GetAttribute("tests"));
            Assert.AreEqual("1", xml.DocumentElement.GetAttribute("errors"));
            Assert.IsNotNull(xml.SelectSingleNode("//error[@type='Aegis.Cancelled']"));
        }

        [Test]
        public void Finalizer_PreservesMetadataAndRecalculatesEffectiveRuleCounts()
        {
            AegisFinding suppressed = new AegisFinding("rule", "Rule", AegisSeverity.Error, "Suppressed");
            AegisValidationReport before = new AegisValidationReport("CI", 123,
                new List<AegisFinding> { suppressed }, new List<AegisRuleExecutionRecord> { Record("rule", AegisRuleExecutionStatus.Findings, 1) });
            AegisValidationReport after = AegisReportFinalizer.ApplySuppressions(before, f => f.Fingerprint == suppressed.Fingerprint);
            Assert.AreEqual(0, after.Findings.Count);
            Assert.AreEqual(0, after.Rules[0].FindingCount);
            Assert.AreEqual(AegisRuleExecutionStatus.Passed, after.Rules[0].Status);
            Assert.AreEqual(before.GeneratedUtc, after.GeneratedUtc);
            Assert.AreEqual(before.DurationMs, after.DurationMs);
            Assert.AreEqual(1, before.Findings.Count, "Finalization must not mutate the previous report.");
        }

        private AegisRegressionRule CreateRule()
        {
            AegisRegressionRule rule = ScriptableObject.CreateInstance<AegisRegressionRule>();
            AssetDatabase.CreateAsset(rule, _folder + "/RegressionRule.asset");
            return rule;
        }

        private AegisValidationProfile OnlyRule(AegisRuleAsset rule)
        {
            AegisValidationProfile profile = new AegisValidationProfile { Name = "Regression" };
            profile.IncludedFolders.Add(_folder);
            foreach (AegisRuleAsset other in AegisRuleDiscovery.DiscoverRules())
                if (other != rule) profile.DisabledRuleIds.Add(other.RuleId);
            return profile;
        }

        private XmlDocument WriteJUnit(AegisValidationReport report, AegisSeverity threshold)
        {
            string path = Path.GetTempFileName();
            try
            {
                AegisReportWriters.WriteJUnit(report, path, threshold);
                XmlDocument xml = new XmlDocument();
                xml.Load(path);
                return xml;
            }
            finally { File.Delete(path); }
        }

        private static AegisRuleExecutionRecord Record(string id, AegisRuleExecutionStatus status, int count)
            => new AegisRuleExecutionRecord(id, id, status, count, 1, "Message <&>");
        private static byte[] ReadIfPresent(string path) => File.Exists(path) ? File.ReadAllBytes(path) : null;
        private static void RestoreFile(string path, byte[] bytes)
        {
            if (bytes == null) File.Delete(path);
            else { Directory.CreateDirectory(Path.GetDirectoryName(path)); File.WriteAllBytes(path, bytes); }
        }
        private static void SetField(object target, string name, object value)
            => target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
        private static object GetField(object target, string name)
            => target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(target);
    }
}
