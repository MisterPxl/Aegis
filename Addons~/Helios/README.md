# Astra Aegis — Helios Integration

An optional **Astra integration**. Install its prerequisites explicitly; the base
packages remain usable independently. This is the **2.0.0 migration candidate**; it requires Aegis 1.x and uses the
`Astra.Aegis.Integrations` namespaces. Existing tags retain the old API.

## Compatibility (unreleased)

The working integration targets Helios Debugger **3.0.0 through 3.x** and uses
`HeliosReportArtifact` with the `application/json` MIME type. Helios 1.x and 2.x are no
longer supported. The branch URL below is a candidate; pin the exact revision
validated with your consumer before shipping.

All Runtime, Editor and test assemblies are excluded when
`HELIOS_DEBUGGER_DISABLE` is set. The base Aegis Editor validator remains available.
Remove this integration when removing either of its package prerequisites.

Git consumers must explicitly install Aegis, Helios and this integration in the
project manifest; UPM does not fetch Git dependencies transitively. For local
testing, copy the integration to a separate sibling package directory instead of
registering a package physically nested inside the local Aegis package.

Optional package:

```text
https://github.com/MisterPxl/Aegis.git?path=/Addons~/Helios#codex/astra-foundation
```

During validated builds, the editor side generates a minimal `AegisValidationSnapshot` resource. At runtime, the Helios side exposes that snapshot through System Info and attaches the same JSON to bug reports.

The snapshot contains only counts and status. It does not embed asset paths, finding messages or full validation reports.

The EditMode suite verifies that the snapshot is exported as UTF-8 JSON by the
Helios materializer and keeps its captured values when the source snapshot changes.

## Prerequisites and tests

The manifest declares these package versions:

- `com.misterpxl.aegis`: `1.0.0` (compatible 1.x).
- `com.misterpxl.helios-debugger`: `3.0.0`.

Install the Astra base packages explicitly in the consumer manifest, using the
Git URLs from their READMEs. Git packages are not fetched transitively from
version-only dependencies. Unity registry dependencies resolve normally.

Add `com.misterpxl.aegis.helios` to the consumer manifest’s `testables` and run
its suites in Unity Test Runner.

## Removal

Remove project components, assets or code that reference this integration before
removing it through Package Manager. The base packages can remain installed.

## Runtime lifecycle

The integration requires Helios 3.0.0 through 3.x. Bootstrap
registers a passive observer before the first scene; it does not create a polling
GameObject, impose a timeout or initialize Helios. Each service generation receives
one provider and, when a snapshot exists, one JSON artifact. Shutdown removes those
exact instances; restarting Helios attaches them to the new generation.

`AegisHeliosBootstrap.Register()` replaces the previous bootstrap registration;
`Stop()` removes it. For custom compositions, own an `AegisHeliosRegistration`
and dispose it when that composition ends. Session reset and BeforeSceneLoad
registration cover repeated Play sessions with domain reload disabled.
