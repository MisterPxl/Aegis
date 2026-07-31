# Creating Aegis Rules

Rules are `ScriptableObject` assets. Aegis discovers them with `AssetDatabase`, so adding a rule does not require modifying a registry, enum or switch.

1. Create a class derived from `AegisRuleAsset`.
2. Add `CreateAssetMenu`.
3. Create a rule asset in the project or package.
4. Run `Tools > Aegis > Project Health`.

Rules should emit findings through `IAegisFindingSink` and keep build/CI behavior side-effect free.
