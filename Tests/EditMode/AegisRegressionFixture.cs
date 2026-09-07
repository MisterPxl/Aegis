using System;
using UnityEngine;

namespace MisterPxl.Aegis.Tests
{
    public sealed class AegisRegressionFixture : ScriptableObject
    {
        [SerializeReference] public AegisRegressionNode Node;
        [HideInInspector] public UnityEngine.Object HiddenReference;
        public UnityEngine.Object VisibleReference;
        [HideInInspector] public UnityEngine.Object[] HiddenCollection = new UnityEngine.Object[1];
    }

    [Serializable]
    public sealed class AegisRegressionNode
    {
        [SerializeReference] public AegisRegressionNode Next;
        public UnityEngine.Object[] Items = new UnityEngine.Object[1];
    }
}
