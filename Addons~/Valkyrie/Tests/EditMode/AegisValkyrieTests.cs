using NUnit.Framework;
using UnityEngine;
using global::Astra.Valkyrie;

namespace Astra.Aegis.Integrations.Valkyrie.Tests
{
    [UnityEngine.Scripting.APIUpdating.MovedFrom(false, "MisterPxl.Aegis.ValkyrieIntegration.Tests", "Aegis.Valkyrie.Editor.Tests", "AegisValkyrieTests")]
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
