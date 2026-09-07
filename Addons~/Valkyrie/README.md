# Astra Aegis — Valkyrie Integration

An optional **Astra integration**. Install its prerequisites explicitly; the base
packages remain usable independently. Astra labels in this working copy will ship
with the next release; existing published tags retain their earlier labels.

Optional package:

```text
https://github.com/MisterPxl/Aegis.git?path=/Addons~/Valkyrie#v0.2.0
```

Adds Aegis rules for:

- missing values on fields marked with Valkyrie `[Required]`;
- null or unresolved managed references in polymorphic collections.

This integration does not replace Valkyrie's global inspector.

For local development and validation, register the core and integration from separate directories, matching the layout of a Git package installation. Unity can fail to associate scripts with their assemblies when one local package is physically nested inside another.

## Prerequisites and tests

The manifest declares these package versions:

- `com.misterpxl.aegis`: `0.2.0`.
- `com.misterpxl.valkyrie`: `1.0.0`.

Install the Astra base packages explicitly in the consumer manifest, using the
Git URLs from their READMEs. Git packages are not fetched transitively from
version-only dependencies. Unity registry dependencies resolve normally.

Add `com.misterpxl.aegis.valkyrie` to the consumer manifest’s `testables` and run
its suites in Unity Test Runner.

## Removal

Remove project components, assets or code that reference this integration before
removing it through Package Manager. The base packages can remain installed.
