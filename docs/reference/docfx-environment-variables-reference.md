# Environment Variables

## `DOCFX_KEEP_DEBUG_INFO`

If set true. Keep following debug info in output HTML. 
- `sourceFile`
- `sourceStartLineNumber`
- `sourceEndLineNumber`
- `jsonPath`
- `data-raw-source`
- `nocheck`

## `DOCFX_SOURCE_BRANCH_NAME`

Used to override git branch name.

## `DOCFX_SOURCE_REPOSITORY`

Used to override git organization and repository names.
It must be defined in the `{organization}/{repository_name}` format (e.g.`dotnet/docfx`).

## `DOCFX_NO_CHECK_CERTIFICATE_REVOCATION_LIST`

Used to disable CRL check.
This setting is intended to be used on offline environment.

## `DOCFX_PDF_TIMEOUT`

Maximum time in milliseconds to override the default [Playwright timeout](https://playwright.dev/docs/api/class-browsercontext#browser-context-set-default-timeout) for PDF generation.