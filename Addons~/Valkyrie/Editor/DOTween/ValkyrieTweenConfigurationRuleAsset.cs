using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using global::Astra.Valkyrie.Integrations.DOTween;
using global::Astra.Valkyrie.Integrations.DOTween.Editor;

namespace Astra.Aegis.Integrations.Valkyrie.DOTween
{
    /// <summary>
    /// Reports Valkyrie DOTween sequences that cannot play as configured: unresolved or
    /// mistyped target bindings, invalid step values, empty sequences and step event
    /// bindings that reference steps which no longer exist. Validation reuses the
    /// integration's own editor validation and never builds or plays a tween. A binding
    /// declared without a target is reported separately as runtime-provided, so projects
    /// that bind targets from code are not treated as broken.
    /// </summary>
    [CreateAssetMenu(fileName = "ValkyrieTweenConfigurationRule", menuName = "Astra/Aegis/Valkyrie/Tween Configuration Rule")]
    [AegisRuleIdentity("Astra.Aegis.Integrations.Valkyrie.DOTween.TweenConfigurationRule")]
    public sealed class ValkyrieTweenConfigurationRuleAsset : AegisProjectAssetRule
    {
        public const string CodePrefix = "Aegis.Valkyrie.DOTween.";
        public const string RuntimeBindingCode = CodePrefix + "RuntimeBinding";
        public const string StaleStepEventCode = CodePrefix + "StaleStepEvent";

        [Tooltip("Also validate TweenSequenceAsset step definitions on their own, without player bindings.")]
        [SerializeField] private bool _validateSequenceAssets = true;
        [Tooltip("Severity for bindings declared on a player without a target; such targets are usually provided at runtime.")]
        [SerializeField] private AegisSeverity _runtimeBindingSeverity = AegisSeverity.Info;
        [Tooltip("Report DOTween integration warnings in addition to errors.")]
        [SerializeField] private bool _reportWarnings = true;

        public override void Evaluate(AegisValidationContext context, IAegisFindingSink sink)
        {
            EvaluateProjectObjects(context, sink, EvaluateObject, "Assets", "Packages");
        }

        private void EvaluateObject(string path, Object obj, IAegisFindingSink sink)
        {
            if (obj is TweenPlayer player)
                EvaluatePlayer(path, player, sink);
            else if (_validateSequenceAssets && obj is TweenSequenceAsset asset)
                EvaluateAsset(path, asset, sink);
        }

        private void EvaluatePlayer(string path, TweenPlayer player, IAegisFindingSink sink)
        {
            string objectId = AegisObjectId.TryGet(player);
            bool assetMode = player.SourceMode == TweenPlayerSourceMode.Asset;
            IReadOnlyList<TweenBuildDiagnostic> diagnostics = TweenSequenceEditorValidation.Validate(player);
            for (int i = 0; i < diagnostics.Count; i++)
            {
                TweenBuildDiagnostic diagnostic = diagnostics[i];
                if (IsDefinitionDiagnostic(diagnostic.Code))
                {
                    // Step definitions living in a TweenSequenceAsset are reported on that asset.
                    if (assetMode && _validateSequenceAssets)
                        continue;
                    Report(sink, path, objectId, diagnostic, StepPath("_timeline", diagnostic.StepIndex));
                    continue;
                }

                int bindingIndex = FindBindingIndex(player, diagnostic.BindingKey);
                if (diagnostic.Code == TweenDiagnosticCode.MissingBinding && bindingIndex >= 0 && player.Bindings[bindingIndex].Target == null)
                {
                    sink.Add(CreateFinding(
                        "Binding '" + diagnostic.BindingKey + "' has no target in this " + (assetMode ? "asset-driven " : string.Empty) +
                        "player; it must be provided at runtime before the sequence plays.",
                        details: diagnostic.Message,
                        assetPath: path,
                        globalObjectId: objectId,
                        propertyPath: BindingTargetPath(bindingIndex),
                        code: RuntimeBindingCode,
                        severity: _runtimeBindingSeverity));
                    continue;
                }

                string propertyPath = bindingIndex >= 0 ? BindingTargetPath(bindingIndex)
                    : !string.IsNullOrEmpty(diagnostic.BindingKey) ? "_bindings"
                    : diagnostic.Code == TweenDiagnosticCode.MissingAsset ? "_asset"
                    : diagnostic.StepIndex >= 0 ? StepPath(assetMode ? "_asset" : "_timeline", assetMode ? -1 : diagnostic.StepIndex)
                    : string.Empty;
                Report(sink, path, objectId, diagnostic, propertyPath);
            }

            EvaluateStepEvents(path, player, objectId, sink);
        }

