---
uid: sdk-compatibility
---

# SDK compatibility

<div id="sdk-compatibility-report">Loading SDK compatibility results… Enable JavaScript to view them.</div>

<details>
<summary>About these checks</summary>

These automated observations are **not a support guarantee**. Untested combinations have no compatibility claim.

- **Basic** checks a C# class's public type and method in the generated API metadata.
- **Razor** checks a component with `@inherits` and a code-behind override, including its generated inheritance and override relationship. Incomplete API metadata is not a pass.

Rows identify the SDK and **project target**. Each result column shows the DocFX version and **tool target framework**, not the project's framework or the installed runtime patch.

For check definitions, local commands, and CI publication details, see [Maintaining SDK compatibility checks](xref:sdk-compatibility-maintenance).

</details>
