using System;
using UnityEngine;

namespace Astra.Aegis.Integrations.Valkyrie.Tests
{
    [UnityEngine.Scripting.APIUpdating.MovedFrom(false, "MisterPxl.Aegis.ValkyrieIntegration.Tests", "Aegis.Valkyrie.Editor.Tests", "AegisValkyrieRegressionFixture")]
    public sealed class AegisValkyrieRegressionFixture : ScriptableObject
    {
        [SerializeReference] public AegisValkyrieNode Node;
        [SerializeReference, HideInInspector] public AegisValkyrieNode HiddenNode;
    }

    [Serializable]
    [UnityEngine.Scripting.APIUpdating.MovedFrom(false, "MisterPxl.Aegis.ValkyrieIntegration.Tests", "Aegis.Valkyrie.Editor.Tests", "AegisValkyrieNode")]
    public sealed class AegisValkyrieNode
    {
        [SerializeReference] public AegisValkyrieNode Next;
    }
}
