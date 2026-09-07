using HeliosApi = global::HeliosDebugger.Helios;
using System.Linq;
using HeliosDebugger;
using NUnit.Framework;

namespace Astra.Aegis.Integrations.Helios.Tests
{
    [UnityEngine.Scripting.APIUpdating.MovedFrom(false, "MisterPxl.Aegis.HeliosIntegration.Tests", "Aegis.Helios.Tests", "AegisHeliosLifecycleTests")]
    public sealed class AegisHeliosLifecycleTests
    {
        private AegisHeliosRegistration _registration;
        [SetUp] public void SetUp() { AegisHeliosBootstrap.Stop(); HeliosApi.Shutdown(); }
        [TearDown] public void TearDown() { _registration?.Dispose(); AegisHeliosBootstrap.Stop(); HeliosApi.Shutdown(); }

        [Test]
        public void DeferredRegistrationSurvivesRestartAndRemovesItsExactAttachments()
        {
            _registration = new AegisHeliosRegistration(new AegisValidationSnapshot { status = "Passed" });
            Assert.That(HeliosApi.IsInitialized, Is.False);
            var first = HeliosApi.Service;
            Assert.That(first.SystemInfo.Providers.OfType<AegisHeliosSystemInfoProvider>().Count(), Is.EqualTo(1));
            Assert.That(first.Reporting.Builder.Attachments.Count(a => a.Name == "aegis-validation.json"), Is.EqualTo(1));
            HeliosApi.Shutdown();
            Assert.That(first.SystemInfo.Providers.OfType<AegisHeliosSystemInfoProvider>(), Is.Empty);
            Assert.That(first.Reporting.Builder.Attachments, Is.Empty);
            var second = HeliosApi.Service;
            HeliosApi.Initialize();
            Assert.That(second.SystemInfo.Providers.OfType<AegisHeliosSystemInfoProvider>().Count(), Is.EqualTo(1));
            var external = new HeliosReportArtifact("aegis-validation.json", "application/json", new byte[0]);
            second.Reporting.AddAttachment(external);
            _registration.Dispose();
            Assert.That(second.SystemInfo.Providers.OfType<AegisHeliosSystemInfoProvider>(), Is.Empty);
            Assert.That(second.Reporting.Builder.Attachments, Is.EqualTo(new[] { external }));
            HeliosApi.Shutdown();
            HeliosApi.Initialize();
            Assert.That(HeliosApi.Service.SystemInfo.Providers.OfType<AegisHeliosSystemInfoProvider>(), Is.Empty);
        }

        [Test]
        public void RegistrationAfterInitializationAndRepeatedBootstrapAreIdempotent()
        {
            HeliosApi.Initialize();
            AegisHeliosBootstrap.Register();
            AegisHeliosBootstrap.Register();
            Assert.That(HeliosApi.Service.SystemInfo.Providers.OfType<AegisHeliosSystemInfoProvider>().Count(), Is.EqualTo(1));
            AegisHeliosBootstrap.Stop();
            Assert.That(HeliosApi.Service.SystemInfo.Providers.OfType<AegisHeliosSystemInfoProvider>(), Is.Empty);
        }

        [Test]
        public void DisposingBeforeHeliosStartsDoesNotCreateOrLeaveARegistration()
        {
            _registration = new AegisHeliosRegistration(null);
            _registration.Dispose();
            Assert.That(HeliosApi.IsInitialized, Is.False);
            HeliosApi.Initialize();
            Assert.That(HeliosApi.Service.SystemInfo.Providers.OfType<AegisHeliosSystemInfoProvider>(), Is.Empty);
        }
    }
}
