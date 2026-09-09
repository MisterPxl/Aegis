using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using global::Astra.Valkyrie.Editor;

namespace Astra.Aegis.Integrations.Valkyrie
{
    /// <summary>
    /// Shows Aegis findings inside the Valkyrie inspector: a summary line with provenance and age,
    /// a help box under each field that has findings, unattached findings at the bottom with a way
    /// to locate their origin, and a targeted validation of the inspected asset or scene. Uses the
    /// Valkyrie composition point only; no second global inspector, no project scan on repaint.
    /// </summary>
    public sealed class AegisValkyrieInspectorExtension : IValkyrieInspectorExtension
    {
        public const string EnabledPreference = "Astra.Aegis.Integrations.Valkyrie.InspectorFindings";
        private static AegisValkyrieInspectorExtension _instance;
        private static IDisposable _registration;
        private readonly List<AegisFinding> _scratch = new List<AegisFinding>();
        private readonly HashSet<string> _drawnPaths = new HashSet<string>(StringComparer.Ordinal);
        private string _lastMessage;

        public int Order => 100;

        public static bool Enabled
        {
            get => EditorPrefs.GetBool(EnabledPreference, true);
            set => EditorPrefs.SetBool(EnabledPreference, value);
        }

        public static bool IsRegistered => _registration != null && ValkyrieInspectorExtensions.IsRegistered(_instance);

        [InitializeOnLoadMethod]
        public static void Register()
        {
            if (IsRegistered) return;
            AegisValkyrieInspectorCache.EnsureHooks();
            _instance = _instance ?? new AegisValkyrieInspectorExtension();
            _registration = ValkyrieInspectorExtensions.Register(_instance);
        }

        public static void Unregister()
        {
            _registration?.Dispose();
            _registration = null;
        }

        public void OnBeginInspector(ValkyrieInspectorContext context)
        {
            _drawnPaths.Clear();
            if (!Enabled || context.Target == null) return;
            bool known = AegisValkyrieInspectorCache.TryGet(context.Target, out AegisInspectorFindings findings);
            using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
            {
                GUILayout.Label(Summarize(known ? findings : null), EditorStyles.miniLabel, GUILayout.ExpandWidth(true));
                if (GUILayout.Button("Validate", EditorStyles.miniButton, GUILayout.Width(60f)))
                {
                    AegisValkyrieInspectorCache.RunTargeted(context.Targets, out _lastMessage);
                    GUIUtility.ExitGUI();
                }
                if (GUILayout.Button("Dashboard", EditorStyles.miniButton, GUILayout.Width(72f)))
                    EditorApplication.ExecuteMenuItem("Tools/Astra/Aegis/Project Health");
            }
            if (!string.IsNullOrEmpty(_lastMessage) && !known)
                EditorGUILayout.HelpBox(_lastMessage, MessageType.None);
        }

        public void OnAfterField(ValkyrieInspectorContext context, SerializedProperty property, InspectedField field)
        {
            if (!Enabled || property == null || context.Target == null) return;
            _drawnPaths.Add(property.propertyPath);
            if (!AegisValkyrieInspectorCache.TryGet(context.Target, out AegisInspectorFindings findings) || findings.Findings.Count == 0) return;
            _scratch.Clear();
            findings.CollectForProperty(property.propertyPath, _scratch);
            for (int i = 0; i < _scratch.Count; i++)
                EditorGUILayout.HelpBox(_scratch[i].Message, ToMessageType(_scratch[i].Severity));
        }

        public void OnEndInspector(ValkyrieInspectorContext context)
        {
            if (!Enabled || context.Target == null) return;
            if (!AegisValkyrieInspectorCache.TryGet(context.Target, out AegisInspectorFindings findings) || findings.Findings.Count == 0) return;
            _scratch.Clear();
            findings.CollectUnattached(_drawnPaths, _scratch);
            for (int i = 0; i < _scratch.Count; i++)
            {
                AegisFinding finding = _scratch[i];
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.HelpBox(finding.Message + (string.IsNullOrEmpty(finding.PropertyPath) ? string.Empty : "\n" + finding.PropertyPath), ToMessageType(finding.Severity));
                    if (GUILayout.Button("Locate", GUILayout.Width(52f), GUILayout.Height(28f)))
                        Locate(finding);
                }
            }
        }

        public static string Summarize(AegisInspectorFindings findings)
        {
            if (findings == null) return "Aegis: no report for this object yet.";
            string provenance = (findings.Source == "targeted" ? "validated " : "report ") + AegisInspectorFindings.FormatAge(findings.CapturedUtc, DateTime.UtcNow);
            if (findings.Findings.Count == 0) return "Aegis: no findings (" + provenance + ").";
            var parts = new List<string>();
            if (findings.ErrorCount > 0) parts.Add(findings.ErrorCount + (findings.ErrorCount == 1 ? " error" : " errors"));
            if (findings.WarningCount > 0) parts.Add(findings.WarningCount + (findings.WarningCount == 1 ? " warning" : " warnings"));
            if (findings.InfoCount > 0) parts.Add(findings.InfoCount + " info");
            return "Aegis: " + string.Join(", ", parts) + " (" + provenance + ").";
        }

        private static MessageType ToMessageType(AegisSeverity severity)
        {
            switch (severity)
            {
                case AegisSeverity.Error: return MessageType.Error;
                case AegisSeverity.Warning: return MessageType.Warning;
                default: return MessageType.Info;
            }
        }

        private static void Locate(AegisFinding finding)
        {
            if (finding == null) return;
            if (!string.IsNullOrEmpty(finding.GlobalObjectId) && GlobalObjectId.TryParse(finding.GlobalObjectId, out GlobalObjectId id))
            {
                UnityEngine.Object obj = GlobalObjectId.GlobalObjectIdentifierToObjectSlow(id);
                if (obj != null) { Selection.activeObject = obj; EditorGUIUtility.PingObject(obj); return; }
            }
            if (string.IsNullOrEmpty(finding.AssetPath)) return;
            UnityEngine.Object asset = AssetDatabase.LoadMainAssetAtPath(finding.AssetPath);
            if (asset != null) { Selection.activeObject = asset; EditorGUIUtility.PingObject(asset); }
        }
    }
}
