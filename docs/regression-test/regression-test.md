# Regression Test

## Introduction

Regression test runs docfx e2e build locally against real docs repo to:
- guard unexptected changes
- ensure expected changes

The test baseline is previous **commit build's output**, which is checked in [docfx.testdata](https://ceapex.visualstudio.com/Engineering/_git/docfx.testdata).  
Each repo has its dedicated branch with the same repo name. e.g. *repo*: [https://github.com/MicrosoftDocs/azure-docs-pr](https://github.com/MicrosoftDocs/azure-docs-pr). *test-data*: [azure-docs-pr](https://ceapex.visualstudio.com/Engineering/_git/docfx.testdata?path=%2F&version=GBazure-docs-pr&_a=contents)

For each PR build, the output will be compared with checked in baseline, and the diff will be commented along with the PR thread if any.

## Selected Docs Repos
Repos are selected for covering different docfx build functionalities. e.g.: the core markdown parser, yml SDP, versioning & xref, reference plugins, etc.

| page view | repos                         | version | loc | xref | conceptual | SDP conceptual | SDP Ref | multi<br/>docset | Ecma2Yaml | Maml2Yaml | JoinTOC | Split<br/>TOC | Rest | vsts | learn | JS TS | special |
|-----------|-------------------------------|---------|-----|------|------------|----------------|---------|------------------|-----------|-----------|---------|---------------|------|------|-------|-------|---------|
| 1         | azure-docs-pr                 |         |     |      | ✔          | ✔              |         |                  |           |           |         |               |      |      |       |       |         |
| 2         | sql-docs-pr                   | ✔       |     |      | ✔          |                |         |                  |           |           |         |               |      |      |       |       |         |
| 4         | docs                          | ✔       |     | ✔    | ✔          |                |         |                  |           |           |         |               |      |      |       |       |         |
| 5         | learn-pr                      |         |     |      |            |                |         |                  |           |           |         |               |      |      | ✔     |       |         |
| 8         | windowsserverdocs-pr          |         |     |      | ✔          |                |         | ✔                |           |           |         |               |      |      |       |       |         |
| 11        | VBA-Docs                      |         |     |      | ✔          |                |         |                  |           |           |         |               |      |      |       |       |         |
| 12        | azure-devops-docs-pr          | ✔       |     |      | ✔          |                |         |                  |           |           |         |               |      |      |       |       |         |
| 14        | AspNetCore.Docs               | ✔       |     |      | ✔          |                |         |                  |           |           |         |               |      |      |       |       |         |
| 15        | microsoft-365-docs-pr(.zh-cn) |         | ✔   |      |            |                |         |                  |           |           |         |               |      |      |       |       |         |
| 18        | powerbi-docs-pr(.de-DE)       |         | ✔   |      |            |                |         |                  |           |           |         |               |      |      |       |       |         |
| 21        | PowerShell-Docs               |         |     |      |            |                |         |                  |           | ✔         |         |               |      |      |       |       |         |
| 50+       | roslyn-docs-api               |         |     |      |            |                | ✔       |                  | ✔         |           |         | ✔             |      |      |       |       |         |
| 50+       | azure-docs-rest-apis          |         |     |      |            |                | ✔       |                  |           |           |         |               | ✔    |      |       |       |         |
| 50+       | mc-docs-pr                    |         |     |      |            |                |         |                  |           |           |         |               |      |      |       |       | ✔       |
| 50+       | dynamics365smb-devitpro       |         |     |      | ✔          |                |         |                  |           |           |         |               |      |      |       |       |         |
| NA        | DevSandBox                    |         |     |      |            | ✔              | ✔       |                  |           |           |         |               | ✔    |      |       |       |         |
| NA        | test                          |         |     |      |            | ✔              | ✔       |                  |           |           |         |               | ✔    |      |       |       |         |
| NA        | DocsRoot                      |         |     |      | ✔          |                |         |                  |           |           |         |               |      |      |       |       |         |
| NA        | windows-compatibility         |         |     |      |            |                |         |                  |           |           |         |               |      | ✔    |       |       |         |
| NA        | azure-docs-cli                |         |     |      |            | ✔              | ✔       |                  |           |           | ✔       |               |      |      |       |       |         |
| NA        | dataprep-dotnet-pr            |         |     |      |            |                | ✔       |                  | ✔         |           | ✔       | ✔             |      |      |       |       |         |
| NA        | azure-mediaplayer-typescript  |         |     |      |            |                | ✔       |                  |           |           |         |               |      |      |       | ✔     |         |
| NA        | quantum-docs-pr               |         |     |      |            |                |         |                  |           |           | ✔       |               |      |      |       |       | ✔       |

* the table is generated with: [itemName=csholmq.excel-to-markdown-table](https://marketplace.visualstudio.com/items?itemName=csholmq.excel-to-markdown-table)
* [source excel ./test-repos.xlsx](./test-repos.xlsx)