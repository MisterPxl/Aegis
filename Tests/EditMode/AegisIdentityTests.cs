using System;
using NUnit.Framework;
using UnityEngine;

namespace Astra.Aegis.Tests
{
    public sealed class AegisIdentityTests
    {
        [TestCase(typeof(MissingMonoScriptRule), "MisterPxl.Aegis.MissingMonoScriptRule")]
        [TestCase(typeof(MissingObjectReferenceRule), "MisterPxl.Aegis.MissingObjectReferenceRule")]
        [TestCase(typeof(NullCollectionEntryRule), "MisterPxl.Aegis.NullCollectionEntryRule")]
        [TestCase(typeof(BuildSceneRule), "MisterPxl.Aegis.BuildSceneRule")]
        [TestCase(typeof(PrefabIntegrityRule), "MisterPxl.Aegis.PrefabIntegrityRule")]
        [TestCase(typeof(DuplicateAegisKeyRule), "MisterPxl.Aegis.DuplicateAegisKeyRule")]
        public void LegacyFallbackIdentitySurvivesNamespaceMigration(Type type, string identity)
        {
            var rule = (AegisRuleAsset)ScriptableObject.CreateInstance(type);
            try
            {
                Assert.That(rule.RuleId, Is.EqualTo(identity));
                var profile = new AegisValidationProfile();
                profile.DisabledRuleIds.Add(identity);
                Assert.That(profile.IsRuleEnabled(rule), Is.False);
            }
            finally { UnityEngine.Object.DestroyImmediate(rule); }
        }

        [Test]
        public void IdentityDoesNotSilentlyPropagateToAnotherRuleType()
        {
            var first = ScriptableObject.CreateInstance<IdentifiedRule>();
            var derived = ScriptableObject.CreateInstance<DerivedRule>();
            try
            {
                Assert.That(first.RuleId, Is.EqualTo("Example.StableIdentity"));
                Assert.That(derived.RuleId, Is.EqualTo(typeof(DerivedRule).FullName));
                Assert.That(first.RuleId, Is.Not.EqualTo(derived.RuleId));
            }
            finally { UnityEngine.Object.DestroyImmediate(first); UnityEngine.Object.DestroyImmediate(derived); }
        }

        [Test]
        public void EmptyIdentityIsRejected()
        {
            Assert.Throws<ArgumentException>(() => new AegisRuleIdentityAttribute(" "));
        }

        [AegisRuleIdentity("Example.StableIdentity")]
        private class IdentifiedRule : AegisRuleAsset
        {
            public override void Evaluate(AegisValidationContext context, IAegisFindingSink sink) { }
        }
        private sealed class DerivedRule : IdentifiedRule { }
    }
}
