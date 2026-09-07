using System;
using UnityEditor;
using UnityEngine;

namespace MisterPxl.Aegis
{
    [CreateAssetMenu(fileName = "PrefabIntegrityRule", menuName = "Aegis/Rules/Prefab Integrity")]
    public sealed class PrefabIntegrityRule : AegisProjectAssetRule
    {
        public override void Evaluate(AegisValidationContext context, IAegisFindingSink sink)
        {
            string[] paths = FindPrefabPaths(context);
            for (int i = 0; i < paths.Length; i++)
            {
                if (context.IsCancellationRequested)
                    return;

                EvaluatePrefab(paths[i], sink);
            }
        }

        private void EvaluatePrefab(string path, IAegisFindingSink sink)
        {
            GameObject root = null;
            try
            {
                GameObject asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (asset == null)
                {
                    sink.Add(CreateFinding($"Prefab '{path}' cannot be loaded.", assetPath: path, code: "Aegis.Prefab.LoadFailed"));
                    return;
                }

                if (PrefabUtility.GetPrefabAssetType(asset) == PrefabAssetType.MissingAsset)
                {
                    sink.Add(CreateFinding($"Prefab '{path}' has a missing source asset.", assetPath: path, code: "Aegis.Prefab.MissingSource"));
                    return;
                }

                root = PrefabUtility.LoadPrefabContents(path);
                foreach (GameObject go in EnumerateHierarchy(root))
                {
                    if (PrefabUtility.GetPrefabInstanceStatus(go) == PrefabInstanceStatus.MissingAsset)
                    {
                        sink.Add(CreateFinding(
                            $"Nested prefab instance '{go.name}' is disconnected.",
                            assetPath: path,
                            globalObjectId: AegisObjectId.TryGet(go),
                            code: "Aegis.Prefab.DisconnectedNested"));
                    }
                }
            }
            catch (Exception ex)
            {
                sink.Add(CreateFinding($"Prefab '{path}' failed integrity inspection.", ex.Message, path, code: "Aegis.Prefab.Exception"));
            }
            finally
            {
                if (root != null)
                    PrefabUtility.UnloadPrefabContents(root);
            }
        }
    }
}
