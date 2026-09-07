# Aegis 1.0 migration candidate

Compatibility update: the current Valkyrie integration 2.0 targets Valkyrie 2.x.
The original Aegis 1.0 qualification below used integration 1.0 and Valkyrie 1.5.
Use the current Astra catalog when upgrading both products; Aegis core and its
Helios integration retain their 1.x API.

This is the first Astra API migration, published on `codex/astra-foundation`.
It is not a tagged release. Install the exact candidate revision recorded in the
framework's `Tools/Astra/catalog.json`; install both Aegis integrations from that
same revision when needed. Aegis remains usable without either integration.

## Upgrade an existing project

1. Commit or back up the project, including assets and their `.meta` files,
   `ProjectSettings/AegisSettings.asset`, `Packages/manifest.json` and its lockfile.
2. Update Aegis and any installed Aegis integrations together to the 1.0 candidate.
   Keep package IDs `com.misterpxl.aegis`, `.aegis.helios` and `.aegis.valkyrie`.
   The integrations require Aegis 1.x. Their partners remain Helios 2.4.x and
   Valkyrie 1.5.x; use the exact post-tag revisions in the Astra catalog.
3. Update custom C# imports, fully qualified references, reflection strings and
   assembly references using the tables below. Recompile custom DLL consumers.
   Do not replace namespaces inside serialized YAML or regenerate `.meta` files.
4. Open Unity, check compilation, inspect existing rules/profiles and run Aegis.
   Verify disabled rules and suppressions still apply. Inspect assets containing
   managed references before saving them, then save and reopen the project.
5. Update CI to `-executeMethod Astra.Aegis.AegisCli.Run`. The obsolete
   `MisterPxl.Aegis.AegisCli.Run` wrapper remains available with the same arguments,
   JSON/JUnit exports and exit codes (0 success, 2 blocking findings,
   3 configuration error, 4 internal error).

The source API change is intentional. `MovedFrom(autoUpdateAPI: false, ...)`
records serialization provenance; it does not promise automatic C# rewrites or
binary compatibility. Apart from the CLI wrapper, old API namespaces are removed.

| Previous namespace | Current namespace |
|---|---|
| `MisterPxl.Aegis` | `Astra.Aegis` |
| `MisterPxl.Aegis.HeliosIntegration` | `Astra.Aegis.Integrations.Helios` |
| `MisterPxl.Aegis.ValkyrieIntegration` | `Astra.Aegis.Integrations.Valkyrie` |
| `Aegis.Samples` | `Astra.Aegis.Samples` |

Suffixes such as `.Editor` and `.Tests` are retained on namespaces.

| Previous assembly | Current assembly |
|---|---|
| `Aegis.Editor` | `Astra.Aegis.Editor` |
| `Aegis.Editor.Tests` | `Astra.Aegis.Tests.EditMode` |
| `Aegis.Helios.Runtime` | `Astra.Aegis.Integrations.Helios.Runtime` |
| `Aegis.Helios.Editor` | `Astra.Aegis.Integrations.Helios.Editor` |
| `Aegis.Helios.Tests` | `Astra.Aegis.Integrations.Helios.Tests.EditMode` |
| `Aegis.Valkyrie.Editor` | `Astra.Aegis.Integrations.Valkyrie.Editor` |
| `Aegis.Valkyrie.Editor.Tests` | `Astra.Aegis.Integrations.Valkyrie.Tests.EditMode` |

Assembly definition GUIDs and script GUIDs are preserved. String-based asmdef
references must be updated; GUID-based references retain their targets.
[identities.json](identities.json) inventories 64 moved package types and the
separate sample type. Imported samples belong to the consumer: retain their
existing GUIDs and assembly placement when applying the sample source update.

## Stable identities and data

Rule assets retain their GUID-based IDs. Built-in fallback rules and the two
Valkyrie rules explicitly retain their old namespace-based IDs, so profile
exclusions and finding fingerprints do not change when their C# names change.
The Custom Rule sample retains `Aegis.Samples.SceneNamingRule`.

If you rename a custom rule that is used without an asset, declare
`[AegisRuleIdentity("Your.Previous.Namespace.RuleType")]` on that exact rule class.
The attribute is not inherited, preventing subclasses from accidentally sharing
an ID. It does not override an asset GUID. If you rename a serialized custom
type, preserve its metadata and add its own Unity migration annotation; the
package cannot infer the history of consumer-owned classes.

Diagnostic codes, serialized field names, `ProjectSettings/AegisSettings.asset`,
`Library/Aegis` report paths and the Helios snapshot resource path stay unchanged.
Historical JSON does not need a namespace replacement.

## Legacy fixtures and validation

`Legacy~` contains immutable project-relative fixtures captured by Unity
6000.4.0f1 on macOS before migration, using Aegis
`3516a50157ee8c9b7515e41eb50a231f8a610594`, Helios
`7201ceb8d671c3b050d027ecc2692aa243ae26c1` and Valkyrie
`7f643420d29785e46a0c72410711f0db8096bdb8`. Both Aegis integrations used the
same old Aegis revision. `legacy-sha256.json` freezes the captured bytes.
The ignored Unity folder suffix prevents accidental import with the package.

The fixture includes eight configured rule assets, an Aegis managed-reference
cycle, a Valkyrie shared/cyclic graph, shared Unity object references, profiles,
a suppression, a historical finding JSON and a Helios snapshot JSON. The finding
fixture is one finding, not a complete historical validation report.

The framework provides `Tools/Astra/Migration/prepare_aegis_probe.py` and
`AegisMigrationProbe.cs`. In a disposable consumer with all three base packages,
both Aegis integrations and their test assemblies enabled, install the probe
with `--api current --fixtures <this-folder>/Legacy~`. Run `Verify`, `Resave`,
then `Verify` in three separate Unity processes. Missing managed-reference types
are checked with `SerializationUtility.HasManagedReferencesWithMissingTypes`.
Never install these settings fixtures into a real project.

The framework's `Documentation/Astra/AegisMigrationValidation.md` records the
test suites, optional-package matrices, CLI checks and representative build.
Coverage is Unity 6000.4.0f1 on macOS; Unity 6000.0, IL2CPP, inspector GUI and
arbitrary consumer-defined serialized types are outside this qualification.

## Rollback

Before any resave, restore the old package revisions and custom source changes.
After saving migrated assets, restore the backed-up assets, metadata and settings
along with the old source and package manifests. New serialized type identities
are not promised to load in the old packages. Do not reverse-migrate by text
replacement. Generated reports and snapshots may be regenerated from the restored
project; keep any reports required for historical comparisons.

Unity's [MovedFrom implementation](https://github.com/Unity-Technologies/UnityCsReference/blob/master/Runtime/Export/Scripting/APIUpdating/UpdatedFromAttribute.cs)
documents the serialization mapping, and its
[missing managed-reference API](https://docs.unity.cn/ScriptReference/SerializationUtility.HasManagedReferencesWithMissingTypes.html)
provides the integrity check used by the probe.
