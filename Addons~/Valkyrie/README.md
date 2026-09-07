# Aegis Valkyrie Integration

Optional package:

```text
https://github.com/MisterPxl/Aegis.git?path=/Addons~/Valkyrie#v0.2.0
```

Adds Aegis rules for:

- missing values on fields marked with Valkyrie `[Required]`;
- null or unresolved managed references in polymorphic collections.

This addon does not replace Valkyrie's global inspector.

For local development and validation, register the core and addon from separate directories, matching the layout of a Git package installation. Unity can fail to associate scripts with their assemblies when one local package is physically nested inside another.
