using System.Linq;
using HeliosDebugger;
using NUnit.Framework;

namespace MisterPxl.Aegis.HeliosIntegration.Tests
{
    public sealed class AegisHeliosLifecycleTests
    {
        private AegisHeliosRegistration _registration;
        [SetUp] public void SetUp() { AegisHeliosBootstrap.Stop(); Helios.Shutdown(); }
        [TearDown] public void TearDown() { _registration?.Dispose(); AegisHeliosBootstrap.Stop(); Helios.Shutdown(); }

        [Test]
        public void DeferredRegistrationSurvivesRestartAndRemovesItsExactAttachments()
        {
            _registration = new AegisHeliosRegistration(new AegisValidationSnapshot { status = "Passed" });
            Assert.That(Helios.IsInitialized, Is.False);
            var first = Helios.Service;
            Assert.That(first.SystemInfo.Providers.OfType<AegisHeliosSystemInfoProvider>().Count(), Is.EqualTo(1));
            Assert.That(first.Reporting.Builder.Attachments.Count(a => a.Name == "aegis-validation.json"), Is.EqualTo(1));
            Helios.Shutdown();
            Assert.That(first.SystemInfo.Providers.OfType<AegisHeliosSystemInfoProvider>(), Is.Empty);
            Assert.That(first.Reporting.Builder.Attachments, Is.Empty);
            var second = Helios.Service;
            Helios.Initialize();
            Assert.That(second.SystemInfo.Providers.OfType<AegisHeliosSystemInfoProvider>().Count(), Is.EqualTo(1));
            var external = new HeliosReportArtifact("aegis-validation.json", "application/json", new byte[0]);
            second.Reporting.AddAttachment(external);
            _registration.Dispose();
            Assert.That(second.SystemInfo.Providers.OfType<AegisHeliosSystemInfoProvider>(), Is.Empty);
            Assert.That(second.Reporting.Builder.Attachments, Is.EqualTo(new[] { external }));
            Helios.Shutdown();
            Helios.Initialize();
            Assert.That(Helios.Service.SystemInfo.Providers.OfType<AegisHeliosSystemInfoProvider>(), Is.Empty);
        }

        [Test]
        public void RegistrationAfterInitializationAndRepeatedBootstrapAreIdempotent()
        {
            Helios.Initialize();
            AegisHeliosBootstrap.Register();
            AegisHeliosBootstrap.Register();
            Assert.That(Helios.Service.SystemInfo.Providers.OfType<AegisHeliosSystemInfoProvider>().Count(), Is.EqualTo(1));
            AegisHeliosBootstrap.Stop();
            Assert.That(Helios.Service.SystemInfo.Providers.OfType<AegisHeliosSystemInfoProvider>(), Is.Empty);
        }

        [Test]
        public void DisposingBeforeHeliosStartsDoesNotCreateOrLeaveARegistration()
        {
            _registration = new AegisHeliosRegistration(null);
            _registration.Dispose();
            Assert.That(Helios.IsInitialized, Is.False);
            Helios.Initialize();
            Assert.That(Helios.Service.SystemInfo.Providers.OfType<AegisHeliosSystemInfoProvider>(), Is.Empty);
        }
    }
}
