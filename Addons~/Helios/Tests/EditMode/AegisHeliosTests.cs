using System.Collections.Generic;
using HeliosDebugger;
using NUnit.Framework;

namespace MisterPxl.Aegis.HeliosIntegration.Tests
{
    public sealed class AegisHeliosTests
    {
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
