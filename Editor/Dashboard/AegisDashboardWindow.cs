using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace MisterPxl.Aegis
{
    public sealed class AegisDashboardWindow : EditorWindow
    {
        private readonly List<AegisFinding> _filteredFindings = new List<AegisFinding>();
        private AegisValidationReport _report;
        private AegisInteractiveRun _run;
        private Label _summary;
        private Label _details;
        private TextField _search;
        private EnumField _severity;
        private ListView _list;
        private AegisFinding _selectedFinding;

        [MenuItem("Tools/Aegis/Project Health")]
        public static void Open()
        {
            AegisDashboardWindow window = GetWindow<AegisDashboardWindow>();
            window.titleContent = new GUIContent("Aegis Project Health");
            window.minSize = new Vector2(780f, 480f);
            window.Show();
        }

        [MenuItem("Tools/Aegis/Run Validation")]
        public static void RunFromMenu()
        {
            AegisDashboardWindow window = GetWindow<AegisDashboardWindow>();
            window.Show();
            window.StartRun();
        }

        [MenuItem("Tools/Aegis/Diagnostics/List Rules")]
        public static void ListRules()
        {
            List<AegisRuleAsset> rules = AegisRuleDiscovery.DiscoverRules();
            for (int i = 0; i < rules.Count; i++)
            {
                AegisRuleAsset rule = rules[i];
                Debug.Log($"Aegis rule: {rule.DisplayName} | {rule.Category} | {rule.RuleId} | {AssetDatabase.GetAssetPath(rule)}");
            }
        }

        private void OnEnable()
        {
            _report = AegisReportStore.LoadLastReport();
            BuildUi();
            RefreshFilter();
        }

        private void OnDisable()
        {
            if (_run != null && _run.IsRunning)
                _run.Cancel();
        }

        private void BuildUi()
        {
            rootVisualElement.Clear();
            rootVisualElement.style.paddingLeft = 8;
            rootVisualElement.style.paddingRight = 8;
            rootVisualElement.style.paddingTop = 8;
            rootVisualElement.style.paddingBottom = 8;

            VisualElement toolbar = new VisualElement();
            toolbar.style.flexDirection = FlexDirection.Row;
            toolbar.style.marginBottom = 6;
            rootVisualElement.Add(toolbar);

            Button runButton = new Button(StartRun) { text = "Run" };
            Button cancelButton = new Button(CancelRun) { text = "Cancel" };
            Button selectedButton = new Button(RunSelectedRule) { text = "Run Selected Rule" };
            Button exportButton = new Button(ExportJson) { text = "Export JSON" };
            Button pingButton = new Button(() => PingFinding(_selectedFinding)) { text = "Ping" };
            Button fixButton = new Button(FixSelected) { text = "Fix Selected" };
            Button suppressButton = new Button(SuppressSelected) { text = "Suppress Selected" };
            Button fixSafeButton = new Button(FixAllSafe) { text = "Fix All Safe" };
            toolbar.Add(runButton);
            toolbar.Add(cancelButton);
            toolbar.Add(selectedButton);
            toolbar.Add(exportButton);
            toolbar.Add(pingButton);
            toolbar.Add(fixButton);
            toolbar.Add(suppressButton);
            toolbar.Add(fixSafeButton);

            _summary = new Label();
            _summary.style.unityFontStyleAndWeight = FontStyle.Bold;
            _summary.style.marginBottom = 6;
            rootVisualElement.Add(_summary);

            VisualElement filters = new VisualElement();
            filters.style.flexDirection = FlexDirection.Row;
            filters.style.marginBottom = 6;
            rootVisualElement.Add(filters);

            _search = new TextField("Search");
            _search.style.flexGrow = 1f;
            _search.RegisterValueChangedCallback(_ => RefreshFilter());
            filters.Add(_search);

            _severity = new EnumField("Minimum Severity", AegisSeverity.Info);
            _severity.RegisterValueChangedCallback(_ => RefreshFilter());
            filters.Add(_severity);

            VisualElement body = new VisualElement();
            body.style.flexDirection = FlexDirection.Row;
            body.style.flexGrow = 1f;
            rootVisualElement.Add(body);

            _list = new ListView(_filteredFindings, 36, MakeFindingRow, BindFindingRow)
            {
                selectionType = SelectionType.Single,
                style =
                {
                    flexGrow = 2f,
                    marginRight = 8
                }
            };
            _list.selectionChanged += OnSelectionChanged;
            _list.itemsChosen += OnItemsChosen;
            body.Add(_list);

            _details = new Label("Run Aegis to inspect project health.");
            _details.style.whiteSpace = WhiteSpace.Normal;
            _details.style.flexGrow = 1f;
            _details.style.minWidth = 260;
            body.Add(_details);

            UpdateSummary();
        }

        private VisualElement MakeFindingRow()
        {
            Label label = new Label();
            label.style.whiteSpace = WhiteSpace.NoWrap;
            return label;
        }

        private void BindFindingRow(VisualElement element, int index)
        {
            Label label = (Label)element;
            if (index < 0 || index >= _filteredFindings.Count)
            {
                label.text = string.Empty;
                return;
            }

            AegisFinding finding = _filteredFindings[index];
            label.text = $"{finding.Severity} | {finding.RuleName} | {finding.Message} | {finding.AssetPath}";
        }

        private void StartRun()
        {
            if (_run != null && _run.IsRunning)
                return;

            AegisValidationProfile profile = AegisSettings.instance.GetProfile("Interactive");
            _run = new AegisInteractiveRun(profile, result =>
            {
                _report = result.Report;
                RefreshFilter();
                Repaint();
            });
            _run.Start();
            _summary.text = "Aegis validation running...";
        }

        private void CancelRun()
        {
            if (_run != null && _run.IsRunning)
                _run.Cancel();
        }

        private void RunSelectedRule()
        {
            if (_selectedFinding == null)
                return;

            List<AegisRuleAsset> rules = AegisRuleDiscovery.DiscoverRules();
            AegisRuleAsset selectedRule = null;
            for (int i = 0; i < rules.Count; i++)
            {
                if (string.Equals(rules[i].RuleId, _selectedFinding.RuleId, StringComparison.OrdinalIgnoreCase))
                {
                    selectedRule = rules[i];
                    break;
                }
            }

            if (selectedRule == null)
                return;

            List<AegisFinding> findings = new List<AegisFinding>();
            List<AegisRuleExecutionRecord> records = new List<AegisRuleExecutionRecord>();
            AegisValidationContext context = new AegisValidationContext(AegisSettings.instance.GetProfile("Interactive"));
            AegisRunner.ExecuteRule(selectedRule, context, findings, records);
            _report = new AegisValidationReport(context.Profile.Name, 0d, findings, records);
            AegisReportStore.SaveLastReport(_report);
            RefreshFilter();
        }

        private void ExportJson()
        {
            if (_report == null)
                return;

            string path = EditorUtility.SaveFilePanel("Export Aegis JSON", "Library/Aegis", "aegis-report.json", "json");
            if (string.IsNullOrWhiteSpace(path))
                return;

            AegisReportWriters.WriteJson(_report, path);
            EditorUtility.RevealInFinder(path);
        }

        private void FixAllSafe()
        {
            if (_report == null)
                return;

            int fixedCount = 0;
            for (int i = 0; i < _report.Findings.Count; i++)
            {
                AegisFinding finding = _report.Findings[i];
                if (finding.FixAction == null || finding.FixAction.Safety != AegisFixSafety.Safe)
                    continue;

                AegisValidationContext context = new AegisValidationContext(AegisSettings.instance.GetProfile("Interactive"));
                AegisFixResult result = finding.FixAction.Execute(finding, context);
                if (result.Success)
                    fixedCount++;
            }

            if (fixedCount > 0)
            {
                AssetDatabase.SaveAssets();
                StartRun();
            }
        }

        private void FixSelected()
        {
            if (_selectedFinding == null || _selectedFinding.FixAction == null)
                return;

            bool canRun = _selectedFinding.FixAction.Safety == AegisFixSafety.Safe
                || EditorUtility.DisplayDialog(
                    "Apply Aegis Fix",
                    $"This fix is marked {_selectedFinding.FixAction.Safety}. Apply it?",
                    "Apply",
                    "Cancel");
            if (!canRun)
                return;

            AegisValidationContext context = new AegisValidationContext(AegisSettings.instance.GetProfile("Interactive"));
            AegisFixResult result = _selectedFinding.FixAction.Execute(_selectedFinding, context);
            if (!result.Success)
            {
                EditorUtility.DisplayDialog("Aegis Fix Failed", result.Message, "OK");
                return;
            }

            AssetDatabase.SaveAssets();
            RunSelectedRule();
        }

        private void SuppressSelected()
        {
            if (_selectedFinding == null)
                return;

            bool confirmed = EditorUtility.DisplayDialog(
                "Suppress Aegis Finding",
                "Suppress this finding fingerprint for the current project? Add a detailed reason in ProjectSettings/AegisSettings.asset if this suppression is kept.",
                "Suppress",
                "Cancel");
            if (!confirmed)
                return;

            AegisSettings.instance.AddSuppression(
                _selectedFinding.Fingerprint,
                "Suppressed from Aegis dashboard.",
                Environment.UserName);
            RefreshFilter();
        }

        private void RefreshFilter()
        {
            _filteredFindings.Clear();
            if (_report != null)
            {
                string search = _search != null ? _search.value : string.Empty;
                AegisSeverity minSeverity = _severity != null ? (AegisSeverity)_severity.value : AegisSeverity.Info;
                for (int i = 0; i < _report.Findings.Count; i++)
                {
                    AegisFinding finding = _report.Findings[i];
                    if (finding.Severity < minSeverity)
                        continue;

                    if (!MatchesSearch(finding, search))
                        continue;

                    _filteredFindings.Add(finding);
                }
            }

            _list?.RefreshItems();
            UpdateSummary();
        }

        private static bool MatchesSearch(AegisFinding finding, string search)
        {
            if (finding == null)
                return false;

            if (string.IsNullOrWhiteSpace(search))
                return true;

            return Contains(finding.Message, search)
                || Contains(finding.AssetPath, search)
                || Contains(finding.RuleName, search)
                || Contains(finding.Code, search);
        }

        private static bool Contains(string value, string search)
        {
            return !string.IsNullOrEmpty(value) && value.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private void UpdateSummary()
        {
            if (_summary == null)
                return;

            if (_report == null)
            {
                _summary.text = "No Aegis report yet.";
                return;
            }

            _summary.text = AegisReportWriters.FormatSummary(_report);
        }

        private void OnSelectionChanged(IEnumerable<object> selection)
        {
            _selectedFinding = null;
            foreach (object item in selection)
            {
                _selectedFinding = item as AegisFinding;
                break;
            }

            UpdateDetails();
        }

        private void OnItemsChosen(IEnumerable<object> chosen)
        {
            foreach (object item in chosen)
            {
                AegisFinding finding = item as AegisFinding;
                if (finding != null)
                    PingFinding(finding);
                break;
            }
        }

        private void UpdateDetails()
        {
            if (_details == null)
                return;

            if (_selectedFinding == null)
            {
                _details.text = _filteredFindings.Count == 0 ? "No findings for the current filter." : "Select a finding to inspect it.";
                return;
            }

            AegisFinding finding = _selectedFinding;
            _details.text =
                $"Rule: {finding.RuleName}\n" +
                $"Severity: {finding.Severity}\n" +
                $"Code: {finding.Code}\n" +
                $"Asset: {finding.AssetPath}\n" +
                $"Property: {finding.PropertyPath}\n" +
                $"Fingerprint: {finding.Fingerprint}\n\n" +
                $"{finding.Message}\n\n{finding.Details}";

        }

        private static void PingFinding(AegisFinding finding)
        {
            if (finding == null || string.IsNullOrWhiteSpace(finding.AssetPath))
                return;

            UnityEngine.Object asset = AssetDatabase.LoadMainAssetAtPath(finding.AssetPath);
            if (asset != null)
            {
                Selection.activeObject = asset;
                EditorGUIUtility.PingObject(asset);
                AssetDatabase.OpenAsset(asset);
            }
        }
    }
}
