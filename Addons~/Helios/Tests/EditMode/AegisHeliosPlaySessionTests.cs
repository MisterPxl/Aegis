using HeliosApi = global::HeliosDebugger.Helios;
using System.Collections;
using System.Linq;
using HeliosDebugger;
using NUnit.Framework;
using UnityEditor;
using UnityEngine.TestTools;

namespace Astra.Aegis.Integrations.Helios.Tests
{
    [UnityEngine.Scripting.APIUpdating.MovedFrom(false, "MisterPxl.Aegis.HeliosIntegration.Tests", "Aegis.Helios.Tests", "AegisHeliosPlaySessionTests")]
    public sealed class AegisHeliosPlaySessionTests
    {
        private bool _oldEnabled;
        private EnterPlayModeOptions _oldOptions;
        [SetUp]
        public void SetUp()
        {
            _oldEnabled = EditorSettings.enterPlayModeOptionsEnabled;
            _oldOptions = EditorSettings.enterPlayModeOptions;
        }
        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (EditorApplication.isPlaying) yield return new ExitPlayMode();
            AegisHeliosBootstrap.Stop();
            HeliosApi.Shutdown();
            EditorSettings.enterPlayModeOptions = _oldOptions;
            EditorSettings.enterPlayModeOptionsEnabled = _oldEnabled;
        }
        [UnityTest]
        public IEnumerator TwoPlaySessionsWithoutDomainReloadRegisterOneProviderEach()
        {
#if !HELIOS_DEBUGGER_DISABLE_AUTO_BOOT
            Assert.Ignore("Run in an isolated empty consumer with HELIOS_DEBUGGER_DISABLE_AUTO_BOOT.");
#endif
            EditorSettings.enterPlayModeOptionsEnabled = true;
            EditorSettings.enterPlayModeOptions = EnterPlayModeOptions.DisableDomainReload;
            for (int i = 0; i < 2; i++)
            {
                yield return new EnterPlayMode(false);
                Assert.That(HeliosApi.IsInitialized, Is.False, "The previous session must be reset; Aegis must not boot HeliosApi.");
                HeliosApi.Initialize();
                Assert.That(HeliosApi.Service.SystemInfo.Providers.OfType<AegisHeliosSystemInfoProvider>().Count(), Is.EqualTo(1));
                HeliosApi.Shutdown();
                HeliosApi.Initialize();
                Assert.That(HeliosApi.Service.SystemInfo.Providers.OfType<AegisHeliosSystemInfoProvider>().Count(), Is.EqualTo(1));
                yield return new ExitPlayMode();
            }
        }
    }
}
