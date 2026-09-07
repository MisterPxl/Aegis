using System;
using UnityEngine;

namespace Astra.Aegis.Tests
{
    [UnityEngine.Scripting.APIUpdating.MovedFrom(false, "MisterPxl.Aegis.Tests", "Aegis.Editor.Tests", "AegisRegressionFixture")]
    public sealed class AegisRegressionFixture : ScriptableObject
    {
        [SerializeReference] public AegisRegressionNode Node;
        [HideInInspector] public UnityEngine.Object HiddenReference;
        public UnityEngine.Object VisibleReference;
        [HideInInspector] public UnityEngine.Object[] HiddenCollection = new UnityEngine.Object[1];
    }

    [Serializable]
    [UnityEngine.Scripting.APIUpdating.MovedFrom(false, "MisterPxl.Aegis.Tests", "Aegis.Editor.Tests", "AegisRegressionNode")]
    public sealed class AegisRegressionNode
    {
        [SerializeReference] public AegisRegressionNode Next;
        public UnityEngine.Object[] Items = new UnityEngine.Object[1];
    }
}