        private void EvaluateStepEvents(string path, TweenPlayer player, string objectId, IAegisFindingSink sink)
        {
            IList<TweenStepDefinition> steps = player.EffectiveSteps;
            IList<TweenStepEventBinding> events = player.StepEvents;
            for (int i = 0; i < events.Count; i++)
            {
                TweenStepEventBinding binding = events[i];
                if (binding == null || string.IsNullOrEmpty(binding.StepId))
                    continue;
                bool found = false;
                for (int s = 0; s < steps.Count && !found; s++)
                    found = steps[s] != null && steps[s].Id == binding.StepId;
                if (found)
                    continue;
                sink.Add(CreateFinding(
                    "Step event binding " + i + " references step '" + binding.StepId + "', which no longer exists in the effective sequence.",
                    details: "Remove the binding or point it at an existing step; its callbacks will never fire.",
                    assetPath: path,
                    globalObjectId: objectId,
                    propertyPath: "_stepEvents.Array.data[" + i + "]._stepId",
                    code: StaleStepEventCode,
                    severity: AegisSeverity.Warning));
            }
        }

        private void EvaluateAsset(string path, TweenSequenceAsset asset, IAegisFindingSink sink)
        {
            string objectId = AegisObjectId.TryGet(asset);
            IReadOnlyList<TweenBuildDiagnostic> diagnostics = TweenSequenceEditorValidation.ValidateAsset(asset);
            for (int i = 0; i < diagnostics.Count; i++)
                Report(sink, path, objectId, diagnostics[i], StepPath("_timeline", diagnostics[i].StepIndex));
        }

        private void Report(IAegisFindingSink sink, string path, string objectId, TweenBuildDiagnostic diagnostic, string propertyPath)
        {
            AegisSeverity severity;
            switch (diagnostic.Severity)
            {
                case TweenDiagnosticSeverity.Error: severity = AegisSeverity.Error; break;
                case TweenDiagnosticSeverity.Warning: severity = AegisSeverity.Warning; break;
                default: severity = AegisSeverity.Info; break;
            }
            if (severity != AegisSeverity.Error && !_reportWarnings)
                return;
            string details = string.IsNullOrEmpty(diagnostic.StepType) ? null
                : "Step " + diagnostic.StepIndex + " (" + diagnostic.StepType + ")" +
                  (string.IsNullOrEmpty(diagnostic.ExpectedType) ? string.Empty : ", expected " + diagnostic.ExpectedType) +
                  (string.IsNullOrEmpty(diagnostic.ActualType) ? string.Empty : ", found " + diagnostic.ActualType) + ".";
            sink.Add(CreateFinding(
                diagnostic.Message,
                details: details,
                assetPath: path,
                globalObjectId: objectId,
                propertyPath: propertyPath,
                code: CodePrefix + diagnostic.Code,
                severity: severity));
        }

        private static bool IsDefinitionDiagnostic(TweenDiagnosticCode code)
        {
            return code == TweenDiagnosticCode.EmptySequence || code == TweenDiagnosticCode.NullStep
                || code == TweenDiagnosticCode.InvalidValue || code == TweenDiagnosticCode.UnsupportedInAsset;
        }

        private static int FindBindingIndex(TweenPlayer player, string key)
        {
            if (string.IsNullOrEmpty(key))
                return -1;
            IList<TweenTargetBinding> bindings = player.Bindings;
            for (int i = 0; i < bindings.Count; i++)
                if (bindings[i] != null && string.Equals(bindings[i].Key?.Trim(), key, System.StringComparison.Ordinal))
                    return i;
            return -1;
        }

        private static string BindingTargetPath(int index) => "_bindings.Array.data[" + index + "]._target";

        private static string StepPath(string root, int stepIndex) => stepIndex >= 0 ? root + "._steps.Array.data[" + stepIndex + "]" : root;
    }
}
