using HeliosApi = global::Astra.Helios.Helios;
using System;
using System.Collections.Generic;
using System.Text;
using global::Astra.Helios;
using UnityEngine;

namespace Astra.Aegis.Integrations.Helios
{
    [Serializable]
    [UnityEngine.Scripting.APIUpdating.MovedFrom(false, "MisterPxl.Aegis.HeliosIntegration", "Aegis.Helios.Runtime", "AegisValidationSnapshot")]
    public sealed class AegisValidationSnapshot
    {
        public string profile;
        public string generatedUtc;
        public string aegisVersion;
        public string status;
        public int errors;
        public int warnings;
        public int infos;

        public HeliosReportArtifact CreateReportArtifact()
        {
            return new HeliosReportArtifact(
                "aegis-validation.json",
                "application/json",
                Encoding.UTF8.GetBytes(JsonUtility.ToJson(this, true)));
        }
    }

    [UnityEngine.Scripting.APIUpdating.MovedFrom(false, "MisterPxl.Aegis.HeliosIntegration", "Aegis.Helios.Runtime", "AegisHeliosSystemInfoProvider")]
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

    /// <summary>Owns Aegis registrations across Helios service generations without booting Helios.</summary>
    [UnityEngine.Scripting.APIUpdating.MovedFrom(false, "MisterPxl.Aegis.HeliosIntegration", "Aegis.Helios.Runtime", "AegisHeliosRegistration")]
    public sealed class AegisHeliosRegistration : IDisposable
    {
        private readonly AegisHeliosSystemInfoProvider _provider;
        private readonly HeliosReportArtifact _artifact;
        private HeliosService _service;
        private bool _disposed;

        public AegisHeliosRegistration(AegisValidationSnapshot snapshot)
        {
            _provider = new AegisHeliosSystemInfoProvider(snapshot);
            _artifact = snapshot?.CreateReportArtifact();
            HeliosApi.Initialized += Attach;
            HeliosApi.ShuttingDown += Detach;
            if (HeliosApi.TryGetService(out var service)) Attach(service);
        }

        private void Attach(HeliosService service)
        {
            if (_disposed || ReferenceEquals(_service, service)) return;
            if (_service != null) Detach(_service);
            _service = service;
            service.SystemInfo.RegisterProvider(_provider);
            if (_artifact != null) service.Reporting.AddAttachment(_artifact);
        }

        private void Detach(HeliosService service)
        {
            if (!ReferenceEquals(_service, service)) return;
            _service = null;
            service.SystemInfo.UnregisterProvider(_provider);
            if (_artifact != null) service.Reporting.RemoveAttachment(_artifact);
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            HeliosApi.Initialized -= Attach;
            HeliosApi.ShuttingDown -= Detach;
            if (_service != null) Detach(_service);
        }
    }

    [UnityEngine.Scripting.APIUpdating.MovedFrom(false, "MisterPxl.Aegis.HeliosIntegration", "Aegis.Helios.Runtime", "AegisHeliosBootstrap")]
    public static class AegisHeliosBootstrap
    {
        private const string SnapshotResourcePath = "AegisValidationSnapshot";
        private static AegisHeliosRegistration _registration;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        public static void Register()
        {
            Stop();
            _registration = new AegisHeliosRegistration(LoadSnapshot());
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        public static void Stop()
        {
            _registration?.Dispose();
            _registration = null;
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
