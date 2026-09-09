using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using global::Astra.Valkyrie.Integrations.DOTween;

namespace Astra.Aegis.Integrations.Valkyrie.DOTween.Tests
{
    public sealed class AegisValkyrieTweenRuleTests
    {
        private const string Folder = "Assets/AegisValkyrieTweenRuleFixture";
        private ValkyrieTweenConfigurationRuleAsset _rule;
        private TweenSequenceAsset _asset;
        private TransformMoveStepDefinition _step;
        private GameObject _prefab;
        private string _prefabPath;

        [SetUp]
        public void SetUp()
        {
            if (!AssetDatabase.IsValidFolder(Folder))
                AssetDatabase.CreateFolder("Assets", System.IO.Path.GetFileName(Folder));
            _rule = ScriptableObject.CreateInstance<ValkyrieTweenConfigurationRuleAsset>();
            _asset = ScriptableObject.CreateInstance<TweenSequenceAsset>();
            _step = new TransformMoveStepDefinition { Duration = 0.5f, EndValue = Vector3.one };
            _step.Target.Mode = TweenTargetMode.Key;
            _step.Target.Key = "Target";
            _asset.Timeline.Steps.Add(_step);
            AssetDatabase.CreateAsset(_asset, Folder + "/Sequence.asset");
            var go = new GameObject("TweenFixture");
            var child = new GameObject("Child").transform;
            child.SetParent(go.transform, false);
            var player = go.AddComponent<TweenPlayer>();
            using (var so = new SerializedObject(player))
            {
                so.FindProperty("_sourceMode").intValue = (int)TweenPlayerSourceMode.Asset;
                so.FindProperty("_asset").objectReferenceValue = _asset;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
            player.Bindings.Add(new TweenTargetBinding("Target", child));
            _prefabPath = Folder + "/Player.prefab";
            _prefab = PrefabUtility.SaveAsPrefabAsset(go, _prefabPath);
            Object.DestroyImmediate(go);
        }

        [TearDown]
        public void TearDown()
        {
            if (_rule != null) Object.DestroyImmediate(_rule);
            AssetDatabase.DeleteAsset(Folder);
        }

        [Test]
        public void ValidPlayerAndAssetProduceNoFindingsAndBuildNoTween()
        {
            int before = global::DG.Tweening.DOTween.TotalActiveTweens();
            var findings = Evaluate();
            Assert.That(findings.Where(f => f.Severity == AegisSeverity.Error), Is.Empty, Describe(findings));
            Assert.That(findings.Where(f => f.Code.StartsWith(ValkyrieTweenConfigurationRuleAsset.CodePrefix)), Is.Empty, Describe(findings));
            Assert.That(global::DG.Tweening.DOTween.TotalActiveTweens(), Is.EqualTo(before), "validation must not build or play tweens");
        }

        [Test]
        public void MissingBindingKeyIsAnErrorOnThePlayerBindings()
        {
            EditPlayer(so => so.FindProperty("_bindings").arraySize = 0);
            var finding = Evaluate().Single(f => f.Code == ValkyrieTweenConfigurationRuleAsset.CodePrefix + "MissingBinding");
            Assert.That(finding.Severity, Is.EqualTo(AegisSeverity.Error));
            Assert.That(finding.AssetPath, Is.EqualTo(_prefabPath));
            Assert.That(finding.PropertyPath, Is.EqualTo("_bindings"));
            Assert.That(finding.Message, Does.Contain("'Target'"));
            Assert.That(finding.GlobalObjectId, Is.Not.Empty);
        }

        [Test]
        public void DeclaredBindingWithoutTargetIsReportedAsRuntimeProvided()
        {
            EditPlayer(so => so.FindProperty("_bindings").GetArrayElementAtIndex(0).FindPropertyRelative("_target").objectReferenceValue = null);
            var findings = Evaluate();
            Assert.That(findings.Where(f => f.Severity == AegisSeverity.Error), Is.Empty, Describe(findings));
            var finding = findings.Single(f => f.Code == ValkyrieTweenConfigurationRuleAsset.RuntimeBindingCode);
            Assert.That(finding.Severity, Is.EqualTo(AegisSeverity.Info));
            Assert.That(finding.PropertyPath, Is.EqualTo("_bindings.Array.data[0]._target"));
        }

        [Test]
        public void IncompatibleBindingTargetIsAnErrorOnThatBinding()
        {
            EditPlayer(so => so.FindProperty("_bindings").GetArrayElementAtIndex(0).FindPropertyRelative("_target").objectReferenceValue = _asset);
            var finding = Evaluate().Single(f => f.Code == ValkyrieTweenConfigurationRuleAsset.CodePrefix + "WrongBindingType");
            Assert.That(finding.Severity, Is.EqualTo(AegisSeverity.Error));
            Assert.That(finding.PropertyPath, Is.EqualTo("_bindings.Array.data[0]._target"));
            Assert.That(finding.Details, Does.Contain("UnityEngine.Transform"));
        }

        [Test]
        public void InvalidStepValueIsReportedOnTheSequenceAssetOnly()
        {
            using (var so = new SerializedObject(_asset))
            {
                so.FindProperty("_timeline._steps").GetArrayElementAtIndex(0).FindPropertyRelative("_duration").floatValue = -1f;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
            AssetDatabase.SaveAssets();
            var findings = Evaluate().Where(f => f.Code == ValkyrieTweenConfigurationRuleAsset.CodePrefix + "InvalidValue").ToList();
            Assert.That(findings, Has.Count.EqualTo(1), Describe(findings));
            Assert.That(findings[0].AssetPath, Is.EqualTo(Folder + "/Sequence.asset"));
            Assert.That(findings[0].PropertyPath, Is.EqualTo("_timeline._steps.Array.data[0]"));
        }

        [Test]
        public void StaleStepEventBindingIsAWarningOnItsStepId()
        {
            EditPlayer(so =>
            {
                var events = so.FindProperty("_stepEvents");
                events.arraySize = 1;
                events.GetArrayElementAtIndex(0).FindPropertyRelative("_stepId").stringValue = "ghost-step";
            });
            var finding = Evaluate().Single(f => f.Code == ValkyrieTweenConfigurationRuleAsset.StaleStepEventCode);
            Assert.That(finding.Severity, Is.EqualTo(AegisSeverity.Warning));
            Assert.That(finding.PropertyPath, Is.EqualTo("_stepEvents.Array.data[0]._stepId"));
            Assert.That(finding.Message, Does.Contain("ghost-step"));
        }

        [Test]
        public void FindingIdentityIsStableAcrossEvaluations()
        {
            EditPlayer(so => so.FindProperty("_bindings").arraySize = 0);
            var first = Evaluate().Single(f => f.Code.EndsWith("MissingBinding"));
            var second = Evaluate().Single(f => f.Code.EndsWith("MissingBinding"));
            Assert.That(first.RuleId, Is.EqualTo("Astra.Aegis.Integrations.Valkyrie.DOTween.TweenConfigurationRule"));
            Assert.That(first.Fingerprint, Is.EqualTo(second.Fingerprint));
            Assert.That(first.Fingerprint, Is.EqualTo(AegisFingerprint.Compute(first.RuleId, first.Code, first.AssetPath, first.GlobalObjectId, first.PropertyPath)));
        }

        private void EditPlayer(System.Action<SerializedObject> edit)
        {
            var root = PrefabUtility.LoadPrefabContents(_prefabPath);
            try
            {
                using (var so = new SerializedObject(root.GetComponent<TweenPlayer>()))
                {
                    edit(so);
                    so.ApplyModifiedPropertiesWithoutUndo();
                }
                PrefabUtility.SaveAsPrefabAsset(root, _prefabPath);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }

        private List<AegisFinding> Evaluate()
        {
            var profile = new AegisValidationProfile();
            profile.IncludedFolders.Add(Folder);
            var findings = new List<AegisFinding>();
            _rule.Evaluate(new AegisValidationContext(profile), new AegisFindingSink(findings));
            return findings;
        }

        private static string Describe(IEnumerable<AegisFinding> findings) => string.Join("\n", findings.Select(f => f.Severity + " " + f.Code + " " + f.PropertyPath + " " + f.Message));
    }
}
