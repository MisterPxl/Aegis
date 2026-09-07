using UnityEditor;
using UnityEngine;

namespace MisterPxl.Aegis
{
    [CreateAssetMenu(fileName = "MissingObjectReferenceRule", menuName = "Astra/Aegis/Rules/Missing Object Reference")]
    public sealed class MissingObjectReferenceRule : AegisProjectAssetRule
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
                    if (iterator.propertyType != SerializedPropertyType.ObjectReference)
                        continue;

                    // Keep the Unity 6000.0-compatible API until the package minimum changes.
#pragma warning disable CS0618
                    bool isMissing = iterator.objectReferenceValue == null && iterator.objectReferenceInstanceIDValue != 0;
#pragma warning restore CS0618
                    if (isMissing)
                    {
                        sink.Add(CreateFinding(
                            $"Missing object reference on '{obj.name}'.",
                            assetPath: path,
                            globalObjectId: AegisObjectId.TryGet(obj),
                            propertyPath: iterator.propertyPath,
                            code: "Aegis.MissingObjectReference"));
                    }
                }
            }
        }
    }
}
