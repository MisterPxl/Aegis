using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace MisterPxl.Aegis
{
    public enum AegisSeverity
    {
        Info = 0,
        Warning = 1,
        Error = 2
    }

    public enum AegisFixSafety
    {
        Safe = 0,
        ReviewRequired = 1,
        Destructive = 2
    }

    public enum AegisRuleExecutionStatus
    {
        Passed = 0,
        Findings = 1,
        Skipped = 2,
        Failed = 3
    }

    public enum AegisExitCode
    {
        Success = 0,
        BlockingFindings = 2,
        ConfigurationError = 3,
        InternalError = 4
    }

    public interface IAegisFindingSink
    {
        void Add(AegisFinding finding);
    }

    public interface IAegisFixAction
    {
        string Label { get; }
        AegisFixSafety Safety { get; }
        AegisFixResult Execute(AegisFinding finding, AegisValidationContext context);
    }

    public interface IAegisKeyProvider
    {
        string AegisKey { get; }
    }

    [Serializable]
    public sealed class AegisFinding
    {
        [SerializeField] private string _ruleId;
        [SerializeField] private string _ruleName;
        [SerializeField] private AegisSeverity _severity;
        [SerializeField] private string _message;
        [SerializeField] private string _details;
        [SerializeField] private string _assetPath;
        [SerializeField] private string _globalObjectId;
        [SerializeField] private string _propertyPath;
        [SerializeField] private string _code;
        [SerializeField] private string _fingerprint;
        [NonSerialized] private IAegisFixAction _fixAction;

        private AegisFinding()
        {
        }

        public AegisFinding(
            string ruleId,
            string ruleName,
            AegisSeverity severity,
            string message,
            string details = null,
            string assetPath = null,
            string globalObjectId = null,
            string propertyPath = null,
            string code = null,
            IAegisFixAction fixAction = null)
        {
            _ruleId = ruleId ?? string.Empty;
            _ruleName = ruleName ?? string.Empty;
            _severity = severity;
            _message = message ?? string.Empty;
            _details = details ?? string.Empty;
            _assetPath = assetPath ?? string.Empty;
            _globalObjectId = globalObjectId ?? string.Empty;
            _propertyPath = propertyPath ?? string.Empty;
            _code = code ?? string.Empty;
            _fixAction = fixAction;
            _fingerprint = AegisFingerprint.Compute(_ruleId, _assetPath, _globalObjectId, _propertyPath, _code, _message);
        }

        public string RuleId => _ruleId;
        public string RuleName => _ruleName;
        public AegisSeverity Severity => _severity;
        public string Message => _message;
        public string Details => _details;
        public string AssetPath => _assetPath;
        public string GlobalObjectId => _globalObjectId;
        public string PropertyPath => _propertyPath;
        public string Code => _code;
        public string Fingerprint => _fingerprint;
        public IAegisFixAction FixAction => _fixAction;

        public bool HasFix => _fixAction != null;
    }

    [Serializable]
    public sealed class AegisRuleExecutionRecord
    {
        [SerializeField] private string _ruleId;
        [SerializeField] private string _ruleName;
        [SerializeField] private AegisRuleExecutionStatus _status;
        [SerializeField] private int _findingCount;
        [SerializeField] private double _durationMs;
        [SerializeField] private string _message;

        private AegisRuleExecutionRecord()
        {
        }

        public AegisRuleExecutionRecord(
            string ruleId,
            string ruleName,
            AegisRuleExecutionStatus status,
            int findingCount,
            double durationMs,
            string message)
        {
            _ruleId = ruleId ?? string.Empty;
            _ruleName = ruleName ?? string.Empty;
            _status = status;
            _findingCount = findingCount;
            _durationMs = durationMs;
            _message = message ?? string.Empty;
        }

        public string RuleId => _ruleId;
        public string RuleName => _ruleName;
        public AegisRuleExecutionStatus Status => _status;
        public int FindingCount => _findingCount;
        public double DurationMs => _durationMs;
        public string Message => _message;
    }

    [Serializable]
    public sealed class AegisValidationReport
    {
        [SerializeField] private string _generatedUtc;
        [SerializeField] private string _unityVersion;
        [SerializeField] private string _aegisVersion;
        [SerializeField] private string _profileName;
        [SerializeField] private double _durationMs;
        [SerializeField] private List<AegisFinding> _findings;
        [SerializeField] private List<AegisRuleExecutionRecord> _rules;

        private AegisValidationReport()
        {
        }

        public AegisValidationReport(
            string profileName,
            double durationMs,
            List<AegisFinding> findings,
            List<AegisRuleExecutionRecord> rules)
        {
            _generatedUtc = DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture);
            _unityVersion = Application.unityVersion;
            _aegisVersion = AegisPackageInfo.Version;
            _profileName = profileName ?? string.Empty;
            _durationMs = durationMs;
            _findings = findings ?? new List<AegisFinding>();
            _rules = rules ?? new List<AegisRuleExecutionRecord>();
        }

        public string GeneratedUtc => _generatedUtc;
        public string UnityVersion => _unityVersion;
        public string AegisVersion => _aegisVersion;
        public string ProfileName => _profileName;
        public double DurationMs => _durationMs;
        public IReadOnlyList<AegisFinding> Findings => _findings;
        public IReadOnlyList<AegisRuleExecutionRecord> Rules => _rules;

        public int ErrorCount => Count(AegisSeverity.Error);
        public int WarningCount => Count(AegisSeverity.Warning);
        public int InfoCount => Count(AegisSeverity.Info);
        public bool HasBlockingFindings(AegisSeverity threshold) => (ErrorCount > 0 && threshold == AegisSeverity.Error)
            || (WarningCount + ErrorCount > 0 && threshold == AegisSeverity.Warning)
            || (Findings.Count > 0 && threshold == AegisSeverity.Info);

        private int Count(AegisSeverity severity)
        {
            int count = 0;
            for (int i = 0; i < _findings.Count; i++)
            {
                if (_findings[i].Severity == severity)
                    count++;
            }

            return count;
        }
    }

    public sealed class AegisFixResult
    {
        private AegisFixResult(bool success, string message, Exception exception)
        {
            Success = success;
            Message = message ?? string.Empty;
            Exception = exception;
        }

        public bool Success { get; }
        public string Message { get; }
        public Exception Exception { get; }

        public static AegisFixResult Succeed(string message = null)
        {
            return new AegisFixResult(true, message ?? "Fix applied.", null);
        }

        public static AegisFixResult Fail(string message, Exception exception = null)
        {
            return new AegisFixResult(false, message, exception);
        }
    }

    public sealed class AegisRunResult
    {
        private AegisRunResult(bool success, string message, AegisValidationReport report, Exception exception)
        {
            Success = success;
            Message = message ?? string.Empty;
            Report = report;
            Exception = exception;
        }

        public bool Success { get; }
        public string Message { get; }
        public AegisValidationReport Report { get; }
        public Exception Exception { get; }

        public static AegisRunResult Succeed(AegisValidationReport report, string message = null)
        {
            return new AegisRunResult(true, message ?? "Aegis validation completed.", report, null);
        }

        public static AegisRunResult Fail(string message, Exception exception = null, AegisValidationReport report = null)
        {
            return new AegisRunResult(false, message, report, exception);
        }
    }

    [Serializable]
    public sealed class AegisValidationProfile
    {
        [SerializeField] private string _name = "Interactive";
        [SerializeField] private AegisSeverity _failureThreshold = AegisSeverity.Error;
        [SerializeField] private List<string> _includedFolders = new List<string>();
        [SerializeField] private List<string> _excludedFolders = new List<string>();
        [SerializeField] private List<string> _includedCategories = new List<string>();
        [SerializeField] private List<string> _disabledRuleIds = new List<string>();
        [SerializeField] private int _frameBudgetMs = 8;

        public string Name
        {
            get => _name;
            set => _name = string.IsNullOrWhiteSpace(value) ? "Profile" : value;
        }

        public AegisSeverity FailureThreshold
        {
            get => _failureThreshold;
            set => _failureThreshold = value;
        }

        public List<string> IncludedFolders => _includedFolders;
        public List<string> ExcludedFolders => _excludedFolders;
        public List<string> IncludedCategories => _includedCategories;
        public List<string> DisabledRuleIds => _disabledRuleIds;

        public int FrameBudgetMs
        {
            get => Mathf.Max(1, _frameBudgetMs);
            set => _frameBudgetMs = Mathf.Max(1, value);
        }

        public bool IsRuleEnabled(AegisRuleAsset rule)
        {
            if (rule == null || !rule.EnabledByDefault)
                return false;

            string ruleId = rule.RuleId;
            for (int i = 0; i < _disabledRuleIds.Count; i++)
            {
                if (string.Equals(_disabledRuleIds[i], ruleId, StringComparison.OrdinalIgnoreCase))
                    return false;
            }

            if (_includedCategories.Count == 0)
                return true;

            for (int i = 0; i < _includedCategories.Count; i++)
            {
                if (string.Equals(_includedCategories[i], rule.Category, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }

        public bool IsPathIncluded(string assetPath)
        {
            string safePath = assetPath ?? string.Empty;
            if (_includedFolders.Count > 0)
            {
                bool hasIncludedMatch = false;
                for (int i = 0; i < _includedFolders.Count; i++)
                {
                    if (PathStartsWith(safePath, _includedFolders[i]))
                        hasIncludedMatch = true;
                }

                if (!hasIncludedMatch)
                    return false;
            }

            for (int i = 0; i < _excludedFolders.Count; i++)
            {
                if (PathStartsWith(safePath, _excludedFolders[i]))
                    return false;
            }

            return true;
        }

        public AegisValidationProfile Clone()
        {
            AegisValidationProfile profile = new AegisValidationProfile
            {
                Name = Name,
                FailureThreshold = FailureThreshold,
                FrameBudgetMs = FrameBudgetMs
            };
            profile.IncludedFolders.AddRange(IncludedFolders);
            profile.ExcludedFolders.AddRange(ExcludedFolders);
            profile.IncludedCategories.AddRange(IncludedCategories);
            profile.DisabledRuleIds.AddRange(DisabledRuleIds);
            return profile;
        }

        private static bool PathStartsWith(string path, string folder)
        {
            if (string.IsNullOrWhiteSpace(folder))
                return false;

            string normalizedPath = path.Replace('\\', '/');
            string normalizedFolder = folder.Replace('\\', '/').TrimEnd('/');
            return normalizedPath.Equals(normalizedFolder, StringComparison.OrdinalIgnoreCase)
                || normalizedPath.StartsWith(normalizedFolder + "/", StringComparison.OrdinalIgnoreCase);
        }
    }

    [Serializable]
    public sealed class AegisSuppression
    {
        [SerializeField] private string _fingerprint;
        [SerializeField] private string _reason;
        [SerializeField] private string _author;
        [SerializeField] private string _expiresUtc;

        private AegisSuppression()
        {
        }

        public AegisSuppression(string fingerprint, string reason, string author, string expiresUtc = null)
        {
            _fingerprint = fingerprint ?? string.Empty;
            _reason = reason ?? string.Empty;
            _author = author ?? string.Empty;
            _expiresUtc = expiresUtc ?? string.Empty;
        }

        public string Fingerprint => _fingerprint;
        public string Reason => _reason;
        public string Author => _author;
        public string ExpiresUtc => _expiresUtc;

        public bool IsExpired(DateTime utcNow)
        {
            if (string.IsNullOrWhiteSpace(_expiresUtc))
                return false;

            if (!DateTime.TryParse(_expiresUtc, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal, out DateTime expiry))
                return true;

            return expiry.ToUniversalTime() <= utcNow;
        }
    }

    public sealed class AegisValidationContext
    {
        private readonly Dictionary<string, string[]> _assetSearchCache = new Dictionary<string, string[]>();
        private readonly Func<bool> _isCancellationRequested;

        public AegisValidationContext(AegisValidationProfile profile, Func<bool> isCancellationRequested = null)
        {
            Profile = profile ?? new AegisValidationProfile();
            _isCancellationRequested = isCancellationRequested;
        }

        public AegisValidationProfile Profile { get; }
        public bool IsCancellationRequested => _isCancellationRequested != null && _isCancellationRequested();

        public string[] FindAssetPaths(string filter, params string[] folders)
        {
            string safeFilter = string.IsNullOrWhiteSpace(filter) ? string.Empty : filter;
            string folderKey = folders == null || folders.Length == 0 ? string.Empty : string.Join("|", folders);
            string cacheKey = safeFilter + "::" + folderKey;
            if (_assetSearchCache.TryGetValue(cacheKey, out string[] cachedPaths))
                return cachedPaths;

            string[] guids = folders == null || folders.Length == 0
                ? AssetDatabase.FindAssets(safeFilter)
                : AssetDatabase.FindAssets(safeFilter, folders);
            List<string> paths = new List<string>(guids.Length);
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                if (!string.IsNullOrWhiteSpace(path) && Profile.IsPathIncluded(path))
                    paths.Add(path);
            }

            paths.Sort(StringComparer.OrdinalIgnoreCase);
            string[] result = paths.ToArray();
            _assetSearchCache[cacheKey] = result;
            return result;
        }
    }

    public abstract class AegisRuleAsset : ScriptableObject
    {
        [SerializeField] private string _displayName;
        [SerializeField] private string _category = "General";
        [SerializeField] private AegisSeverity _defaultSeverity = AegisSeverity.Error;
        [SerializeField] private bool _enabledByDefault = true;
        [SerializeField] private int _order;

        public virtual string RuleId
        {
            get
            {
                string path = AssetDatabase.GetAssetPath(this);
                if (!string.IsNullOrEmpty(path))
                {
                    string guid = AssetDatabase.AssetPathToGUID(path);
                    if (!string.IsNullOrEmpty(guid))
                        return guid;
                }

                return GetType().FullName;
            }
        }

        public string DisplayName => string.IsNullOrWhiteSpace(_displayName) ? ObjectNames.NicifyVariableName(GetType().Name) : _displayName;
        public string Category => string.IsNullOrWhiteSpace(_category) ? "General" : _category;
        public AegisSeverity DefaultSeverity => _defaultSeverity;
        public bool EnabledByDefault => _enabledByDefault;
        public int Order => _order;

        public abstract void Evaluate(AegisValidationContext context, IAegisFindingSink sink);

        protected AegisFinding CreateFinding(
            string message,
            string details = null,
            string assetPath = null,
            string globalObjectId = null,
            string propertyPath = null,
            string code = null,
            IAegisFixAction fixAction = null,
            AegisSeverity? severity = null)
        {
            return new AegisFinding(
                RuleId,
                DisplayName,
                severity ?? DefaultSeverity,
                message,
                details,
                assetPath,
                globalObjectId,
                propertyPath,
                code,
                fixAction);
        }
    }

    public sealed class AegisFindingSink : IAegisFindingSink
    {
        private readonly List<AegisFinding> _findings;

        public AegisFindingSink(List<AegisFinding> findings)
        {
            _findings = findings ?? throw new ArgumentNullException(nameof(findings));
        }

        public void Add(AegisFinding finding)
        {
            if (finding != null)
                _findings.Add(finding);
        }
    }

    public static class AegisPackageInfo
    {
        public const string Version = "0.1.0";
        public const string ReportFolder = "Library/Aegis";
        public const string LastReportPath = "Library/Aegis/last-report.json";
    }

    public static class AegisFingerprint
    {
        public static string Compute(params string[] values)
        {
            StringBuilder builder = new StringBuilder();
            if (values != null)
            {
                for (int i = 0; i < values.Length; i++)
                    builder.Append(values[i] ?? string.Empty).Append('\n');
            }

            using (SHA256 sha = SHA256.Create())
            {
                byte[] hash = sha.ComputeHash(Encoding.UTF8.GetBytes(builder.ToString()));
                StringBuilder result = new StringBuilder(hash.Length * 2);
                for (int i = 0; i < hash.Length; i++)
                    result.Append(hash[i].ToString("x2", CultureInfo.InvariantCulture));

                return result.ToString();
            }
        }
    }

    public static class AegisObjectId
    {
        public static string TryGet(UnityEngine.Object obj)
        {
            if (obj == null)
                return string.Empty;

            try
            {
                GlobalObjectId id = GlobalObjectId.GetGlobalObjectIdSlow(obj);
                return id.ToString();
            }
            catch
            {
                return string.Empty;
            }
        }
    }
}
