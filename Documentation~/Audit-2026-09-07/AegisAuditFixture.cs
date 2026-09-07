using System;
using UnityEngine;
namespace Astra.Aegis.Tests {
public sealed class AegisAuditFixture : ScriptableObject {
 [SerializeReference] public AegisAuditNode node;
 [HideInInspector] public UnityEngine.Object hiddenReference;
 public UnityEngine.Object visibleReference;
}
[Serializable] public sealed class AegisAuditNode { [SerializeReference] public AegisAuditNode next; }
}
