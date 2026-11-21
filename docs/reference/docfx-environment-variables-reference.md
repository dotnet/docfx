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

## `DOCFX_SOURCE_REPOSITORY_URL`

Used to override git organization and repository names.
It must be defined in the `https://{host_name}/{organization}/{repository_name}` format (e.g.`https://github.com/dotnet/docfx`).

## `DOCFX_NO_CHECK_CERTIFICATE_REVOCATION_LIST`

Used to disable CRL check.
This setting is intended to be used on offline environment.

## `DOCFX_PDF_TIMEOUT`

Maximum time in milliseconds to override the default [Playwright timeout](https://playwright.dev/docs/api/class-browsercontext#browser-context-set-default-timeout) for PDF generation. After a page timed out loading, it is also how long the PDF waits for each of the page's answers, 30 seconds by default, before it leaves the page out; `0` waits without a timeout.

## `PLAYWRIGHT_NODEJS_PATH`

Custom Node.js executable path that will be used by the `docfx pdf` command.
By default, docfx automatically detect installed Node.js from `PATH`.
