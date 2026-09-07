# Changelog

## Unreleased

- Prepare the Helios integration 0.3.0 candidate for Helios 2.4.0 lifecycle notifications.
- Replace the 30-second poller with a disposable passive registration that survives service restart and resets between Play sessions.
- Remove only the integration’s own provider and attachment on shutdown/disposal; add late-start, restart, teardown and no-domain-reload regressions.

- Adopt Astra display names, menu paths and integration terminology.
- Target Helios 2.3.1–2.x report artifacts and follow runtime stripping.

Earlier integration releases were recorded in the parent Aegis changelog.
