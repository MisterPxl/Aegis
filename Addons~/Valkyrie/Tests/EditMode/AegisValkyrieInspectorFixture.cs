using UnityEngine;
using global::Astra.Valkyrie;

namespace Astra.Aegis.Integrations.Valkyrie.Tests
{
    public sealed class AegisValkyrieInspectorFixture : ScriptableObject
    {
        [Required] public Object RequiredObject;
        public string Label;
    }
}
