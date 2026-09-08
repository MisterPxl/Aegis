# Changelog

## 2.0.0 — migration candidate (unreleased)

- Target Helios 3.x Astra namespaces and assemblies; update runtime, Editor and test version constraints together.
- Keep Aegis 1.x, snapshot serialization, report formats and passive lifecycle behavior.

## 1.0.0 — migration candidate (unreleased)

- Move C# namespaces and assemblies to Astra.Aegis and Astra.Aegis.Integrations.
- Preserve script/assembly GUIDs and declare old type identities for Unity serialization.
- Preserve built-in rule IDs, diagnostic codes, profile settings and report formats.
- Update consumers and asmdef references together; the old CLI entry point remains as a forwarding wrapper.


## Unreleased

- Prepare the Helios integration 0.3.0 candidate for Helios 2.4.0 lifecycle notifications.
- Replace the 30-second poller with a disposable passive registration that survives service restart and resets between Play sessions.
- Remove only the integration’s own provider and attachment on shutdown/disposal; add late-start, restart, teardown and no-domain-reload regressions.

- Adopt Astra display names, menu paths and integration terminology.
- Target Helios 2.3.1–2.x report artifacts and follow runtime stripping.

Earlier integration releases were recorded in the parent Aegis changelog.
