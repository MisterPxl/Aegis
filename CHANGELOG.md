# Changelog

## Unreleased

- Fix nested Valkyrie `[Required]` validation by sharing inspector presence checks and messages, with serialized property paths and cycle-safe traversal.
- Require the Valkyrie 1.5 nested-inspector Editor helpers; add parity, inheritance, collection and asset reimport regressions.

- Adopt Astra display names, menus, integration terminology and shared documentation conventions. Package IDs and C# APIs remain unchanged.

- Fixed: update the Helios integration to the 2.x report artifact API and JSON MIME type.
- Fixed: require Helios 2.3.1 and exclude the integration's Runtime, Editor and tests when
  `HELIOS_DEBUGGER_DISABLE` is defined.
- Tests: add report materialization and snapshot isolation regression tests.
- Fixed: serialized property scans include hidden fields and terminate on cyclic managed references, including the Valkyrie integration.
- Fixed: built-in and Valkyrie rule classes have matching script filenames and stable metadata so rule assets survive saving and reimporting.
- Fixed: the Custom Rule sample is an Editor script with a matching filename and no longer enters Player compilation.
- Fixed: dashboard runs, selected-rule runs and synchronous runs share suppression filtering and effective rule counts; suppressing a selected finding also clears its stale selection.
- Fixed: JUnit exports use active findings and the configured failure threshold, distinguish execution errors from validation failures, and preserve skipped rules.
- Fixed: settings initialize all profiles before saving and migrate legacy Build/CI profiles without discarding configured filters, thresholds or disabled rules.
- Fixed: cancelled runs return an incomplete result and retain the last completed report; JSON, JUnit and the dashboard expose cancellation explicitly.
- Changed: rule execution exceptions return CLI exit code 4 and still export both reports, even when their diagnostic finding has been suppressed.
- Added: regression tests for rule asset persistence, cyclic/hidden data, suppressions, profile migration, cancellation and JUnit semantics.

## 0.2.0

- Fixed: scanning a scene already open in the editor no longer closes it (potential loss of unsaved work).
- Fixed: `MissingObjectReferenceRule` now scans references nested in structs, serialized classes and collections.
- Fixed: fallback rule discovery no longer leaks a new set of `ScriptableObject` instances on every run.
- Fixed: `AegisCli.Run` now exits with code 3 for an unknown `-aegisProfile` instead of silently using the Interactive profile.
- Fixed: JUnit `failures` attribute now matches the emitted `<failure>` elements.
- Fixed: `Run Selected Rule` merges its results into the last report instead of overwriting it.
- Fixed: built-in rules honour cancellation between assets, so `Cancel` responds without waiting for a full rule.
- Fixed: Helios build snapshot reuses the build gate report instead of re-validating the whole project, and stale snapshots left by failed builds are cleaned up on domain reload.
- Fixed: Helios runtime registration retries for 30 seconds when Helios initializes after scene load.
- Changed: finding fingerprints are computed from stable anchors (rule, code, asset path, object id, property path) instead of the message, so renames and count changes no longer invalidate suppressions. Existing suppressions must be re-created.
- Changed: dashboard fix buttons are disabled when no fix action is available (e.g. reports reloaded from disk).
- Changed: addons moved to `Addons~` so the root package no longer embeds them; install them with `?path=/Addons~/<Name>`.

## 0.1.0

- Initial Aegis validation framework.
- Added Project Health dashboard, build gate and CI runner.
- Added default rules for missing scripts, missing references, collection entries, build scenes, prefab integrity and duplicate Aegis keys.
- Added optional Valkyrie and Helios integrations.
