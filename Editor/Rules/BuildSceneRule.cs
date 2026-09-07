using UnityEditor;
using UnityEngine;

namespace Astra.Aegis
{
    [CreateAssetMenu(fileName = "BuildSceneRule", menuName = "Astra/Aegis/Rules/Build Scene")]
    [UnityEngine.Scripting.APIUpdating.MovedFrom(false, "MisterPxl.Aegis", "Aegis.Editor", "BuildSceneRule")]
    [AegisRuleIdentity("MisterPxl.Aegis.BuildSceneRule")]
    public sealed class BuildSceneRule : AegisRuleAsset
    {
        public override void Evaluate(AegisValidationContext context, IAegisFindingSink sink)
        {
            EditorBuildSettingsScene[] scenes = EditorBuildSettings.scenes;
            if (scenes == null || scenes.Length == 0)
            {
                sink.Add(CreateFinding(
                    "No scenes are configured in Build Settings.",
                    code: "Aegis.BuildScenes.Empty"));
                return;
            }

            bool firstEnabledSeen = false;
            for (int i = 0; i < scenes.Length; i++)
            {
                EditorBuildSettingsScene scene = scenes[i];
                if (scene == null || string.IsNullOrWhiteSpace(scene.path))
                {
                    sink.Add(CreateFinding($"Build scene entry {i} has an empty path.", code: "Aegis.BuildScenes.EmptyPath"));
                    continue;
                }

                if (!AssetDatabase.LoadAssetAtPath<SceneAsset>(scene.path))
                {
                    sink.Add(CreateFinding(
                        $"Build scene '{scene.path}' does not exist.",
                        assetPath: scene.path,
                        code: "Aegis.BuildScenes.Missing"));
                    continue;
                }

                if (!firstEnabledSeen)
                {
                    if (!scene.enabled)
                    {
                        sink.Add(CreateFinding(
                            $"First build scene '{scene.path}' is disabled.",
                            assetPath: scene.path,
                            code: "Aegis.BuildScenes.FirstDisabled"));
                    }
                    else
                    {
                        firstEnabledSeen = true;
                    }
                }
            }
        }
    }
}
