using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

#pragma warning disable 0618
namespace MisterPxl.Aegis
{
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

        private static void EvaluateSceneObjects(string path, IAegisFindingSink sink, AegisProjectObjectEvaluator evaluator)
        {
            Scene scene = default;
            try
            {
                scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
                GameObject[] roots = scene.GetRootGameObjects();
                for (int i = 0; i < roots.Length; i++)
                {
                    foreach (GameObject go in EnumerateHierarchy(roots[i]))
                        EvaluateGameObjectAndComponents(path, go, sink, evaluator);
                }
            }
            finally
            {
                if (scene.IsValid())
                    EditorSceneManager.CloseScene(scene, true);
            }
        }

        private static void EvaluatePrefabObjects(string path, IAegisFindingSink sink, AegisProjectObjectEvaluator evaluator)
        {
            GameObject root = null;
            try
            {
                root = PrefabUtility.LoadPrefabContents(path);
                foreach (GameObject go in EnumerateHierarchy(root))
                    EvaluateGameObjectAndComponents(path, go, sink, evaluator);
            }
            finally
            {
                if (root != null)
                    PrefabUtility.UnloadPrefabContents(root);
            }
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

    [CreateAssetMenu(fileName = "MissingMonoScriptRule", menuName = "Aegis/Rules/Missing Mono Script")]
    public sealed class MissingMonoScriptRule : AegisProjectAssetRule
    {
        public override void Evaluate(AegisValidationContext context, IAegisFindingSink sink)
        {
            string[] prefabs = FindPrefabPaths(context);
            for (int i = 0; i < prefabs.Length; i++)
                EvaluatePrefab(prefabs[i], sink);

            string[] scenes = FindScenePaths(context);
            for (int i = 0; i < scenes.Length; i++)
                EvaluateScene(scenes[i], sink);
        }

        private void EvaluatePrefab(string path, IAegisFindingSink sink)
        {
            GameObject root = null;
            try
            {
                root = PrefabUtility.LoadPrefabContents(path);
                foreach (GameObject go in EnumerateHierarchy(root))
                    AddIfMissing(path, go, sink);
            }
            finally
            {
                if (root != null)
                    PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private void EvaluateScene(string path, IAegisFindingSink sink)
        {
            Scene scene = default;
            try
            {
                scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
                GameObject[] roots = scene.GetRootGameObjects();
                for (int i = 0; i < roots.Length; i++)
                {
                    foreach (GameObject go in EnumerateHierarchy(roots[i]))
                        AddIfMissing(path, go, sink);
                }
            }
            finally
            {
                if (scene.IsValid())
                    EditorSceneManager.CloseScene(scene, true);
            }
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

    [CreateAssetMenu(fileName = "MissingObjectReferenceRule", menuName = "Aegis/Rules/Missing Object Reference")]
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

            SerializedProperty iterator = serializedObject.GetIterator();
            bool enterChildren = true;
            while (iterator.NextVisible(enterChildren))
            {
                enterChildren = false;
                if (iterator.propertyType != SerializedPropertyType.ObjectReference)
                    continue;

                if (iterator.objectReferenceValue == null && iterator.objectReferenceInstanceIDValue != 0)
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

            SerializedProperty iterator = serializedObject.GetIterator();
            bool enterChildren = true;
            while (iterator.NextVisible(enterChildren))
            {
                enterChildren = true;
                if (!iterator.isArray || iterator.propertyType == SerializedPropertyType.String)
                    continue;

                for (int index = 0; index < iterator.arraySize; index++)
                {
                    SerializedProperty element = iterator.GetArrayElementAtIndex(index);
                    if (element == null || element.propertyType != SerializedPropertyType.ObjectReference)
                        continue;

                    if (element.objectReferenceValue == null && element.objectReferenceInstanceIDValue == 0)
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

    [CreateAssetMenu(fileName = "BuildSceneRule", menuName = "Aegis/Rules/Build Scene")]
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

    [CreateAssetMenu(fileName = "PrefabIntegrityRule", menuName = "Aegis/Rules/Prefab Integrity")]
    public sealed class PrefabIntegrityRule : AegisProjectAssetRule
    {
        public override void Evaluate(AegisValidationContext context, IAegisFindingSink sink)
        {
            string[] paths = FindPrefabPaths(context);
            for (int i = 0; i < paths.Length; i++)
                EvaluatePrefab(paths[i], sink);
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

    [CreateAssetMenu(fileName = "DuplicateAegisKeyRule", menuName = "Aegis/Rules/Duplicate Aegis Key")]
    public sealed class DuplicateAegisKeyRule : AegisRuleAsset
    {
        public override void Evaluate(AegisValidationContext context, IAegisFindingSink sink)
        {
            Dictionary<string, string> firstPathByKey = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            string[] paths = context.FindAssetPaths("t:ScriptableObject", "Assets", "Packages");
            for (int i = 0; i < paths.Length; i++)
            {
                UnityEngine.Object[] assets = AssetDatabase.LoadAllAssetsAtPath(paths[i]);
                for (int assetIndex = 0; assetIndex < assets.Length; assetIndex++)
                {
                    IAegisKeyProvider provider = assets[assetIndex] as IAegisKeyProvider;
                    if (provider == null)
                        continue;

                    string key = provider.AegisKey;
                    if (string.IsNullOrWhiteSpace(key))
                        continue;

                    if (firstPathByKey.TryGetValue(key, out string firstPath))
                    {
                        sink.Add(CreateFinding(
                            $"Duplicate Aegis key '{key}'.",
                            $"First occurrence: {firstPath}",
                            paths[i],
                            AegisObjectId.TryGet(assets[assetIndex]),
                            code: "Aegis.DuplicateKey",
                            severity: AegisSeverity.Warning));
                    }
                    else
                    {
                        firstPathByKey.Add(key, paths[i]);
                    }
                }
            }
        }
    }
}
#pragma warning restore 0618
