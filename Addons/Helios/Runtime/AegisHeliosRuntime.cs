using System;
using System.Collections.Generic;
using System.Text;
using HeliosDebugger;
using UnityEngine;

namespace MisterPxl.Aegis.HeliosIntegration
{
    [Serializable]
    public sealed class AegisValidationSnapshot
    {
        public string profile;
        public string generatedUtc;
        public string aegisVersion;
        public string status;
        public int errors;
        public int warnings;
        public int infos;
    }

    public sealed class AegisHeliosSystemInfoProvider : IHeliosSystemInfoProvider
    {
        private readonly AegisValidationSnapshot _snapshot;

        public AegisHeliosSystemInfoProvider(AegisValidationSnapshot snapshot)
        {
            _snapshot = snapshot;
        }

        public string Name => "Aegis";

        public void Collect(List<HeliosSerializablePair> values)
        {
            if (_snapshot == null)
            {
                values.Add(new HeliosSerializablePair("Aegis.Status", "Not validated"));
                return;
            }

            values.Add(new HeliosSerializablePair("Aegis.Status", _snapshot.status));
            values.Add(new HeliosSerializablePair("Aegis.Profile", _snapshot.profile));
            values.Add(new HeliosSerializablePair("Aegis.GeneratedUtc", _snapshot.generatedUtc));
            values.Add(new HeliosSerializablePair("Aegis.Version", _snapshot.aegisVersion));
            values.Add(new HeliosSerializablePair("Aegis.Errors", _snapshot.errors.ToString()));
            values.Add(new HeliosSerializablePair("Aegis.Warnings", _snapshot.warnings.ToString()));
            values.Add(new HeliosSerializablePair("Aegis.Info", _snapshot.infos.ToString()));
        }
    }

    public static class AegisHeliosBootstrap
    {
        private const string SnapshotResourcePath = "AegisValidationSnapshot";
        private static bool _registered;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void RegisterWhenHeliosExists()
        {
#if HELIOS_DEBUGGER_DISABLE
            return;
#else
            if (_registered || !Helios.IsInitialized)
                return;

            AegisValidationSnapshot snapshot = LoadSnapshot();
            Helios.RegisterSystemInfoProvider(new AegisHeliosSystemInfoProvider(snapshot));
            if (snapshot != null)
            {
                string json = JsonUtility.ToJson(snapshot, true);
                Helios.AddReportAttachment(new HeliosReportAttachment("aegis-validation.json", Encoding.UTF8.GetBytes(json)));
            }

            _registered = true;
#endif
        }

        private static AegisValidationSnapshot LoadSnapshot()
        {
            TextAsset text = Resources.Load<TextAsset>(SnapshotResourcePath);
            if (text == null || string.IsNullOrWhiteSpace(text.text))
                return null;

            try
            {
                return JsonUtility.FromJson<AegisValidationSnapshot>(text.text);
            }
            catch
            {
                return null;
            }
        }
    }
}
