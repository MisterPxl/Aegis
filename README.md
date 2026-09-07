# Aegis Project Validation

Aegis is an Editor-only Unity package for project health validation. It discovers validation rules as assets, runs them in an interactive dashboard, blocks builds when configured, and exports deterministic reports for CI.

## Install

Add the package from Git:

```text
https://github.com/MisterPxl/Aegis.git#v0.2.0
```

Optional addons:

```text
https://github.com/MisterPxl/Aegis.git?path=/Addons~/Valkyrie#v0.2.0
https://github.com/MisterPxl/Aegis.git?path=/Addons~/Helios#v0.2.0
```

## Quick Start

Open `Tools > Aegis > Project Health`, then click `Run`.

Aegis stores the latest report in `Library/Aegis/last-report.json`. This file is intentionally outside `Assets/`.

## Build Gate

The build gate runs with the `Build` profile through `IPreprocessBuildWithReport`. It throws `BuildFailedException` only when findings meet the configured failure threshold.

## CI

Use Unity batchmode:

```bash
Unity -batchmode -quit -projectPath "$PWD" \
  -executeMethod MisterPxl.Aegis.AegisCli.Run \
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
