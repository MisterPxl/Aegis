using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using HeliosDebugger;
using NUnit.Framework;
using UnityEngine;

namespace Astra.Aegis.Integrations.Helios.Tests
{
    [UnityEngine.Scripting.APIUpdating.MovedFrom(false, "MisterPxl.Aegis.HeliosIntegration.Tests", "Aegis.Helios.Tests", "AegisHeliosTests")]
    public sealed class AegisHeliosTests
    {
        [Test]
        public void SnapshotArtifact_PreservesJsonAndMimeTypeInExportedReport()
        {
            var snapshot = new AegisValidationSnapshot
            {
                profile = "Build été",
                generatedUtc = "2026-09-07T12:00:00Z",
                aegisVersion = "0.2.0",
                status = "Passed",
                errors = 0,
                warnings = 3,
                infos = 2
            };
            HeliosReportArtifact artifact = snapshot.CreateReportArtifact();
            var bundle = new HeliosReportBundle("aegis-test", DateTime.UtcNow, new[] { artifact });
            string directory = Path.Combine(Path.GetTempPath(), "AegisHelios-" + Guid.NewGuid().ToString("N"));
            try
            {
                HeliosMaterializedReport exported = null;
                HeliosReportResult result = null;
                var operation = new HeliosReportMaterializer().Materialize(
                    bundle, directory, HeliosReportOperationContext.None,
                    (report, outcome) => { exported = report; result = outcome; });
                while (operation.MoveNext()) { }

                Assert.That(result, Is.Not.Null);
                Assert.That(result.Success, Is.True, result.Message);
                Assert.That(exported.Artifacts.Count, Is.EqualTo(1));
                Assert.That(exported.Artifacts[0].Name, Is.EqualTo("aegis-validation.json"));
                Assert.That(exported.Artifacts[0].MimeType, Is.EqualTo("application/json"));
                string json = File.ReadAllText(exported.Artifacts[0].Path, Encoding.UTF8);
                Assert.That(json, Is.EqualTo(JsonUtility.ToJson(snapshot, true)));
                var restored = JsonUtility.FromJson<AegisValidationSnapshot>(json);
                Assert.That(restored.profile, Is.EqualTo(snapshot.profile));
                Assert.That(restored.warnings, Is.EqualTo(3));
            }
            finally
            {
                if (Directory.Exists(directory))
                    Directory.Delete(directory, true);
            }
        }

        [Test]
        public void SnapshotArtifact_IsUnaffectedByLaterSnapshotChanges()
        {
            var snapshot = new AegisValidationSnapshot { status = "Passed", warnings = 1 };
            HeliosReportArtifact artifact = snapshot.CreateReportArtifact();
            snapshot.status = "Failed";
            snapshot.warnings = 9;

            var captured = JsonUtility.FromJson<AegisValidationSnapshot>(Encoding.UTF8.GetString(artifact.GetContentCopy()));

            Assert.That(captured.status, Is.EqualTo("Passed"));
            Assert.That(captured.warnings, Is.EqualTo(1));
        }

        [Test]
        public void SystemInfoProvider_ReportsNotValidatedWithoutSnapshot()
        {
            AegisHeliosSystemInfoProvider provider = new AegisHeliosSystemInfoProvider(null);
            List<HeliosSerializablePair> values = new List<HeliosSerializablePair>();

            provider.Collect(values);

            Assert.AreEqual("Aegis.Status", values[0].Key);
            Assert.AreEqual("Not validated", values[0].Value);
        }

        [Test]
        public void SystemInfoProvider_ReportsSnapshotCounts()
        {
            AegisValidationSnapshot snapshot = new AegisValidationSnapshot
            {
                profile = "Build",
                generatedUtc = "2026-01-01T00:00:00Z",
                aegisVersion = "0.1.0",
                status = "Passed",
                errors = 0,
                warnings = 1,
                infos = 2
            };
            AegisHeliosSystemInfoProvider provider = new AegisHeliosSystemInfoProvider(snapshot);
            List<HeliosSerializablePair> values = new List<HeliosSerializablePair>();

            provider.Collect(values);

            Assert.IsTrue(values.Exists(pair => pair.Key == "Aegis.Status" && pair.Value == "Passed"));
            Assert.IsTrue(values.Exists(pair => pair.Key == "Aegis.Warnings" && pair.Value == "1"));
        }
    }
}
