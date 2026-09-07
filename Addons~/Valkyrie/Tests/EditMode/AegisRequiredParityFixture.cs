using System;
using System.Collections.Generic;
using UnityEngine;
using global::Astra.Valkyrie;

namespace Astra.Aegis.Integrations.Valkyrie.Tests
{
    [UnityEngine.Scripting.APIUpdating.MovedFrom(false, "MisterPxl.Aegis.ValkyrieIntegration.Tests", "Aegis.Valkyrie.Editor.Tests", "AegisRequiredParityFixture")]
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

    [UnityEngine.Scripting.APIUpdating.MovedFrom(false, "MisterPxl.Aegis.ValkyrieIntegration.Tests", "Aegis.Valkyrie.Editor.Tests", "AegisRequiredParityBase")]
    public abstract class AegisRequiredParityBase : ScriptableObject
    {
        [Required, SerializeField] private UnityEngine.Object _inheritedTarget;
    }

    [Serializable]
    [UnityEngine.Scripting.APIUpdating.MovedFrom(false, "MisterPxl.Aegis.ValkyrieIntegration.Tests", "Aegis.Valkyrie.Editor.Tests", "RequiredFields")]
    public sealed class RequiredFields
    {
        [Required] public UnityEngine.Object Target;
    }

    [Serializable]
    [UnityEngine.Scripting.APIUpdating.MovedFrom(false, "MisterPxl.Aegis.ValkyrieIntegration.Tests", "Aegis.Valkyrie.Editor.Tests", "RequiredStruct")]
    public struct RequiredStruct
    {
        [Required] public string Text;
    }

    [Serializable]
    [UnityEngine.Scripting.APIUpdating.MovedFrom(false, "MisterPxl.Aegis.ValkyrieIntegration.Tests", "Aegis.Valkyrie.Editor.Tests", "RequiredNode")]
    public abstract class RequiredNode
    {
        [Required, SerializeField] private UnityEngine.Object _inheritedNodeTarget;
    }

    [Serializable]
    [UnityEngine.Scripting.APIUpdating.MovedFrom(false, "MisterPxl.Aegis.ValkyrieIntegration.Tests", "Aegis.Valkyrie.Editor.Tests", "RequiredCycleNode")]
    public sealed class RequiredCycleNode : RequiredNode
    {
        [Required] public string Text = "";
        [SerializeReference] public RequiredNode Next;
    }
}
