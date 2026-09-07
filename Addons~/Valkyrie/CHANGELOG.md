# Changelog

## 1.0.0 — migration candidate (unreleased)

- Move C# namespaces and assemblies to Astra.Aegis and Astra.Aegis.Integrations.
- Preserve script/assembly GUIDs and declare old type identities for Unity serialization.
- Preserve built-in rule IDs, diagnostic codes, profile settings and report formats.
- Update consumers and asmdef references together; the old CLI entry point remains as a forwarding wrapper.


## Unreleased

- Fix nested Valkyrie `[Required]` validation by sharing inspector presence checks and messages, with serialized property paths and cycle-safe traversal.
- Require the Valkyrie 1.5 nested-inspector Editor helpers; add parity, inheritance, collection and asset reimport regressions.

- Adopt Astra display names, menu paths and integration terminology.

Earlier integration releases were recorded in the parent Aegis changelog.
