using UnityEditor;
using UnityEngine;

namespace MisterPxl.Aegis
{
    [CreateAssetMenu(fileName = "NullCollectionEntryRule", menuName = "Aegis/Rules/Null Collection Entry")]
    public sealed class NullCollectionEntryRule : AegisProjectAssetRule
    {
        public override void Evaluate(AegisValidationContext context, IAegisFindingSink sink)
        {
            EvaluateProjectObjects(context, sink, EvaluateObject, "Assets");
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
                    if (!iterator.isArray || iterator.propertyType == SerializedPropertyType.String)
                        continue;

                    for (int index = 0; index < iterator.arraySize; index++)
                    {
                        SerializedProperty element = iterator.GetArrayElementAtIndex(index);
                        if (element == null || element.propertyType != SerializedPropertyType.ObjectReference)
                            continue;

                        // Keep the Unity 6000.0-compatible API until the package minimum changes.
#pragma warning disable CS0618
                        bool isNull = element.objectReferenceValue == null && element.objectReferenceInstanceIDValue == 0;
#pragma warning restore CS0618
                        if (isNull)
                        {
                            sink.Add(CreateFinding(
                                $"Null entry at index {index} in '{iterator.displayName}'.",
                                assetPath: path,
                                globalObjectId: AegisObjectId.TryGet(obj),
                                propertyPath: $"{iterator.propertyPath}.Array.data[{index}]",
                                code: "Aegis.NullCollectionEntry",
                                severity: AegisSeverity.Warning));
                        }
                    }
                }
            }
        }
    }
}
