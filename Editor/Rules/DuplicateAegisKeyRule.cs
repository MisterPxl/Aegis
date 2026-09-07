using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace MisterPxl.Aegis
{
    [CreateAssetMenu(fileName = "DuplicateAegisKeyRule", menuName = "Astra/Aegis/Rules/Duplicate Aegis Key")]
    public sealed class DuplicateAegisKeyRule : AegisRuleAsset
    {
        public override void Evaluate(AegisValidationContext context, IAegisFindingSink sink)
        {
            Dictionary<string, string> firstPathByKey = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            string[] paths = context.FindAssetPaths("t:ScriptableObject", "Assets", "Packages");
            for (int i = 0; i < paths.Length; i++)
            {
                if (context.IsCancellationRequested)
                    return;

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
