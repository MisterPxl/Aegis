using System;
using UnityEngine;

namespace MisterPxl.Aegis.ValkyrieIntegration.Tests
{
    public sealed class AegisValkyrieRegressionFixture : ScriptableObject
    {
        [SerializeReference] public AegisValkyrieNode Node;
        [SerializeReference, HideInInspector] public AegisValkyrieNode HiddenNode;
    }

    [Serializable]
    public sealed class AegisValkyrieNode
    {
        [SerializeReference] public AegisValkyrieNode Next;
    }
}
