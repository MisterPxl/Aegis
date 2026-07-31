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

                scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
                GameObject gameObject = new GameObject("Scene Object");
                SceneManager.MoveGameObjectToScene(gameObject, scene);
                Assert.IsTrue(EditorSceneManager.SaveScene(scene, scenePath));
                EditorSceneManager.CloseScene(scene, true);
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

        private sealed class ThrowingRule : AegisRuleAsset
        {
            public override void Evaluate(AegisValidationContext context, IAegisFindingSink sink)
            {
                throw new System.InvalidOperationException("Expected failure.");
            }
        }
    }
}
