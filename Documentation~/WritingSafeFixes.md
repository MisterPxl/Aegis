# Writing Safe Fixes

Fixes are opt-in actions attached to findings.

- `Safe`: can run from `Fix All Safe`.
- `ReviewRequired`: requires a user confirmation.
- `Destructive`: requires a user confirmation and should never delete assets silently.

Before applying a fix, revalidate that the target asset and property still match the finding. Use `Undo`, mark modified objects dirty and save assets explicitly.
