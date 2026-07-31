# Aegis Project Validation

Aegis is an Editor-only Unity package for project health validation. It discovers validation rules as assets, runs them in an interactive dashboard, blocks builds when configured, and exports deterministic reports for CI.

## Install

Add the package from Git:

```text
https://github.com/MisterPxl/Aegis.git#v0.1.0
```

Optional addons:

```text
https://github.com/MisterPxl/Aegis.git?path=/Addons/Valkyrie#v0.1.0
https://github.com/MisterPxl/Aegis.git?path=/Addons/Helios#v0.1.0
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

## Creating Rules

Create a subclass of `AegisRuleAsset`, then create an asset from its `CreateAssetMenu` entry. No central registry is required.

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
