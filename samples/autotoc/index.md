# Auto TOC Sample

This sample demonstrates the **auto-populate TOC** feature in docfx.

## How it works

When you set `auto: true` in a `toc.yml` file, docfx will automatically:

1. Add all markdown files in the same folder to the TOC
2. Recursively include files from subfolders that don't have their own `toc.yml`
3. Stop at folders that have their own `toc.yml` (those folders manage their own TOC)

## Folder Structure

```
autotoc/
├── toc.yml              (auto: true - root TOC)
├── index.md             (manually added)
├── getting-started.md   (auto-added)
├── guides/              (no toc.yml - files auto-added as nested items)
│   ├── installation.md
│   └── configuration.md
├── tutorials/           (has toc.yml - manages its own TOC)
│   ├── toc.yml          (auto: true)
│   ├── beginner.md
│   └── advanced.md
└── reference/           (has toc.yml with auto: false - manual TOC)
    ├── toc.yml
    └── api.md
```

## Try it out

Run `docfx build` in this folder to see the auto-populated TOC in action!
