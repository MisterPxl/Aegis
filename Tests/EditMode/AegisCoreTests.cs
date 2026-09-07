using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MisterPxl.Aegis.Tests
{
    public sealed class AegisCoreTests
    {
        [Test]
        public void Fingerprint_IsDeterministic()
        {
            string first = AegisFingerprint.Compute("rule", "path", "property");
            string second = AegisFingerprint.Compute("rule", "path", "property");

            Assert.AreEqual(first, second);
        }

        [Test]
        public void Profile_ExcludesConfiguredFolders()
        {
            AegisValidationProfile profile = new AegisValidationProfile();
            profile.ExcludedFolders.Add("Assets/Generated");

            Assert.IsFalse(profile.IsPathIncluded("Assets/Generated/File.asset"));
            Assert.IsTrue(profile.IsPathIncluded("Assets/Manual/File.asset"));
        }

        [Test]
        public void Report_CountsFindingsBySeverity()
        {
            System.Collections.Generic.List<AegisFinding> findings = new System.Collections.Generic.List<AegisFinding>
            {
                new AegisFinding("rule", "Rule", AegisSeverity.Error, "Error"),
                new AegisFinding("rule", "Rule", AegisSeverity.Warning, "Warning"),
                new AegisFinding("rule", "Rule", AegisSeverity.Info, "Info")
            };
            AegisValidationReport report = new AegisValidationReport("Test", 1d, findings, new System.Collections.Generic.List<AegisRuleExecutionRecord>());

            Assert.AreEqual(1, report.ErrorCount);
            Assert.AreEqual(1, report.WarningCount);
            Assert.AreEqual(1, report.InfoCount);
        }

        [Test]
        public void Runner_IsolatesRuleExceptions()
        {
            ThrowingRule rule = ScriptableObject.CreateInstance<ThrowingRule>();
            System.Collections.Generic.List<AegisFinding> findings = new System.Collections.Generic.List<AegisFinding>();
            System.Collections.Generic.List<AegisRuleExecutionRecord> records = new System.Collections.Generic.List<AegisRuleExecutionRecord>();

            AegisRunner.ExecuteRule(rule, new AegisValidationContext(new AegisValidationProfile()), findings, records);

            Assert.AreEqual(1, findings.Count);
            Assert.AreEqual(AegisRuleExecutionStatus.Failed, records[0].Status);
            Object.DestroyImmediate(rule);
        }

        [Test]
        public void MissingObjectReferenceRule_ScansSceneWithoutLoadingSceneObjectsAsAssets()
        {
            const string folderPath = "Assets/AegisGeneratedTests";
            bool createdFolder = false;
            string scenePath = folderPath + "/SceneScan_" + System.Guid.NewGuid().ToString("N") + ".unity";
            Scene scene = default;
            MissingObjectReferenceRule rule = null;

            try
            {
                if (!AssetDatabase.IsValidFolder(folderPath))
                {
                    AssetDatabase.CreateFolder("Assets", "AegisGeneratedTests");
                    createdFolder = true;
                }

                // Additive creation is forbidden while an untitled unsaved scene is open (batchmode default),
                // so build the fixture as the single scene, save it, then replace it to leave it unloaded.
                scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                new GameObject("Scene Object");
                Assert.IsTrue(EditorSceneManager.SaveScene(scene, scenePath));
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                scene = default;

                AegisValidationProfile profile = new AegisValidationProfile();
                profile.IncludedFolders.Add(folderPath);
                rule = ScriptableObject.CreateInstance<MissingObjectReferenceRule>();
                List<AegisFinding> findings = new List<AegisFinding>();

                Assert.DoesNotThrow(() => rule.Evaluate(new AegisValidationContext(profile), new AegisFindingSink(findings)));
            }
            finally
            {
                if (scene.IsValid())
                    EditorSceneManager.CloseScene(scene, true);

                if (rule != null)
                    Object.DestroyImmediate(rule);

                AssetDatabase.DeleteAsset(scenePath);
                if (createdFolder)
                    AssetDatabase.DeleteAsset(folderPath);
            }
        }

        [Test]
        public void SceneScan_DoesNotCloseSceneAlreadyOpenInEditor()
        {
            const string folderPath = "Assets/AegisGeneratedTests";
            bool createdFolder = false;
            string scenePath = folderPath + "/OpenSceneScan_" + System.Guid.NewGuid().ToString("N") + ".unity";
            Scene scene = default;
            MissingObjectReferenceRule rule = null;

            try
            {
                if (!AssetDatabase.IsValidFolder(folderPath))
                {
                    AssetDatabase.CreateFolder("Assets", "AegisGeneratedTests");
                    createdFolder = true;
                }

                // Keep the fixture scene open as the active scene while the rule scans it.
                scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                new GameObject("Scene Object");
                Assert.IsTrue(EditorSceneManager.SaveScene(scene, scenePath));

                AegisValidationProfile profile = new AegisValidationProfile();
                profile.IncludedFolders.Add(folderPath);
                rule = ScriptableObject.CreateInstance<MissingObjectReferenceRule>();
                List<AegisFinding> findings = new List<AegisFinding>();

                rule.Evaluate(new AegisValidationContext(profile), new AegisFindingSink(findings));

                Scene sceneAfterScan = SceneManager.GetSceneByPath(scenePath);
                Assert.IsTrue(sceneAfterScan.IsValid() && sceneAfterScan.isLoaded, "Aegis must not close a scene the user already had open.");
            }
            finally
            {
                // The fixture scene is the only loaded scene, so release it by replacing it.
                if (scene.IsValid())
                    EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

                if (rule != null)
                    Object.DestroyImmediate(rule);

                AssetDatabase.DeleteAsset(scenePath);
                if (createdFolder)
                    AssetDatabase.DeleteAsset(folderPath);
            }
        }

        [Test]
        public void Fingerprint_IgnoresMessageForAnchoredFindings()
        {
            AegisFinding first = new AegisFinding("rule", "Rule", AegisSeverity.Error, "3 issues", assetPath: "Assets/A.prefab", globalObjectId: "gid", code: "X");
            AegisFinding second = new AegisFinding("rule", "Rule", AegisSeverity.Error, "2 issues", assetPath: "Assets/A.prefab", globalObjectId: "gid", code: "X");

            Assert.AreEqual(first.Fingerprint, second.Fingerprint);
        }

        [Test]
        public void Fingerprint_UsesMessageForUnanchoredFindings()
        {
            AegisFinding first = new AegisFinding("rule", "Rule", AegisSeverity.Error, "Entry 0", code: "X");
            AegisFinding second = new AegisFinding("rule", "Rule", AegisSeverity.Error, "Entry 1", code: "X");

            Assert.AreNotEqual(first.Fingerprint, second.Fingerprint);
        }

        [Test]
        public void Settings_TryGetProfile_RejectsUnknownName()
        {
            Assert.IsFalse(AegisSettings.instance.TryGetProfile("Nope", out AegisValidationProfile unknown));
            Assert.IsNull(unknown);

            Assert.IsTrue(AegisSettings.instance.TryGetProfile("build", out AegisValidationProfile build));
            Assert.AreEqual("Build", build.Name);
        }

        [Test]
        public void JUnit_FailuresAttributeMatchesFailureElements()
        {
            List<AegisRuleExecutionRecord> records = new List<AegisRuleExecutionRecord>
            {
                new AegisRuleExecutionRecord("a", "Passed Rule", AegisRuleExecutionStatus.Passed, 0, 1d, string.Empty),
                new AegisRuleExecutionRecord("b", "Findings Rule", AegisRuleExecutionStatus.Findings, 2, 1d, string.Empty),
                new AegisRuleExecutionRecord("c", "Failed Rule", AegisRuleExecutionStatus.Failed, 1, 1d, "Boom")
            };
            AegisValidationReport report = new AegisValidationReport("Test", 1d, new List<AegisFinding>
            {
                new AegisFinding("b", "Findings Rule", AegisSeverity.Error, "Blocking finding")
            }, records);
            string path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "aegis-junit-" + System.Guid.NewGuid().ToString("N") + ".xml");

            try
            {
                AegisReportWriters.WriteJUnit(report, path);
                string xml = System.IO.File.ReadAllText(path);

                System.Xml.XmlDocument document = new System.Xml.XmlDocument();
                document.LoadXml(xml);
                Assert.AreEqual(1, document.SelectNodes("//failure").Count);
                Assert.AreEqual("1", document.DocumentElement.GetAttribute("failures"));
                Assert.AreEqual(1, document.SelectNodes("//error").Count);
                Assert.AreEqual("1", document.DocumentElement.GetAttribute("errors"));
            }
            finally
            {
                System.IO.File.Delete(path);
            }
        }

        private sealed class ThrowingRule : AegisRuleAsset
        {
            public override void Evaluate(AegisValidationContext context, IAegisFindingSink sink)
            {
                throw new System.InvalidOperationException("Expected failure.");
            }
        }
    }
}
