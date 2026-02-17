# Auto TOC Sample

This sample demonstrates the **auto-populate TOC** feature in docfx.

## Quick Start

```bash
cd samples/autotoc
docfx build
```

## Feature Overview

When you set `auto: true` in a `toc.yml` file:

| Behavior | Description |
|----------|-------------|
| Same folder files | Automatically added to TOC |
| Subfolders without `toc.yml` | Recursively included as nested items |
| Subfolders with `toc.yml` | Excluded (they manage their own TOC) |
| Existing items | Preserved, not duplicated |

## Sample Structure

```
autotoc/
├── docfx.json
├── toc.yml              ← auto: true (root TOC)
├── index.md             ← manually listed
├── getting-started.md   ← auto-added
├── guides/              ← no toc.yml
│   ├── installation.md  ← auto-added as nested item
│   └── configuration.md ← auto-added as nested item
├── tutorials/           ← has toc.yml (boundary)
│   ├── toc.yml          ← auto: true (independent TOC)
│   ├── beginner.md      ← auto-added to tutorials TOC
│   └── advanced.md      ← auto-added to tutorials TOC
└── reference/           ← has toc.yml (boundary)
    ├── toc.yml          ← auto: false (manual TOC)
    ├── api.md           ← manually listed
    └── unlisted.md      ← NOT in TOC (auto is off)
```

## Expected Result

### Root TOC (`toc.yml`)
After build, the root TOC will contain:
- Home (manually added)
- Getting Started (auto-added)
- Guides (auto-added folder)
  - Installation
  - Configuration

Note: `tutorials/` and `reference/` are NOT included because they have their own `toc.yml`.

### Tutorials TOC (`tutorials/toc.yml`)
- Beginner (auto-added)
- Advanced (auto-added)

### Reference TOC (`reference/toc.yml`)
- API Reference (manually added)

Note: `unlisted.md` is NOT included because `auto: false`.
