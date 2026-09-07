# Astra Aegis — Valkyrie Integration

An optional **Astra integration**. Install its prerequisites explicitly; the base
packages remain usable independently. This is the **2.0.0 migration candidate**; it requires Aegis 1.x and uses the
`Astra.Aegis.Integrations` namespaces. Existing tags retain the old API.

Optional package:

```text
https://github.com/MisterPxl/Aegis.git?path=/Addons~/Valkyrie#codex/astra-foundation
```

Adds Aegis rules for:

- missing values on fields marked with Valkyrie `[Required]`;
- null or unresolved managed references in polymorphic collections.

This integration does not replace Valkyrie's global inspector.

For local development and validation, copy each package into its own `Packages/<package-id>` directory in an isolated consumer and remove its `file:` manifest override. Local external packages can compile while Unity fails to associate their `MonoScript` assets with their types; asset save/reimport tests must also pass. Do not register an integration physically nested inside its base package.

## Prerequisites and tests

The manifest declares these package versions:

- `com.misterpxl.aegis`: `1.0.0` (compatible 1.x).
- `com.misterpxl.valkyrie`: `2.0.0` (compatible 2.x; uses the Astra annotations and Editor APIs).

Install the Astra base packages explicitly in the consumer manifest, using the
Git URLs from their READMEs. Git packages are not fetched transitively from
version-only dependencies. Unity registry dependencies resolve normally.

Add `com.misterpxl.aegis.valkyrie` to the consumer manifest’s `testables` and run
its suites in Unity Test Runner.

## Required validation (unreleased)

The working integration uses Valkyrie’s Editor helpers to share the inspector’s
presence checks and messages. The validated Valkyrie source is commit `7f64342`
(on `codex/astra-conventions`), which includes the nested-inspector implementation;
the older `v1.5.0` tag alone does not identify those later fixes. The branch URL
above includes these changes; pin validated revisions before a release.

Validation traverses serialized nested classes, structs, lists, arrays and
polymorphic references, including private fields inherited from base classes.
Shared managed objects are visited once and cycles terminate. Findings keep the
`Aegis.Valkyrie.Required` code and use the actual serialized property path so the
inspector can locate the field. Nonserialized fields are excluded; hidden
serialized fields remain validated even though the inspector hides them.

As in the inspector, null object/exposed/managed references and empty strings
are missing. Whitespace-only strings and empty collections are accepted. A
collection-level `[Required]` does not implicitly annotate its elements; place
`[Required]` on fields in the element type to validate them. Custom messages are
preserved. The suite includes parity checks and real save/reimport coverage.

## Removal

Remove project components, assets or code that reference this integration before
removing it through Package Manager. The base packages can remain installed.
