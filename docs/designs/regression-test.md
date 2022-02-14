# Regression Test

## Introduction

Regression test runs docfx e2e build locally against real docs repo to:
- guard unexpected changes
- ensure expected changes

The test baseline is previous **commit build's output**, which is checked in [docfx.testdata](https://ceapex.visualstudio.com/Engineering/_git/docfx.testdata).  
Each repo has its dedicated branch with the same repo name. e.g. *repo*: [https://github.com/MicrosoftDocs/azure-docs-pr](https://github.com/MicrosoftDocs/azure-docs-pr). *test-data*: [azure-docs-pr](https://ceapex.visualstudio.com/Engineering/_git/docfx.testdata?path=%2F&version=GBazure-docs-pr&_a=contents)

For each PR build, the output will be compared with checked in baseline, and the diff will be commented along with the PR thread if any.

## Selected Docs Repos
Repos are selected for covering different docfx build functionalities. e.g.: the core markdown parser, yml SDP, versioning & xref, reference plugins, etc.

See [source excel ./regression-test-repos.csv](./regression-test-repos.csv) for details