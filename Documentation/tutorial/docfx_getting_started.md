Getting Started with *DocFX*
===============

1. What is *DocFX*
---------------

*DocFX* is an API documentation generator for .NET, and currently it supports C# and VB.
It generates API reference documentation from triple-slash comments in your source code.
It also allows you to use Markdown files to create additional topics such as tutorials and how-tos, and to customize the generated reference documentation.
*DocFX* builds a static HTML website from your source code and Markdown files, which can be easily hosted on any web servers (for example, *github.io*).
Also, *DocFX* provides you the flexibility to customize the layout and style of your website through templates.
If you are interested in creating your own website with your own styles, you can follow [how to create custom template](howto_create_custom_template.md) to create custom templates.

*DocFX* also has the following cool features:

* Integration with your source code. You can click "View Source" on an API to navigate to the source code in GitHub (your source code must be pushed to GitHub).
* Cross-platform support. We have exe version that runs under Windows. It can also runs cross platforms on Linux/macOS with Mono.
* Integration with Visual Studio. You can seamlessly use *DocFX* within Visual Studio.
* Markdown extensions. We introduced *DocFX Flavored Markdown(DFM)* to help you write API documentation. DFM is *100%* compatible with *GitHub Flavored Markdown(GFM)* with some useful extensions, like *file inclusion*, *code snippet*, *cross reference*, and *yaml header*.
For detailed description about DFM, please refer to [DFM](../spec/docfx_flavored_markdown.md).

> [!Warning]
> **Prerequisites** [Visual Studio 2017](https://www.visualstudio.com/downloads/) is needed for `docfx metadata` msbuild projects. It is not required when generating metadata directly from source code (`.cs`, `.vb`) or assemblies (`.dll`)

2. Use *DocFX* as a command-line tool
-----------------------

*Step1.* DocFX ships as a [chocolatey package](https://chocolatey.org/packages/docfx).
Install docfx through [Chocolatey](https://chocolatey.org/install) by calling `cinst docfx -y`.

Alternatively, you can download and unzip *docfx.zip* from https://github.com/dotnet/docfx/releases, extract it to a local folder, and add it to PATH so you can run it anywhere.

*Step2.* Create a sample project
```
docfx init -q
```

This command generates a default project named `docfx_project`.

*Step3.* Build the website
```
docfx docfx_project\docfx.json --serve
```

Now you can view the generated website on http://localhost:8080.

3. Use *DocFX* integrated with Visual Studio
---------------

*Step1.* Install the [`docfx.console`](https://www.nuget.org/packages/docfx.console/) (formerly `docfx.msbuild`) nuget package on the project that you want to document.
It add itself to the build targets and add the `docfx.json` configuration file along with other files.

*Step2.* Compile, a `_site` folder is generated with the documentation.

> [!NOTE]
> *Possible warning*:
> - *Cache is corrupted*: if your project targets multiple frameworks, you have to indicate one to be the main for the documentation, through the [`TargetFramework` property](https://github.com/dotnet/docfx/issues/1254#issuecomment-294080535) in `docfx.json`:
>
>      "metadata": [
>        {
>          "src": "...",
>          "dest": "...",
>          "properties": {
>            "TargetFramework": <one_of_your_framework>
>          }
>        },
>      ]


4. Use *DocFX* with a Build Server
---------------

*DocFX* can be used in a Continuous Integration environment.

Most build systems do not checkout the branch that is being built, but
use a `detached head` for the specific commit.  DoxFX needs the the branch name to implement the `View Source` link in the API documentation.

Setting the environment variable `DOCFX_SOURCE_BRANCH_NAME` tells DocFX which branch name to use.

Many build systems set an environment variable with the branch name.  DocFX uses the following:

- `APPVEYOR_REPO_BRANCH` - [AppVeyor](https://www.appveyor.com/)
- `BUILD_SOURCEBRANCHNAME` - [Visual Studio Team Services](https://www.visualstudio.com/team-services/)
- `CI_BUILD_REF_NAME` - [GitLab CI](https://about.gitlab.com/gitlab-ci/)
- `Git_Branch` - [TeamCity](https://www.jetbrains.com/teamcity/)
- `GIT_BRANCH` - [Jenkins](https://jenkins.io/)
- `GIT_LOCAL_BRANCH` - [Jenkins](https://jenkins.io/)

> [!NOTE]
> *Known issue in AppVeyor*: Currently `platform: Any CPU` in *appveyor.yml* causes `docfx metadata` failure. https://github.com/dotnet/docfx/issues/1078

5. Build from source code
----------------
As a prerequisite, you need:
- [Visual Studio 2017](https://www.visualstudio.com/vs/) with *.NET Core cross-platform development* toolset
- [Node.js](https://nodejs.org)

*Step1.* `git clone https://github.com/dotnet/docfx.git` to get the latest code.

*Step2.* Run `build.cmd` under root folder.

*Step3.* Add `artifacts` folder to nuget source by in IDE:
  > Tools > NuGet Package Manager > Package Manager Settings > Package Sources

*Step4.* Follow steps in #2, #3, #4 to use *DocFX* in command-line, IDE or .NET Core.

6. A seed project to play with *DocFX*
-------------------------
Here is a seed project https://github.com/docascode/docfx-seed. It contains

1. A basic C# project under `src`.
2. Several conceptual files under `articles`.
3. An overwrite file to add extra content to API under `specs`.
4. `toc.yml` under root folder. It renders as the navbar of the website.
5. `docfx.json` under root folder. It is the configuration file that `docfx` depends upon.

> [!Tip]
> It is a good practice to separate files with different type into different folders.

7. Q&A
-------------------------
1. Q: How do I quickly reference APIs from other APIs or conceptual files?
   A: Use `@uid` syntax.
2. Q: What is `uid` and where do I find `uid`?
   A: Refer to [Cross Reference](../spec/docfx_flavored_markdown.md#cross-reference) section in [DFM](../spec/docfx_flavored_markdown.md).
3. Q: How do I quickly find `uid` in the website?
   A: In the generated website, hit F12 to view source, and look at the title of an API. You can find `uid` in `data-uid` attribute.
