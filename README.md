# Generate your API documentation with DocFX

[![Join the chat at https://gitter.im/dotnet/docfx](https://badges.gitter.im/dotnet/docfx.svg)](https://gitter.im/dotnet/docfx?utm_source=badge&utm_medium=badge&utm_campaign=pr-badge&utm_content=badge)

|            | Windows  |
| ---------- | --------- | ------
| **master** | [![masterstatus](http://docfx-ci-0.cloudapp.net/app/rest/builds/buildType:(id:DocfxCi_Master)/statusIcon)](http://docfx-ci-0.cloudapp.net/viewType.html?buildTypeId=DocfxCi_Master)
| **dev**    | [![devstatus](http://docfx-ci-0.cloudapp.net/app/rest/builds/buildType:(id:DocfxCi_DocfxCiForDevBranch)/statusIcon)](http://docfx-ci-0.cloudapp.net/viewType.html?buildTypeId=DocfxCi_DocfxCiForDevBranch)

## What is it?
DocFX makes it extremely easy to generate your developer hub, complete with API reference, landing page, and how-to.
There are currently two versions of the tool:

* Windows specific IDE version which uses .NET Framework and works with [VS 2015 community](https://www.visualstudio.com/en-us/downloads/download-visual-studio-vs.aspx)
* Cross platform console version which uses .NET Core and DNX

We currently support C# and VB projects. 

## How to build?
### Prerequisites
1. [VS 2015 community](https://www.visualstudio.com/en-us/downloads/download-visual-studio-vs.aspx) or above
2. [DNVM](http://docs.asp.net/en/latest/getting-started/installing-on-windows.html#install-the-net-version-manager-dnvm)

### Steps
1. `dnvm install 1.0.0-rc1-final`
2. Run `build.cmd` under `docfx` code repo

> Possible build issues
  1. *DNX.PackageManager not found*  
   Install http://www.microsoft.com/en-us/download/details.aspx?id=49442. Note that there are 2 msi to be installed.
  2. *Test failure with message `\r\n` not equal to `\n` for Windows*  
  Set `git config --global core.autocrlf true`

## How do I play with `docfx`?
Please refer to [Getting Started](http://dotnet.github.io/docfx/tutorial/docfx_getting_started.html).

## What's included?
File/Folder     | Description 
:----------     | :----------
LICENSE         | Project license information
README.md       | Introduction to the project
CONTRIBUTING.md | Contribution guidelines to how to contribute to the repo
Documentation   | Documentation project using `docfx` to produce the documentation site
src             | Source code for `docfx`
test            | Test cases for `docfx` using *xunit* test framework
tools           | Source code for tools used in code build and deployment

## How to Contribute
Check out the [contributing](CONTRIBUTING.md) page to see the best places to log issues and start discussions.
This project has adopted the code of conduct defined by the [Contributor Covenant](http://contributor-covenant.org/) to clarify expected behavior in our community.
For more information see the [.NET Foundation Code of Conduct](http://www.dotnetfoundation.org/code-of-conduct).

## License
DocFX is licensed under the [MIT license](LICENSE).

### .NET Foundation
The DocFX is supported by the [.NET Foundation](http://www.dotnetfoundation.org).
