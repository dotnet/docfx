# Roadmap

## The Present State

Docfx is the core compiler to power the content pipeline of [Microsoft Docs](https://learn.microsoft.com). Docfx v3 has gained enough features and is stable enough to support the need for Microsoft Docs. We have completely retired docfx v2 internally for Microsoft Docs and are willing to make docfx v3 publicly consumable by the open source community.

However, there are a few architectural differences between Microsoft Docs and an open-source community release. We still have a bit of work to do before it is publicly consumable. Until then, we recommend users to keep using docfx v2.

## The Gap

The architectural difference between Microsoft Docs and a community release requires additional investments in the following areas:

- **Static Site**: Microsoft Docs is not a pure static website, while most of the core contents are served statically, a substantial portion of site features are either dedicated micro-services or rendered dynamically. Features like site search, versioning needs to be built separately for the community release.

- **API Reference**: Microsoft Docs uses a separate set of tools to ingest API references for a variety of programming languages in addition to C#. While it is different from the `metadata` command in docfx v2, it presents an opportunity for us to support more programming languages in docfx v3 and converge into one API reference pipeline. These ingestion tools today requires deep knowledge and complex configuration to work with. Work is needed to consolidate and simplify the pipeline setup for the community release.

- **PDF**: Microsoft Docs supports PDF files. The tooling for PDF today consumes docfx v3 build output but is not directly wired to docfx. Work is needed to consolidate and simplify the pipeline setup for the community release.

- **Site Template**: Microsoft Docs uses a monolith site template suited for its dynamic rendering architecture, which may or may not be reusable for the community release. There is a basic working [site template for docfx v3](https://github.com/docascode/template) migrated from the docfx v2. Work is needed to improve this template for the community release.

## The Future Plan

We are wiling to ship a formal v3 release for the open-source community. There are just other priorities that have taken precedence over this work. We will keep this roadmap updated once there is more information to share.
