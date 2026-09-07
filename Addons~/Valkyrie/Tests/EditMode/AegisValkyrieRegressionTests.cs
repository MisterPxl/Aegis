using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace MisterPxl.Aegis.ValkyrieIntegration.Tests
{
    public sealed class AegisValkyrieRegressionTests
    {
        private string _folder;

        [SetUp]
        public void SetUp()
        {
            _folder = "Assets/AegisValkyrieRegression_" + Guid.NewGuid().ToString("N");
            AssetDatabase.CreateFolder("Assets", System.IO.Path.GetFileName(_folder));
        }

        [TearDown]
        public void TearDown() => AssetDatabase.DeleteAsset(_folder);

        [TestCase(typeof(ValkyrieRequiredRuleAsset))]
        [TestCase(typeof(ValkyrieManagedReferenceRuleAsset))]
        public void RuleAsset_SurvivesReimport(Type type)
        {
            string path = _folder + "/Rule.asset";
            AegisRuleAsset rule = (AegisRuleAsset)ScriptableObject.CreateInstance(type);
            AssetDatabase.CreateAsset(rule, path);
            AssetDatabase.SaveAssets();
            Resources.UnloadAsset(rule);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            rule = AssetDatabase.LoadAssetAtPath<AegisRuleAsset>(path);
            Assert.IsNotNull(rule);
            Assert.AreEqual(type, rule.GetType());
        }

        [Test]
        public void ManagedReferenceRule_HandlesCyclesAndHiddenFields()
        {
            AegisValkyrieRegressionFixture fixture = ScriptableObject.CreateInstance<AegisValkyrieRegressionFixture>();
            fixture.Node = new AegisValkyrieNode();
            fixture.Node.Next = fixture.Node;
            AssetDatabase.CreateAsset(fixture, _folder + "/Fixture.asset");
            using (SerializedObject serialized = new SerializedObject(fixture))
            {
                int count = 0;
                foreach (SerializedProperty property in AegisSerializedProperties.Enumerate(serialized))
                    Assert.Less(++count, 100);
            }
            ValkyrieManagedReferenceRuleAsset rule = ScriptableObject.CreateInstance<ValkyrieManagedReferenceRuleAsset>();
            try
            {
                AegisValidationProfile profile = new AegisValidationProfile();
                profile.IncludedFolders.Add(_folder);
                List<AegisFinding> findings = new List<AegisFinding>();
                rule.Evaluate(new AegisValidationContext(profile), new AegisFindingSink(findings));
                Assert.AreEqual(1, findings.Count(f => f.PropertyPath == "HiddenNode"));
            }
            finally { UnityEngine.Object.DestroyImmediate(rule); }
        }
    }
}
