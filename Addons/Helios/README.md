# Aegis Helios Integration

Optional package:

```text
https://github.com/MisterPxl/Aegis.git?path=/Addons/Helios#v0.1.0
```

During validated builds, the editor side generates a minimal `AegisValidationSnapshot` resource. At runtime, the Helios side exposes that snapshot through System Info and attaches the same JSON to bug reports.

The snapshot contains only counts and status. It does not embed asset paths, finding messages or full validation reports.
