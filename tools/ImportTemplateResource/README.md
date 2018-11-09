# Import Docs Template Resource Files

## Description

This tool helps to import resource files from current `docs template` repo for **all supported locales** to `docfx template` project, resource files currently only includes `token` files

## Usages

`dotnet run ImportTemplateResource -- <source-template-url> <git-pat> [<locales>]`

- <source-template-url>: The source `docs template` repo remote url, like "https://github.com/Microsoft/templates.docs.msft", required.
- <git-pat>: The git `personal access token` to access `docs template` repo, required.
- <locales>: which locales you want to import, default is all locales supported by docs template.

## Example

`dotnet run ImportTemplateResource -- https://github.com/Microsoft/templates.docs.msft <git-pat>`

`dotnet run ImportTemplateResource -- https://github.com/Microsoft/templates.docs.msft <git-pat> en-us,ja-jp`
