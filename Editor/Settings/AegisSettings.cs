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
        [SerializeField] private AegisValidationProfile _interactiveProfile;
        [SerializeField] private AegisValidationProfile _buildProfile;
        [SerializeField] private AegisValidationProfile _ciProfile;
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

        public AegisValidationProfile InteractiveProfile => EnsureProfile(ref _interactiveProfile, "Interactive", AegisSeverity.Error, 8);
        public AegisValidationProfile BuildProfile => EnsureProfile(ref _buildProfile, "Build", AegisSeverity.Error, 1);
        public AegisValidationProfile CiProfile => EnsureProfile(ref _ciProfile, "CI", AegisSeverity.Error, 1);
        public IReadOnlyList<AegisSuppression> Suppressions => _suppressions;

        public AegisValidationProfile GetProfile(string profileName)
        {
            if (string.Equals(profileName, "Build", StringComparison.OrdinalIgnoreCase))
                return BuildProfile.Clone();

            if (string.Equals(profileName, "CI", StringComparison.OrdinalIgnoreCase))
                return CiProfile.Clone();

            return InteractiveProfile.Clone();
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
            Save(true);
        }

        private static AegisValidationProfile EnsureProfile(
            ref AegisValidationProfile profile,
            string name,
            AegisSeverity threshold,
            int frameBudgetMs)
        {
            if (profile != null)
                return profile;

            profile = new AegisValidationProfile
            {
                Name = name,
                FailureThreshold = threshold,
                FrameBudgetMs = frameBudgetMs
            };
            return profile;
        }
    }
}
