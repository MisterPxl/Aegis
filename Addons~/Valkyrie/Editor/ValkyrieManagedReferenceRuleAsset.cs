using System;
using System.Collections.Generic;
using System.Reflection;
using MisterPxl.Aegis;
using UnityEditor;
using UnityEngine;
using Valkyrie;

namespace MisterPxl.Aegis.ValkyrieIntegration
{
    [CreateAssetMenu(fileName = "ValkyrieManagedReferenceRule", menuName = "Astra/Aegis/Valkyrie/Managed Reference Rule")]
    public sealed class ValkyrieManagedReferenceRuleAsset : AegisProjectAssetRule
    {
        public override void Evaluate(AegisValidationContext context, IAegisFindingSink sink)
        {
            EvaluateProjectObjects(context, sink, EvaluateObject, "Assets", "Packages");
        }

        private void EvaluateObject(string path, UnityEngine.Object obj, IAegisFindingSink sink)
        {
            if (obj == null)
                return;

            SerializedObject serializedObject;
            try
            {
                serializedObject = new SerializedObject(obj);
            }
            catch
            {
                return;
            }

            using (serializedObject)
            {
                foreach (SerializedProperty iterator in AegisSerializedProperties.Enumerate(serializedObject))
                {
                    if (iterator.propertyType != SerializedPropertyType.ManagedReference)
                        continue;

                    if (string.IsNullOrEmpty(iterator.managedReferenceFullTypename))
                    {
                        sink.Add(CreateFinding(
                            $"Managed reference '{iterator.displayName}' is null.",
                            assetPath: path,
                            globalObjectId: AegisObjectId.TryGet(obj),
                            propertyPath: iterator.propertyPath,
                            code: "Aegis.Valkyrie.ManagedReferenceNull",
                            severity: AegisSeverity.Warning));
                        continue;
                    }

                    if (TryResolveType(iterator.managedReferenceFullTypename) == null)
                    {
                        sink.Add(CreateFinding(
                            $"Managed reference '{iterator.displayName}' points to a missing type.",
                            iterator.managedReferenceFullTypename,
                            path,
                            AegisObjectId.TryGet(obj),
                            iterator.propertyPath,
                            "Aegis.Valkyrie.ManagedReferenceMissingType"));
                    }
                }
            }
        }

        private static Type TryResolveType(string fullTypename)
        {
            int split = fullTypename.IndexOf(' ');
            if (split < 0 || split >= fullTypename.Length - 1)
                return null;

            string assemblyName = fullTypename.Substring(0, split);
            string typeName = fullTypename.Substring(split + 1);
            return Type.GetType($"{typeName}, {assemblyName}");
        }
    }
}
