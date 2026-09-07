# Astra Aegis — Helios Integration

An optional **Astra integration**. Install its prerequisites explicitly; the base
packages remain usable independently. Astra labels in this working copy will ship
with the next release; existing published tags retain their earlier labels.

## Compatibility (unreleased)

The working integration targets Helios Debugger **2.3.1 through 2.x** and uses
`HeliosReportArtifact` with the `application/json` MIME type. Helios 1.x is no
longer supported by this working version. The published `v0.2.0` URL below still
contains the earlier integration; these changes require a new integration release.

All Runtime, Editor and test assemblies are excluded when
`HELIOS_DEBUGGER_DISABLE` is set. The base Aegis Editor validator remains available.
Remove this integration when removing either of its package prerequisites.

Git consumers must explicitly install Aegis, Helios and this integration in the
project manifest; UPM does not fetch Git dependencies transitively. For local
testing, copy the integration to a separate sibling package directory instead of
registering a package physically nested inside the local Aegis package.

Optional package:

```text
https://github.com/MisterPxl/Aegis.git?path=/Addons~/Helios#v0.2.0
```

During validated builds, the editor side generates a minimal `AegisValidationSnapshot` resource. At runtime, the Helios side exposes that snapshot through System Info and attaches the same JSON to bug reports.

The snapshot contains only counts and status. It does not embed asset paths, finding messages or full validation reports.

The EditMode suite verifies that the snapshot is exported as UTF-8 JSON by the
Helios materializer and keeps its captured values when the source snapshot changes.

## Prerequisites and tests

The manifest declares these package versions:

- `com.misterpxl.aegis`: `0.2.0`.
- `com.misterpxl.helios-debugger`: `2.3.1`.

Install the Astra base packages explicitly in the consumer manifest, using the
Git URLs from their READMEs. Git packages are not fetched transitively from
version-only dependencies. Unity registry dependencies resolve normally.

Add `com.misterpxl.aegis.helios` to the consumer manifest’s `testables` and run
its suites in Unity Test Runner.

## Removal

Remove project components, assets or code that reference this integration before
removing it through Package Manager. The base packages can remain installed.
