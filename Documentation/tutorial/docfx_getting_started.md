Getting Started with *DocFX*
===============

1. What is *DocFX*
---------------

*DocFX* is an API documentation generator for .NET, currently it supports C# and VB.
It has the ability to generate API reference documentation from triple slash comments of your source code.
What's more, it allows you to use markdown files to add additional content to the generated documentation.
*DocFX* builds a static HTML web site from your source code and markdown files, which can be easily hosted on any web servers, for example, *github.io*.
*DocFX* also provides you the flexibility to customize the layout and style of your web site through templates.
If you are interested in creating your own web site with your own styles, you can follow [how to create custom template](howto_create_custom_template.md) to create custom templates.

*DocFX* also has the following cool features:

* Integrated with your source code. You can click "View Source" on an API to navigate to the source code in GitHub (your source code must be pushed to GitHub).
* Cross platform. We have both exe version that runs under Windows and a DNX version that runs cross platform.
* Integrated with Visual Studio. *DocFX* can be used within Visual Studio seamlessly.
* Markdown extensions. *DocFX Flavored Markdown(DFM)* is introduced to help you write API documentation. DFM is *100%* compatible with *Github Flavored Markdown(GFM)* with some useful extensions, like *file inclusion*, *code snippet*, *cross reference*, and *yaml header*.
For detailed description about DFM, please refer to [DFM](../spec/docfx_flavored_markdown.md).


2. Use *DocFX* as a command-line tool
-----------------------

*Step1.* Download and unzip *docfx.zip* from https://github.com/dotnet/docfx/releases and extract to a local folder, add it to PATH so you can run it anywhere.

*Step2.* Create a sample project
```
docfx init -q
```

A default project named `docfx_project` will be generated.

*Step3.* Build the web site
```
docfx docfx_project\docfx.json --serve
```

Now you can view the generated web site on http://localhost:8080.

3. Use *DocFX* in Visual Studio
---------------

As a prerequisite, you will need [Visual Studio 2015](https://www.visualstudio.com/downloads/download-visual-studio-vs) to use *DocFX* in IDE.

*Step1.* Open Visual Studio and create a C# project as your documentation project. You can use the *ASP.NET Web Application* as it has built-in *preview* feature which can be used to preview the generated web site easily.

*Step2.* Install Nuget Package `docfx.msbuild` within *Package Manager Console*:
```
Install-Package docfx.msbuild
```

*Step3.* Right click on the website project, and click *View* -> *View in Browser*, navigate to `/_site` sub url to view your web site!

4. Use *DocFX* under DNX
----------------
As a prerequisite, you will need to install [DNVM](http://docs.asp.net/en/latest/getting-started/installing-on-windows.html#install-the-net-version-manager-dnvm) and [DNX](http://docs.asp.net/en/latest/getting-started/installing-on-windows.html#install-the-net-execution-environment-dnx).

*Step1.* `SET DNX_FEED=https://www.myget.org/F/aspnetrelease/api/v2/` as we depends on the release version of aspnet 1.0.0-rc1.

*Step2.* `dnvm upgrade` to get the latest dnvm.

*Step3.* Add feed https://www.myget.org/F/aspnetrelease/api/v2/ to Nuget.config.
> For Windows, the nuget config file is *%AppData%\NuGet\NuGet.config*.

> For Linux/OSX, the nuget config file is *~/.config/NuGet/NuGet.config*.

Sample nuget.config
```xml
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <add key="myget.release" value="https://www.myget.org/F/aspnetrelease/api/v2/" />
    <add key="nuget.org" value="https://www.nuget.org/api/v2/" />
  </packageSources>
  <disabledPackageSources />
  <activePackageSource>
    <add key="nuget.org" value="https://www.nuget.org/api/v2/" />
  </activePackageSource>
</configuration>
```

*Step4.* `dnu commands install docfx` to install *DocFX* as a command.

*Step5.* `docfx init -q` to generate a sample project.

*Step6.* `docfx docfx_project\docfx.json --serve` to build your project and preview your site at http://localhost:8080.

Please refer to [*DocFX* User Manual](docfx.exe_user_manual.md) for detailed description of `docfx.json`.

5. Build from source code
----------------

*Step1.* `git clone https://github.com/dotnet/docfx.git` to get the latest code.

*Step2.* Run `build.cmd` under root folder.

*Step3.* Add `artifacts` folder to nuget source by in IDE:
  > Tools > Nuget Package Manager > Package Manager Settings > Package Sources

*Step4.* Follow steps in #2, #3, #4 to use *DocFX* in command-line, IDE or DNX.

6. A seed project to play with *DocFX*
-------------------------
Here is a seed project https://github.com/docascode/docfx-seed. It contains

1. A basic C# project under `src`.
2. Several conceptual files under `articles`.
3. An override file to add extra content to API under `specs`.
4. `toc.yml` under root folder. It will be rendered as the navbar of the website.
5. `docfx.json` under root folder. It is the configuration file that `docfx` depends on.

> Tips
  It is a good practice to seperate files with different type into different folders.

7. Q&A
-------------------------
1. Q: How to quickly reference APIs from other APIs or conceptual files?
   A: Use `@uid` syntax.
2. Q: What is `uid` and where to find `uid`?
   A: Refer to [Cross Reference](../spec/docfx_flavored_markdown.md#cross-reference) section in [DFM](../spec/docfx_flavored_markdown.md).
3. Q: How to quickly find `uid` in web site?
   A: In the generated web site, F12 to view source, look at the title of an API, you can `uid` in `data-uid` attribute.