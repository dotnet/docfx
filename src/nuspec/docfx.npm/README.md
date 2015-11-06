# docfx

> **NOTE**
> Currently only support running in Windows. Cross-platform support in under development.

`docfx` is a documentation generation tool for .NET source code and markdown files. It has the ability to extract triple slash comments out from your source code. What's more, it has syntax to link additional files to API to add additional remarks. `docfx` will scan your source code and your additional conceptual files and generate a complete HTML documentation website for you. `docfx` provides the flexibility for you to customize the website through templates.

`docfx` in npm is a `js` wrapper of `docfx.exe`.

## Quick start with `docfx`

**Step 1** Install `docfx` globally
```
npm install -g docfx
```

**Step 2** Init `docfx` config file following the instructions
```
docfx init
```

**Step 3** Run `docfx`, the documentation will be generated under the output folder you defined above.
```
docfx
```

## Furthur reference
Please refer to [docfx homepage](http://aspnet.github.io/docfx/index.html) for detailed information about docfx.
