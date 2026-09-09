# Changelog

## 2.1.0 - 2026-09-09

- Show Aegis findings inside the Valkyrie inspector through the Valkyrie composition point:
  per-object summary with provenance and age, help boxes under the concerned fields, unattached
  findings with `Locate`, targeted validation of the owning asset/scene, cached and invalidated on
  modification, import, newer report and play mode changes.
- Add the optional Tween Configuration Rule for Valkyrie DOTween: pre-build detection of
  missing/mistyped bindings, invalid steps, empty sequences and stale step event bindings,
  without building tweens; runtime-provided bindings are distinguished from invalid
  configurations. Compiled only when `com.misterpxl.valkyrie.dotween` 3.x is installed.

## 2.0.0 - 2026-09-09

- Reference Astra Valkyrie annotations and Editor APIs; constrain this integration to Valkyrie 2.x.
- Preserve rule classes, GUIDs, fallback IDs and serialized data; Aegis remains at 1.x.


## 1.0.0 - 2026-09-09

- Move C# namespaces and assemblies to Astra.Aegis and Astra.Aegis.Integrations.
- Preserve script/assembly GUIDs and declare old type identities for Unity serialization.
- Preserve built-in rule IDs, diagnostic codes, profile settings and report formats.
- Update consumers and asmdef references together; the old CLI entry point remains as a forwarding wrapper.

- Fix nested Valkyrie `[Required]` validation by sharing inspector presence checks and messages, with serialized property paths and cycle-safe traversal.
- Require the Valkyrie 1.5 nested-inspector Editor helpers; add parity, inheritance, collection and asset reimport regressions.

- Adopt Astra display names, menu paths and integration terminology.

Earlier integration releases were recorded in the parent Aegis changelog.
