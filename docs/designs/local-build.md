# Local build

Local build allows users to build against a locally stored [MicrosoftDocs](https://github.com/MicrosoftDocs) repository.

## Scope

- Provide a tool that runs on both Windows and MacOS
- The installation of the tool should be isolated and simple
- Build against all branches in a local repository
- Build output contains errors, warnings, suggestions with deep links to local files
- Ability to build all files and only changed files
- If the build require access to protected resources, authenticate the user using Azure Active Directory
- Build result should be the same as server build, except where it doesn't make sense (E.g., resolving GitHub contributors locally would exhaust the users GitHub rate limit so I wouldn't fit for local build).

## User Experience

The initial MVP project can be a command line tool that satisfies the above requirements, the final product is an vscode extension that provides a seamless authoring experience in vscode.

### Phase 1: Command Line Tool

- Install latest [.NET Core SDK](https://dotnet.microsoft.com/download)
- Install docfx using `dotnet tool install -g docfx --version "3.0.0-*" --add-source https://www.myget.org/F/docfx-v3/api/v2`
- Open a [MicrosoftDocs](https://github.com/MicrosoftDocs) repository in vscode
- Open vscode integrated terminal
- Run `docfx build` to build all files
- Run `docfx build --changed` to build only changed files
- Errors, warnings, suggestions will show up in command line output with `ctrl` clickable links to source location
![](./images/local-build-cli-deep-link.png)
- If authentication is required, users are guided to sign in using the standard [Azure AD device login flow](https://docs.microsoft.com/en-us/azure/active-directory/develop/v2-oauth2-device-code).
  > The command line pauses and shows: _To sign in, use a web browser to open the page https://aka.ms/devicelogin and enter the code XXXX to authenticate._

### Phase 2: vscode Integration

- Install the offical [Docs Authoring Pack](https://marketplace.visualstudio.com/items?itemName=docsmsft.docs-authoring-pack), local build extension is bundled as part of it.
- In vscode command palette, choose `Docs: Build All Files` to build everything, choose `Docs: Build Changed Files` to build only files that are changed
![](./images/local-build-vscode-commands.png)
- Errors, warnings, suggestions will show up as standard errors in vscode `Problems Window` as well as squiggles in files.
![](./images/local-build-vscode-diagnostics.png)
- If authentication is required, users are prompted to sign in using the standard Azure AD login flow.
![](./images/local-build-vscode-sign-in.png)
- Build may need to download some external dependencies for the first time or occationally afterwards, users are prompted and build will then automatically download these dependcencies.
![](./images/local-build-vscode-restore.png)

## Technical Design

 