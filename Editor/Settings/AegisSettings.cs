using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace MisterPxl.Aegis
{
    [FilePath("ProjectSettings/AegisSettings.asset", FilePathAttribute.Location.ProjectFolder)]
    public sealed class AegisSettings : ScriptableSingleton<AegisSettings>
    {
        [SerializeField] private bool _buildGateEnabled = true;
        [SerializeField] private AegisValidationProfile _interactiveProfile = CreateProfile("Interactive", 8);
        [SerializeField] private AegisValidationProfile _buildProfile = CreateProfile("Build", 1);
        [SerializeField] private AegisValidationProfile _ciProfile = CreateProfile("CI", 1);
        [SerializeField] private int _profileSchemaVersion;
        [SerializeField] private List<AegisSuppression> _suppressions = new List<AegisSuppression>();

        public bool BuildGateEnabled
        {
            get => _buildGateEnabled;
            set
            {
                _buildGateEnabled = value;
                Save();
            }
        }

        public AegisValidationProfile InteractiveProfile { get { EnsureProfiles(); return _interactiveProfile; } }
        public AegisValidationProfile BuildProfile { get { EnsureProfiles(); return _buildProfile; } }
        public AegisValidationProfile CiProfile { get { EnsureProfiles(); return _ciProfile; } }
        public IReadOnlyList<AegisSuppression> Suppressions => _suppressions;

        public AegisValidationProfile GetProfile(string profileName)
        {
            if (string.Equals(profileName, "Build", StringComparison.OrdinalIgnoreCase))
                return BuildProfile.Clone();

            if (string.Equals(profileName, "CI", StringComparison.OrdinalIgnoreCase))
                return CiProfile.Clone();

            return InteractiveProfile.Clone();
        }

        public bool TryGetProfile(string profileName, out AegisValidationProfile profile)
        {
            if (string.Equals(profileName, "Interactive", StringComparison.OrdinalIgnoreCase))
            {
                profile = InteractiveProfile.Clone();
                return true;
            }

            if (string.Equals(profileName, "Build", StringComparison.OrdinalIgnoreCase))
            {
                profile = BuildProfile.Clone();
                return true;
            }

            if (string.Equals(profileName, "CI", StringComparison.OrdinalIgnoreCase))
            {
                profile = CiProfile.Clone();
                return true;
            }

            profile = null;
            return false;
        }

        public bool IsSuppressed(AegisFinding finding)
        {
            if (finding == null)
                return false;

            DateTime utcNow = DateTime.UtcNow;
            for (int i = 0; i < _suppressions.Count; i++)
            {
                AegisSuppression suppression = _suppressions[i];
                if (suppression == null || suppression.IsExpired(utcNow))
                    continue;

                if (string.Equals(suppression.Fingerprint, finding.Fingerprint, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }

        public void AddSuppression(string fingerprint, string reason, string author, string expiresUtc = null)
        {
            if (string.IsNullOrWhiteSpace(fingerprint))
                return;

            _suppressions.Add(new AegisSuppression(fingerprint, reason, author, expiresUtc));
            Save();
        }

        public void RemoveSuppression(string fingerprint)
        {
            _suppressions.RemoveAll(item => item != null
                && string.Equals(item.Fingerprint, fingerprint, StringComparison.OrdinalIgnoreCase));
            Save();
        }

        public void Save()
        {
            EnsureProfiles();
            Save(true);
        }

        private void OnEnable()
        {
            EnsureProfiles();
        }

        private void EnsureProfiles()
        {
            _interactiveProfile = _interactiveProfile ?? CreateProfile("Interactive", 8);
            _buildProfile = _buildProfile ?? CreateProfile("Build", 1);
            _ciProfile = _ciProfile ?? CreateProfile("CI", 1);

            if (_profileSchemaVersion >= 1)
                return;

            MigrateProfile(_buildProfile, "Build");
            MigrateProfile(_ciProfile, "CI");
            _profileSchemaVersion = 1;
        }

        private static void MigrateProfile(AegisValidationProfile profile, string name)
        {
            // Older saves materialized uninitialized inline profiles with Interactive defaults.
            // Preserve thresholds, filters, disabled rules and non-default frame budgets.
            if (string.Equals(profile.Name, "Interactive", StringComparison.OrdinalIgnoreCase))
            {
                profile.Name = name;
                if (profile.FrameBudgetMs == 8)
                    profile.FrameBudgetMs = 1;
            }
        }

        private static AegisValidationProfile CreateProfile(string name, int frameBudgetMs)
        {
            return new AegisValidationProfile
            {
                Name = name, FailureThreshold = AegisSeverity.Error, FrameBudgetMs = frameBudgetMs
            };
        }
    }
}
