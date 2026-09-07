# Profiles and Suppressions

Aegis ships with three project profiles:

- `Interactive`: used by the dashboard.
- `Build`: used by the build gate.
- `CI`: used by `AegisCli.Run`.

Profiles define failure thresholds, folders, categories and disabled rule IDs. They store exceptions rather than exhaustive rule lists so new rules can participate automatically.

Suppressions use finding fingerprints and include a reason, author and optional expiration. Expired suppressions are ignored and findings reappear. An `expiresUtc` value that cannot be parsed is treated as expired.

All runners and the dashboard apply suppressions to both findings and per-rule counts. Suppressing a selected finding updates the current report and clears its selection. Removing a suppression requires a new run to recover findings omitted from the previous report.

Settings initialize all three profiles before saving. Legacy Build/CI profiles incorrectly named `Interactive` are migrated on access: their names and accidental default frame budgets are corrected, while thresholds, folders, categories, disabled rule IDs and non-default budgets are preserved. The next settings save persists the migration.

Cancelled runs have `IsCancelled = true`, return an unsuccessful `AegisRunResult`, and do not overwrite `Library/Aegis/last-report.json`. Their partial findings can still be inspected or exported; JSON preserves `_isCancelled` and JUnit contains a cancellation error. The dashboard labels the partial result as incomplete.

Fingerprints are computed from stable identity fields: rule ID, finding code, asset path, global object ID and property path. The finding message is only used when none of those anchors is set, so renaming an object or changing a count in the message does not invalidate a suppression.
