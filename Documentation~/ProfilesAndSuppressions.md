# Profiles and Suppressions

Aegis ships with three project profiles:

- `Interactive`: used by the dashboard.
- `Build`: used by the build gate.
- `CI`: used by `AegisCli.Run`.

Profiles define failure thresholds, folders, categories and disabled rule IDs. They store exceptions rather than exhaustive rule lists so new rules can participate automatically.

Suppressions use finding fingerprints and include a reason, author and optional expiration. Expired suppressions are ignored and findings reappear. An `expiresUtc` value that cannot be parsed is treated as expired.

Fingerprints are computed from stable identity fields: rule ID, finding code, asset path, global object ID and property path. The finding message is only used when none of those anchors is set, so renaming an object or changing a count in the message does not invalidate a suppression.
