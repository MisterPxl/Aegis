using MisterPxl.Aegis;
using UnityEditor;
using UnityEngine;

namespace Aegis.Samples
{
    [CreateAssetMenu(fileName = "SceneNamingRule", menuName = "Aegis/Samples/Scene Naming Rule")]
    public sealed class SceneNamingRule : AegisRuleAsset
    {
        public override void Evaluate(AegisValidationContext context, IAegisFindingSink sink)
        {
            string[] scenePaths = context.FindAssetPaths("t:Scene", "Assets");
            for (int i = 0; i < scenePaths.Length; i++)
            {
                string fileName = System.IO.Path.GetFileNameWithoutExtension(scenePaths[i]);
                if (!fileName.StartsWith("Scene_", System.StringComparison.Ordinal))
                {
                    sink.Add(CreateFinding(
                        $"Scene '{fileName}' does not use the sample Scene_ prefix.",
                        assetPath: scenePaths[i],
                        code: "Sample.SceneNaming",
                        severity: AegisSeverity.Info));
                }
            }
        }
    }
}
