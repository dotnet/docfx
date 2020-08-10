# Local build

Local build allows users to build against a locally stored [MicrosoftDocs](https://github.com/MicrosoftDocs) repository.

## Scope

- Provide a vscode extension that runs on both Windows and MacOS
- Build against all branches in a local repository
- Build output contains errors, warnings, suggestions with deep links to local files
- If the build require access to protected resources, authenticate the user
- Build result should be the same as server build, except where it doesn't make sense (E.g., resolving GitHub contributors locally would exhaust users GitHub rate limit).

## Build MicrosoftDocs repos

`docfx` is the tool to build any MicrosoftDocs repo. Ideally, a user could clone a repo, run `docfx build` against it and get build result locally. But currently `docfx` does not understand `.openpublishing.config.json` and `docfx.json`. On the build server, this step is done though a config migration process written in JavaScript. Providing v2 config backward compatibility allows us to:

- Report the correct line info for diagnostics against v2 config file
- Allow local build using `docfx build`
- Removed NodeJS dependency

### Config

There are 3 sources of config that affects build: config at rest, environment specific configs and server configs.

#### Config at Rest

These are the config files stored in git repos (`docfx.json`, `.openpublishing.config.json`, `.openpublishing.redirection.json`). `docfx` need to provide backward compatibility support of these v2 configuration schemas.

#### Environment specific Config

Environment specific configs are global per environment (local build or server build). They are passed in as environment variables or command line options. E.g., github access token for server build.

#### Server Configs

Some configs are MicrosoftDocs specific and are controlled by a separate service. They are not checked into repos. Examples include metadata validation rules, xref, base path, product name.

##### Retrieving server configs

`docfx` calls a web API to retrieve these configs. The API endpoint is specified in `docfx.yml` using the `use` (currently named `extend`) property:

```yml
# docfx.yml
name: Docs.azure-documents
use: https://api.docs.com/config
files: '**/*.md'
xref:
- https://api.docs.com/xref/dotnet
- https://api.docs.com/xref/java
```

> For backward compatibility with v2 configs, we automatically add `use: https://api.docs.com/config` whenever `.openpublishing.config.json` file exists

The response is a JSON object that confirms to `docfx.yml` configuration schema.

_Example Request:_

```
GET /config/docfx
```

_Example Response:_
```json
{
   "monikerDefinition": "https://api.docs.com/config/monikerDefinition",
   "metadataSchema": "https://api.docs.com/config/metadataSchema",
   "rules": {
     "h1-missing": {
       "severity": "error"
     }
   }
}
```

##### Config parameterization

Server side config is different between repos, docsets or locals. `docfx` parameterizes outgoing HTTP requests using HTTP headers:

name | example
----|----
`Docfx-Name` | Docs.azure-documents
`Docfx-Locale` | en-us
`Docfx-Repository-Url` | https://github.com/MicrosoftDocs/azure-docs
`Docfx-Repository-Branch` | master

> An alternative is to pass these parameters explicitly use URL parameters, such design may produce tedious config like `https://api.docs.com/config/docfx?name={name}&locale={locale}&repository_url={repository_url}&repository_branch={repository_branch}` and it is hard to version.

> The practice of using `X-` as custom http headers is __deprecated__, thus our headers are prefixed by `Docfx_`.

##### Config Authentication

`docfx` access protected resources using standard HTTP headers as described in [Credential.md](./credential.md). The config service exposed by build service uses the current OPS token based authentication.

## vscode Extension

The docfx vscode extension provides rich authoring tools when working with docs repos inside vscode. The initial phase is to provide a simple command that can trigger a build and show diagnostics in vscode problems window. As we progress, we can light up more advanced features like providing diagnostics as typing, providing intellisense, etc.

### Acquisition

The vscode extension is initially a standalone extension, but will be bundled into `docs-authoring-pack` after getting matured.

Users don't need to install anything particular after installing the vscode extension. This is done by publishing docfx as a self contained, platform specific executable to a well known blob storage location. The vscode extension downloads and unzips latest docfx for the current platform on `extension activation`. This allows the vscode extension to ship independently from docfx and ensure the latest version of docfx is used.

### Trigger Build

Handling build command is divided into two phases given the time constraint.

#### Phase 1

For the initial phase, the vscode extension calls `docfx build` and `docfx restore` directly, parses generated report files and presents them to problems window. It pipelines `docfx` console output to a specific output channel.

#### Phase 2

To enable rich interaction between vscode and docfx, we'll based of all vscode extension features on top of [LSP (Language Server Protocol)](https://langserver.org/) in phase 2. 

It allows advanced features like intellisense and refactoring. 

For local build, switch to LSP also allows us to consolidate all authoring features onto the same pipeline and enables advanced features like error log streaming, progress reporting, on demand authentication and on demand restore.

LSP defines the [standard communication protocol](https://microsoft.github.io/language-server-protocol/specifications/specification-3-14/) using [JSON RPC](https://www.jsonrpc.org/specification), a simple, light-weight JSON based remote procedure call protocol as the base protocol. To integrate LSP with vscode, language servers are launched as a separate process and communicate with vscode using standard input and standard output streams.

The vscode extension calls `docfx serve --language-server` to start docfx as a local server. The server exposes APIs to interact with language clients, it also implements contracts specified in Language Service Protocols to support publishing diagnostics as typing, providing intellisense as typing...

> The reason to use `docfx serve --language-server` as the command over `docfx watch` is that watching file changes is done in language clients (vscode) for [good reasons](https://microsoft.github.io/language-server-protocol/specification#workspace_didChangeWatchedFiles). If there is need to watch file system changes without a language client (vscode), we can choose to add a `--watch` flag to `docfx serve` that enables file system watching.

To support local build, `docfx` exposes these custom APIs:

#### `docfx/build`

Triggers a build of the current workspace. Error, warning and suggestions are communicated back to vscode using `textDocument/publishDiagnostics` notification.

_params_:

```csharp
{
  // when true, only publish diagnostics without producing any output files on disk, this can significantly speed up build speed
  "checksOnly": true
}
```

### Authentication

OPS service authentication is done in vscode by combining AAD and GitHub login to retrieve OPS token. OPS token is stored securely in KeyChain on MacOS and Credential Manager on Windows. We currently don't handle on-demand authenticate in the initial phase. On-demand authentication is considered after implementing LSP.
