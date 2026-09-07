using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Astra.Aegis
{
    [UnityEngine.Scripting.APIUpdating.MovedFrom(false, "MisterPxl.Aegis", "Aegis.Editor", "AegisProjectAssetRule")]
    public abstract class AegisProjectAssetRule : AegisRuleAsset
    {
        protected delegate void AegisProjectObjectEvaluator(string path, UnityEngine.Object obj, IAegisFindingSink sink);

        protected static string[] FindPrefabPaths(AegisValidationContext context)
        {
            return context.FindAssetPaths("t:Prefab", "Assets", "Packages");
        }

        protected static string[] FindScenePaths(AegisValidationContext context)
        {
            return context.FindAssetPaths("t:Scene", "Assets");
        }

        protected static IEnumerable<GameObject> EnumerateHierarchy(GameObject root)
        {
            if (root == null)
                yield break;

            Stack<Transform> stack = new Stack<Transform>();
            stack.Push(root.transform);
            while (stack.Count > 0)
            {
                Transform current = stack.Pop();
                yield return current.gameObject;
                for (int i = current.childCount - 1; i >= 0; i--)
                    stack.Push(current.GetChild(i));
            }
        }

        protected static void EvaluateProjectObjects(
            AegisValidationContext context,
            IAegisFindingSink sink,
            AegisProjectObjectEvaluator evaluator,
            params string[] folders)
        {
            string[] paths = context.FindAssetPaths("t:Object", folders);
            for (int i = 0; i < paths.Length; i++)
            {
                if (context.IsCancellationRequested)
                    return;

                EvaluateObjectsAtPath(paths[i], sink, evaluator);
            }
        }

        private static void EvaluateObjectsAtPath(string path, IAegisFindingSink sink, AegisProjectObjectEvaluator evaluator)
        {
            if (AssetDatabase.IsValidFolder(path))
                return;

            if (IsScenePath(path))
            {
                EvaluateSceneObjects(path, sink, evaluator);
                return;
            }

            if (IsPrefabPath(path))
            {
                EvaluatePrefabObjects(path, sink, evaluator);
                return;
            }

            UnityEngine.Object[] assets = AssetDatabase.LoadAllAssetsAtPath(path);
            for (int assetIndex = 0; assetIndex < assets.Length; assetIndex++)
                evaluator(path, assets[assetIndex], sink);
        }

        protected static void VisitSceneGameObjects(string path, Action<GameObject> visitor)
        {
            Scene existingScene = SceneManager.GetSceneByPath(path);
            bool wasLoaded = existingScene.IsValid() && existingScene.isLoaded;
            bool wasInHierarchy = existingScene.IsValid();
            Scene scene = wasLoaded ? existingScene : EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
            try
            {
                GameObject[] roots = scene.GetRootGameObjects();
                for (int i = 0; i < roots.Length; i++)
                {
                    foreach (GameObject go in EnumerateHierarchy(roots[i]))
                        visitor(go);
                }
            }
            finally
            {
                // Never close a scene the user already had loaded, and keep pre-existing
                // hierarchy entries (valid but unloaded scenes) in the hierarchy.
                if (!wasLoaded && scene.IsValid())
                    EditorSceneManager.CloseScene(scene, !wasInHierarchy);
            }
        }

        protected static void VisitPrefabGameObjects(string path, Action<GameObject> visitor)
        {
            GameObject root = null;
            try
            {
                root = PrefabUtility.LoadPrefabContents(path);
                foreach (GameObject go in EnumerateHierarchy(root))
                    visitor(go);
            }
            finally
            {
                if (root != null)
                    PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void EvaluateSceneObjects(string path, IAegisFindingSink sink, AegisProjectObjectEvaluator evaluator)
        {
            VisitSceneGameObjects(path, go => EvaluateGameObjectAndComponents(path, go, sink, evaluator));
        }

        private static void EvaluatePrefabObjects(string path, IAegisFindingSink sink, AegisProjectObjectEvaluator evaluator)
        {
            VisitPrefabGameObjects(path, go => EvaluateGameObjectAndComponents(path, go, sink, evaluator));
        }

        private static void EvaluateGameObjectAndComponents(
            string path,
            GameObject go,
            IAegisFindingSink sink,
            AegisProjectObjectEvaluator evaluator)
        {
            evaluator(path, go, sink);

            Component[] components = go.GetComponents<Component>();
            for (int i = 0; i < components.Length; i++)
                evaluator(path, components[i], sink);
        }

        private static bool IsScenePath(string path)
        {
            return path.EndsWith(".unity", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsPrefabPath(string path)
        {
            return path.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase);
        }
    }
}
