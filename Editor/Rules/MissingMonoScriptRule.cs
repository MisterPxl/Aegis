using UnityEditor;
using UnityEngine;

namespace MisterPxl.Aegis
{
    [CreateAssetMenu(fileName = "MissingMonoScriptRule", menuName = "Aegis/Rules/Missing Mono Script")]
    public sealed class MissingMonoScriptRule : AegisProjectAssetRule
    {
        public override void Evaluate(AegisValidationContext context, IAegisFindingSink sink)
        {
            string[] prefabs = FindPrefabPaths(context);
            for (int i = 0; i < prefabs.Length; i++)
            {
                if (context.IsCancellationRequested)
                    return;

                EvaluatePrefab(prefabs[i], sink);
            }

            string[] scenes = FindScenePaths(context);
            for (int i = 0; i < scenes.Length; i++)
            {
                if (context.IsCancellationRequested)
                    return;

                EvaluateScene(scenes[i], sink);
            }
        }

        private void EvaluatePrefab(string path, IAegisFindingSink sink)
        {
            VisitPrefabGameObjects(path, go => AddIfMissing(path, go, sink));
        }

        private void EvaluateScene(string path, IAegisFindingSink sink)
        {
            VisitSceneGameObjects(path, go => AddIfMissing(path, go, sink));
        }

        private void AddIfMissing(string path, GameObject go, IAegisFindingSink sink)
        {
            int count = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(go);
            if (count <= 0)
                return;

            sink.Add(CreateFinding(
                $"{count} missing MonoBehaviour script(s) on '{go.name}'.",
                assetPath: path,
                globalObjectId: AegisObjectId.TryGet(go),
                code: "Aegis.MissingMonoScript"));
        }
    }
}
