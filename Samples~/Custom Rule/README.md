# Custom Rule Sample

This sample shows a subjective project policy rule. It is intentionally shipped as a sample rather than a default rule.

After importing, create a `Scene Naming Rule` asset from `Assets > Create > Aegis > Samples > Scene Naming Rule`.

The rule lives in `Editor/SceneNamingRule.cs` so it is excluded from Player builds and Unity can persist its script reference. When upgrading an already imported copy of the old sample, remove its obsolete `AegisCustomRuleExample.cs` before importing the new version to avoid duplicate class definitions.
