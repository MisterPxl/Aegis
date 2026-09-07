using System.Collections.Generic;
using UnityEditor;

namespace Astra.Aegis
{
    [UnityEngine.Scripting.APIUpdating.MovedFrom(false, "MisterPxl.Aegis", "Aegis.Editor", "AegisSerializedProperties")]
    public static class AegisSerializedProperties
    {
        // The yielded cursor is valid until the next iteration. Call Copy() to retain it.
        // Inspect hidden serialized fields too, but only descend into each managed object once.
        public static IEnumerable<SerializedProperty> Enumerate(SerializedObject serializedObject)
        {
            HashSet<long> visitedReferences = new HashSet<long>();
            using (SerializedProperty iterator = serializedObject.GetIterator())
            {
                bool enterChildren = true;
                while (iterator.Next(enterChildren))
                {
                    enterChildren = iterator.propertyType != SerializedPropertyType.String;
                    if (iterator.propertyType == SerializedPropertyType.ManagedReference)
                        enterChildren = visitedReferences.Add(iterator.managedReferenceId);

                    yield return iterator;
                }
            }
        }
    }
}
