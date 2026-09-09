using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using global::Astra.Valkyrie;
using global::Astra.Valkyrie.Editor;

namespace Astra.Aegis.Integrations.Valkyrie.Tests
{
    public sealed class AegisValkyrieInspectorTests
    {
        private const string Folder = "Assets/AegisValkyrieAegisValkyrieInspectorFixture";
        private AegisValkyrieInspectorFixture _fixture;
        private AegisValkyrieInspectorFixture _other;
        private ValkyrieRequiredRuleAsset _rule;

        [SetUp]
        public void SetUp()
        {
            if (!AssetDatabase.IsValidFolder(Folder)) AssetDatabase.CreateFolder("Assets", System.IO.Path.GetFileName(Folder));
            _fixture = ScriptableObject.CreateInstance<AegisValkyrieInspectorFixture>(); AssetDatabase.CreateAsset(_fixture, Folder + "/Fixture.asset");
            _other = ScriptableObject.CreateInstance<AegisValkyrieInspectorFixture>(); _other.RequiredObject = _fixture; AssetDatabase.CreateAsset(_other, Folder + "/Other.asset");
            _rule = ScriptableObject.CreateInstance<ValkyrieRequiredRuleAsset>(); AssetDatabase.CreateAsset(_rule, Folder + "/Rule.asset");
            AegisValkyrieInspectorCache.InvalidateAll();
        }

        [TearDown]
        public void TearDown()
        {
            AegisValkyrieInspectorCache.InvalidateAll();
            AssetDatabase.DeleteAsset(Folder);
        }

        [Test]
        public void ExtensionIsRegisteredThroughTheValkyrieCompositionPoint()
        {
            AegisValkyrieInspectorExtension.Register();
            Assert.That(AegisValkyrieInspectorExtension.IsRegistered, Is.True);
            Assert.That(ValkyrieInspectorExtensions.Registered.OfType<AegisValkyrieInspectorExtension>().Count(), Is.EqualTo(1));
            AegisValkyrieInspectorExtension.Unregister();
            Assert.That(AegisValkyrieInspectorExtension.IsRegistered, Is.False);
            AegisValkyrieInspectorExtension.Register();
        }

        [Test]
        public void TargetedValidationScopesToTheAssetAndAttachesFindingsToFields()
        {
            Assert.That(AegisValkyrieInspectorCache.RunTargeted(new UnityEngine.Object[] { _fixture }, out string message), Is.True, message);
            Assert.That(message, Does.Contain(Folder + "/Fixture.asset").And.Not.Contain("Other.asset"));
            Assert.That(AegisValkyrieInspectorCache.TryGet(_fixture, out AegisInspectorFindings findings), Is.True);
            Assert.That(findings.Source, Is.EqualTo("targeted"));
            Assert.That(findings.ErrorCount, Is.EqualTo(1), string.Join("; ", findings.Findings.Select(f => f.Code + " " + f.PropertyPath)));
            var attached = new List<AegisFinding>();
            findings.CollectForProperty("RequiredObject", attached);
            Assert.That(attached, Has.Count.EqualTo(1));
            Assert.That(attached[0].Code, Is.EqualTo("Aegis.Valkyrie.Required"));
            var unattached = new List<AegisFinding>();
            findings.CollectUnattached(new[] { "RequiredObject", "Label" }, unattached);
            Assert.That(unattached, Is.Empty);
            Assert.That(AegisValkyrieInspectorExtension.Summarize(findings), Does.StartWith("Aegis: 1 error (validated just now)"));
            Assert.That(AegisValkyrieInspectorCache.TryGet(_other, out AegisInspectorFindings otherFindings) && otherFindings.Source == "targeted", Is.False, "only the validated object gets a targeted entry");
        }

        [Test]
        public void UndoModificationInvalidatesTheTargetedEntryOnly()
        {
            AegisValkyrieInspectorCache.EnsureHooks();
            Assert.That(AegisValkyrieInspectorCache.RunTargeted(new UnityEngine.Object[] { _fixture, _other }, out _), Is.True);
            Assert.That(AegisValkyrieInspectorCache.Count, Is.EqualTo(2));
            Undo.RecordObject(_fixture, "edit");
            _fixture.Label = "changed";
            Undo.FlushUndoRecordObjects();
            AegisValkyrieInspectorCache.Invalidate(_fixture);
            Assert.That(AegisValkyrieInspectorCache.TryGet(_fixture, out AegisInspectorFindings after) && after.Source == "targeted", Is.False, "modified object loses its targeted entry");
            Assert.That(AegisValkyrieInspectorCache.TryGet(_other, out AegisInspectorFindings kept), Is.True);
            Assert.That(kept.Source, Is.EqualTo("targeted"));
        }

        [Test]
        public void LastReportFindingsAreServedPerObjectWithTheirAge()
        {
            string id = AegisObjectId.TryGet(_fixture);
            var finding = new AegisFinding("rule", "Rule", AegisSeverity.Warning, "From report", null, Folder + "/Fixture.asset", id, "Label", "Test.Code", null);
            var report = new AegisValidationReport("Full", 1, new List<AegisFinding> { finding }, new List<AegisRuleExecutionRecord>());
            AegisReportStore.SaveLastReport(report);
            AegisValkyrieInspectorCache.InvalidateAll();
            Assert.That(AegisValkyrieInspectorCache.TryGet(_fixture, out AegisInspectorFindings findings), Is.True);
            Assert.That(findings.Source, Is.EqualTo("report")); Assert.That(findings.WarningCount, Is.EqualTo(1));
            Assert.That(AegisValkyrieInspectorExtension.Summarize(findings), Does.StartWith("Aegis: 1 warning (report "));
            Assert.That(AegisValkyrieInspectorCache.TryGet(_other, out AegisInspectorFindings none), Is.True);
            Assert.That(none.Findings, Is.Empty);
            Assert.That(AegisInspectorFindings.MatchesProperty("_bindings.Array.data[0]._target", "_bindings"), Is.True);
            Assert.That(AegisInspectorFindings.MatchesProperty("_bindingsExtra", "_bindings"), Is.False);
            Assert.That(AegisInspectorFindings.FormatAge(DateTime.UtcNow.AddMinutes(-5), DateTime.UtcNow), Is.EqualTo("5 min ago"));
        }

        [Test]
        public void UnsavedObjectsCannotBeValidatedAndSayWhy()
        {
            var transient = new GameObject("Transient");
            try
            {
                Assert.That(AegisValkyrieInspectorCache.RunTargeted(new UnityEngine.Object[] { transient }, out string message), Is.False);
                Assert.That(message, Does.Contain("Save the scene"));
            }
            finally { UnityEngine.Object.DestroyImmediate(transient); }
        }
    }
}
