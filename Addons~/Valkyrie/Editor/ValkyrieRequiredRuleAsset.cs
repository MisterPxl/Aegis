using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using Valkyrie;
using Valkyrie.Editor;

namespace MisterPxl.Aegis.ValkyrieIntegration
{
    [CreateAssetMenu(fileName = "ValkyrieRequiredRule", menuName = "Astra/Aegis/Valkyrie/Required Rule")]
    public sealed class ValkyrieRequiredRuleAsset : AegisProjectAssetRule
    {
        public override void Evaluate(AegisValidationContext context, IAegisFindingSink sink)
        {
            EvaluateProjectObjects(context, sink, EvaluateObject, "Assets", "Packages");
        }

        private void EvaluateObject(string path, UnityEngine.Object obj, IAegisFindingSink sink)
        {
            if (obj == null)
                return;

            using (var serialized = new SerializedObject(obj))
            {
                foreach (SerializedProperty property in AegisSerializedProperties.Enumerate(serialized))
                {
                    // Attribute ownership belongs to the serialized field, not its list elements.
                    if (property.name.StartsWith("data[", StringComparison.Ordinal))
                        continue;
                    object[] owners = SerializedPropertyContext.GetOwners(property);
                    FieldInfo field = FindField(owners.Length == 0 ? null : owners[0]?.GetType(), property.name);
                    RequiredAttribute required = field?.GetCustomAttribute<RequiredAttribute>(true);
                    if (required == null)
                        continue;

                    // Use the inspector's presence semantics, including managed/exposed references.
                    string message = PropertyRenderer.GetRequiredMessage(property, required);
                    if (message == null)
                        continue;

                    sink.Add(CreateFinding(
                        message,
                        assetPath: path,
                        globalObjectId: AegisObjectId.TryGet(obj),
                        propertyPath: property.propertyPath,
                        code: "Aegis.Valkyrie.Required"));
                }
            }
        }

        private static FieldInfo FindField(Type type, string name)
        {
            // Validation includes hidden serialized fields; the inspector's layout cache omits them.
            for (Type current = type; current != null; current = current.BaseType)
            {
                FieldInfo field = current.GetField(name, BindingFlags.Instance | BindingFlags.Public
                    | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
                if (field != null)
                    return field;
            }
            return null;
        }
    }
}
