using System;
using System.Collections.Generic;
using System.Reflection;
using MisterPxl.Aegis;
using UnityEditor;
using UnityEngine;
using Valkyrie;

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

            FieldInfo[] fields = obj.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            for (int i = 0; i < fields.Length; i++)
            {
                FieldInfo field = fields[i];
                RequiredAttribute required = field.GetCustomAttribute<RequiredAttribute>(true);
                if (required == null)
                    continue;

                object value = field.GetValue(obj);
                if (!IsMissing(value))
                    continue;

                string message = string.IsNullOrWhiteSpace(required.Message)
                    ? $"Required field '{ObjectNames.NicifyVariableName(field.Name)}' is missing."
                    : required.Message;
                sink.Add(CreateFinding(
                    message,
                    assetPath: path,
                    globalObjectId: AegisObjectId.TryGet(obj),
                    propertyPath: field.Name,
                    code: "Aegis.Valkyrie.Required"));
            }
        }

        private static bool IsMissing(object value)
        {
            if (value == null)
                return true;

            if (value is string text)
                return string.IsNullOrWhiteSpace(text);

            if (value is UnityEngine.Object unityObject)
                return unityObject == null;

            return false;
        }
    }
}
