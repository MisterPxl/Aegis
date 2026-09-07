using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Valkyrie;
using Valkyrie.Editor;

namespace MisterPxl.Aegis.ValkyrieIntegration.Tests
{
    public sealed class AegisRequiredParityTests
    {
        private string _folder;
        private string _path;
        private AegisRequiredParityFixture _fixture;
        private ValkyrieRequiredRuleAsset _rule;

        [SetUp]
        public void SetUp()
        {
            _folder = "Assets/AegisRequiredParity_" + Guid.NewGuid().ToString("N");
            AssetDatabase.CreateFolder("Assets", System.IO.Path.GetFileName(_folder));
            _path = _folder + "/Fixture.asset";
            _fixture = ScriptableObject.CreateInstance<AegisRequiredParityFixture>();
            var node = new RequiredCycleNode();
            node.Next = node;
            _fixture.Node = _fixture.SharedNode = node;
            AssetDatabase.CreateAsset(_fixture, _path);
            AssetDatabase.SaveAssets();
            _rule = ScriptableObject.CreateInstance<ValkyrieRequiredRuleAsset>();
        }

        [TearDown]
        public void TearDown()
        {
            UnityEngine.Object.DestroyImmediate(_rule);
            AssetDatabase.DeleteAsset(_folder);
        }

        [Test]
        public void Required_TraversesNestedCollectionsStructsAndInheritedPrivateFields()
        {
            var findings = Evaluate();
            CollectionAssert.AreEquivalent(new[]
            {
                "_inheritedTarget", "Nested.Target", "Structure.Text", "Entries.Array.data[0].Target",
                "Node._inheritedNodeTarget", "Node.Text", "MissingNode", "Exposed", "Empty", "Target", "HiddenTarget"
            }, findings.Select(f => f.PropertyPath));
            Assert.That(findings.All(f => f.Code == "Aegis.Valkyrie.Required"));
            Assert.That(findings.All(f => f.AssetPath == _path));
            Assert.That(findings.All(f => !string.IsNullOrEmpty(f.GlobalObjectId)));
            Assert.That(findings.Single(f => f.PropertyPath == "Target").Message, Is.EqualTo("Choose a target"));
        }

        [TestCase("Nested.Target")]
        [TestCase("Entries.Array.data[0].Target")]
        [TestCase("Structure.Text")]
        [TestCase("Node.Text")]
        [TestCase("MissingNode")]
        [TestCase("Exposed")]
        [TestCase("Empty")]
        [TestCase("Whitespace")]
        [TestCase("EmptyCollection")]
        public void Required_MatchesInspectorPresenceAndMessage(string propertyPath)
        {
            using (var serialized = new SerializedObject(_fixture))
            {
                var property = serialized.FindProperty(propertyPath);
                Assert.That(property, Is.Not.Null);
                string inspector = PropertyRenderer.GetRequiredMessage(property, new RequiredAttribute());
                var finding = Evaluate().SingleOrDefault(f => f.PropertyPath == propertyPath);
                Assert.That(finding?.Message, Is.EqualTo(inspector));
            }
        }

        [Test]
        public void Required_ValidValuesDisappearAndReimportPreservesCycleSafety()
        {
            _fixture.Nested.Target = _fixture;
            _fixture.Entries[0].Target = _fixture;
            _fixture.Structure = new RequiredStruct { Text = "configured" };
            _fixture.Empty = "configured";
            _fixture.Target = _fixture;
            _fixture.Exposed = new ExposedReference<UnityEngine.Object> { defaultValue = _fixture };
            EditorUtility.SetDirty(_fixture);
            AssetDatabase.SaveAssets();
            Resources.UnloadAsset(_fixture);
            AssetDatabase.ImportAsset(_path, ImportAssetOptions.ForceUpdate);
            _fixture = AssetDatabase.LoadAssetAtPath<AegisRequiredParityFixture>(_path);
            var paths = Evaluate().Select(f => f.PropertyPath).ToArray();
            foreach (string resolved in new[] { "Nested.Target", "Entries.Array.data[0].Target", "Structure.Text", "Empty", "Target", "Exposed" })
                Assert.That(paths, Does.Not.Contain(resolved));
            Assert.That(paths, Does.Contain("Node.Text"));
            Assert.That(paths.Length, Is.EqualTo(5));
        }

        private List<AegisFinding> Evaluate()
        {
            var profile = new AegisValidationProfile();
            profile.IncludedFolders.Add(_folder);
            var findings = new List<AegisFinding>();
            _rule.Evaluate(new AegisValidationContext(profile), new AegisFindingSink(findings));
            return findings;
        }
    }
}
