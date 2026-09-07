# Astra Aegis — Project Validation

Part of the **Astra** family. This package works independently of the Astra framework.

This branch contains the **1.0.0 migration candidate**: namespaces and assemblies
have changed. Read [the migration guide](Documentation~/Migration-1.0/README.md)
before upgrading an existing project. No 1.0 release tag is published yet.

Aegis is an Editor-only Unity package for project health validation. It discovers validation rules as assets, runs them in an interactive dashboard, blocks builds when configured, and exports deterministic reports for CI.

## Install

Add the package from Git:

```text
https://github.com/MisterPxl/Aegis.git#codex/astra-foundation
```

Optional integrations:

```text
https://github.com/MisterPxl/Aegis.git?path=/Addons~/Valkyrie#codex/astra-foundation
https://github.com/MisterPxl/Aegis.git?path=/Addons~/Helios#codex/astra-foundation
```

## Quick Start

Open `Tools > Astra > Aegis > Project Health`, then click `Run`.

Aegis stores the latest report in `Library/Aegis/last-report.json`. This file is intentionally outside `Assets/`.

## Build Gate

The build gate runs with the `Build` profile through `IPreprocessBuildWithReport`. It throws `BuildFailedException` only when findings meet the configured failure threshold.

## CI

Use Unity batchmode:

```bash
Unity -batchmode -quit -projectPath "$PWD" \
  -executeMethod Astra.Aegis.AegisCli.Run \
  -aegisProfile CI \
  -aegisJson Library/Aegis/aegis-report.json \
  -aegisJUnit Library/Aegis/aegis-report.xml
```

Exit codes:

- `0`: success
- `2`: blocking findings
- `3`: configuration error
- `4`: internal error

JUnit reports use the chosen profile's failure threshold and effective, unsuppressed findings. Disabled rules are skipped; rule execution exceptions are reported as errors and return exit code `4`. Both report files are exported when a rule fails. When calling `AegisReportWriters.WriteJUnit` directly, pass the profile's threshold as its third argument (the default is `Error`).

## Creating Rules

Create a subclass of `AegisRuleAsset`, then create an asset from its `CreateAssetMenu` entry. No central registry is required.

Place each rule in an `Editor` folder or Editor-only assembly, in a script matching the class name (for example `Editor/MyRule.cs`).

```csharp
public sealed class MyRule : AegisRuleAsset
{
    public override void Evaluate(AegisValidationContext context, IAegisFindingSink sink)
    {
        sink.Add(CreateFinding("Something is invalid.", assetPath: "Assets/My.asset"));
    }
}
```

## Fixes

Findings may expose `IAegisFixAction`. Safe fixes can be applied through `Fix All Safe`; review-required or destructive fixes need explicit confirmation.

## Astra conventions

See [Astra conventions](Documentation~/AstraConventions.md) for product identity,
menu paths, terminology and the staged API migration policy.

Menu migration: `Tools > Aegis` is now `Tools > Astra > Aegis`.

## Tests

Add `com.misterpxl.aegis` to the consumer manifest’s `testables` and run its
EditMode suite in Unity Test Runner. Include each integration in `testables` when
validating that integration.

## Removal

Remove Aegis integrations first, then project-owned custom rules and rule assets
that reference Aegis. Remove the base package through Package Manager. Keep reports
from `Library/Aegis` separately if needed; the package does not delete project data.
