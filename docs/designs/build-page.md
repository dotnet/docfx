# Build page pipeline workflow

## Build page pipeline input

- markdown files (conceptual)
  - markdown content
  - metadata defined in yml header

- yml/json files (schema driven)
  - input model
  - metadata defined in `metadata` section

- config file (global metadata + file metadata)

## Build Metadata

- Get `Input Metadata` from file(`yml header` or `metadata section`) & config(`global/file metadata`)
  ```md
  ---
  ms.author: docfx
  author: docfx
  ms.updated_at: 8/13/2019
  ---

  markdown content
  ```

- Create `System Metadata` (including `document_id`, `git_content_url`...) based on `Input Metadata`
  ```json
  {
      "document_id": "abc",
      "git_content_url": "https://github.com/dotnet/docfx/docs/build-page.md",
      "locale": "en-us",
      "canonical_url": "https://docs.microsoft.com/docfx/build-page"
      ...
  }
  ```

- [SDP] Transform `Input Metadata` based on `Schema`

- Merge `System Metadata` into `Input Metadata` to create `Output Metadata`

## Build Page

- [Conceptual] Markup markdown files to create `Intermediate Model`
  ```json
  {
      "conceptual": "html content",
      "word_count": 5,
      "title": "title",
      "raw_title": "raw_title",
  }
  ```

- [SDP] Transform `Input Model` based on `Schema` to create `Intermediate Model`
  ```json
  {
      "metadata": 
      {
          "title": "title",
          "summary": "summary",
      },
      "xref":
      {
          "uid": "uid",
          "href": "resolved href",
          "display_name": "display name"
      }
  }
  ```
- Create `Output Model` based on the merging of:

    - `Intermediate Model`

    - `Output Metadata`

## Apply Template