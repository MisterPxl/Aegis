using System;
using System.Collections.Generic;
using UnityEngine;
using Valkyrie;

namespace MisterPxl.Aegis.ValkyrieIntegration.Tests
{
    public sealed class AegisRequiredParityFixture : AegisRequiredParityBase
    {
        public RequiredFields Nested = new RequiredFields();
        public RequiredStruct Structure;
        public List<RequiredFields> Entries = new List<RequiredFields> { new RequiredFields() };
        [SerializeReference] public RequiredNode Node;
        [SerializeReference] public RequiredNode SharedNode;
        [Required, SerializeReference] public RequiredNode MissingNode;
        [Required] public ExposedReference<UnityEngine.Object> Exposed;
        [Required] public string Whitespace = "   ";
        [Required] public string Empty = "";
        [Required("Choose a target")] public UnityEngine.Object Target;
        [Required, HideInInspector] public UnityEngine.Object HiddenTarget;
        [Required] public UnityEngine.Object[] EmptyCollection = Array.Empty<UnityEngine.Object>();
        [Required, NonSerialized] public UnityEngine.Object NotSerialized;
    }

    public abstract class AegisRequiredParityBase : ScriptableObject
    {
        [Required, SerializeField] private UnityEngine.Object _inheritedTarget;
    }

    [Serializable]
    public sealed class RequiredFields
    {
        [Required] public UnityEngine.Object Target;
    }

    [Serializable]
    public struct RequiredStruct
    {
        [Required] public string Text;
    }

    [Serializable]
    public abstract class RequiredNode
    {
        [Required, SerializeField] private UnityEngine.Object _inheritedNodeTarget;
    }

    [Serializable]
    public sealed class RequiredCycleNode : RequiredNode
    {
        [Required] public string Text = "";
        [SerializeReference] public RequiredNode Next;
    }
}
