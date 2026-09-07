# Creating Aegis Rules

Rules are `ScriptableObject` assets. Aegis discovers them with `AssetDatabase`, so adding a rule does not require modifying a registry, enum or switch.

1. Create a class derived from `AegisRuleAsset` in an `Editor` folder or an Editor-only assembly. Use one rule class per script, with a filename matching the class name.
2. Add `CreateAssetMenu`.
3. Create a rule asset in the project or package.
4. Run `Tools > Astra > Aegis > Project Health`.

Rules should emit findings through `IAegisFindingSink` and keep build/CI behavior side-effect free.

Use `AegisSerializedProperties.Enumerate(serializedObject)` when inspecting serialized properties. It includes hidden fields and visits the children of each managed reference only once, including cyclic graphs. The yielded cursor is reused; call `Copy()` if you need to retain a property after the next iteration.

`Evaluate` runs synchronously on the editor thread. The interactive frame budget schedules whole rules; it does not preempt a rule that is already executing. Check `context.IsCancellationRequested` in custom loops when cancellation can be requested programmatically.

Earlier versions saved built-in and Valkyrie rule assets without a valid script reference. Correct filenames prevent new broken assets, but existing assets with a missing script must be repaired or recreated; this update does not rewrite project assets automatically.
