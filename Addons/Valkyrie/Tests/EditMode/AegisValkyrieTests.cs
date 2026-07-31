using NUnit.Framework;
using UnityEngine;
using Valkyrie;

namespace MisterPxl.Aegis.ValkyrieIntegration.Tests
{
    public sealed class AegisValkyrieTests
    {
        [Test]
        public void RequiredRule_CanBeCreated()
        {
            ValkyrieRequiredRuleAsset rule = ScriptableObject.CreateInstance<ValkyrieRequiredRuleAsset>();

            Assert.IsNotNull(rule);
            Assert.IsTrue(rule.EnabledByDefault);

            Object.DestroyImmediate(rule);
        }

        private sealed class RequiredFixture : ScriptableObject
        {
            [Required] public Object RequiredObject;
        }
    }
}
