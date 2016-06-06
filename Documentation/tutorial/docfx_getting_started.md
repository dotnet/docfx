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
* Cross-platform support. We have both exe version that runs under Windows and a DNX version that runs cross platform.
* Integration with Visual Studio. You can seamlessly use *DocFX* within Visual Studio.
* Markdown extensions. We introduced *DocFX Flavored Markdown(DFM)* to help you write API documentation. DFM is *100%* compatible with *GitHub Flavored Markdown(GFM)* with some useful extensions, like *file inclusion*, *code snippet*, *cross reference*, and *yaml header*.
For detailed description about DFM, please refer to [DFM](../spec/docfx_flavored_markdown.md).


2. Use *DocFX* as a command-line tool
-----------------------

*Step1.* Download and unzip *docfx.zip* from https://github.com/dotnet/docfx/releases, extract it to a local folder, and add it to PATH so you can run it anywhere.

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

3. Use *DocFX* in Visual Studio
---------------

As a prerequisite, you need [Visual Studio 2015](https://www.visualstudio.com/downloads/download-visual-studio-vs) to use *DocFX* in IDE.

*Step1.* Open Visual Studio and create a C# project as your documentation project. You can create an empty *ASP.NET Web Application* since it has a built-in *preview* feature that can be used to preview the generated website easily.

*Step2.* Right click on the website project, and choose *Manage NuGet Packages...* to open the NuGet Package Manager. Search and install *docfx.msbuild* package.

*Step3.* Create a `.cs` class in the website project, make sure the class is `public`, for example:

```csharp
namespace WebApplication1
{
    public class Class1
    {
    }
}
```

*Step4.* Right click on the website project, and click *View* -> *View in Browser*, navigate to `/_site` sub URL to view your website!

4. Use *DocFX* under DNX
----------------
As a prerequisite, you need to install [DNVM](http://docs.asp.net/en/latest/getting-started/installing-on-windows.html#install-the-net-version-manager-dnvm) and [DNX](http://docs.asp.net/en/latest/getting-started/installing-on-windows.html#install-the-net-execution-environment-dnx).

*Step1.* `SET DNX_FEED=https://www.myget.org/F/aspnetrelease/api/v2/` as we depend upon the release version of ASP.NET 1.0.0-rc1.

*Step2.* `dnvm upgrade` to get the latest dnvm.

*Step3.* Add feed https://www.myget.org/F/aspnetrelease/api/v2/ to NuGet.config.
> For Windows, the NuGet config file is *%AppData%\NuGet\NuGet.config*.

> For Linux/OSX, the NuGet config file is *~/.config/NuGet/NuGet.config*.

Sample NuGet.config
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
As a prerequisite, you need to install [Microsoft Build Tools 2015](https://www.microsoft.com/en-us/download/details.aspx?id=48159).

*Step1.* `git clone https://github.com/dotnet/docfx.git` to get the latest code.

*Step2.* Run `build.cmd` under root folder.

*Step3.* Add `artifacts` folder to nuget source by in IDE:
  > Tools > NuGet Package Manager > Package Manager Settings > Package Sources

*Step4.* Follow steps in #2, #3, #4 to use *DocFX* in command-line, IDE or DNX.

6. A seed project to play with *DocFX*
-------------------------
Here is a seed project https://github.com/docascode/docfx-seed. It contains

1. A basic C# project under `src`.
2. Several conceptual files under `articles`.
3. An overwrite file to add extra content to API under `specs`.
4. `toc.yml` under root folder. It renders as the navbar of the website.
5. `docfx.json` under root folder. It is the configuration file that `docfx` depends upon.

> Tip:
  It is a good practice to seperate files with different type into different folders.

7. Q&A
-------------------------
1. Q: How do I quickly reference APIs from other APIs or conceptual files?
   A: Use `@uid` syntax.
2. Q: What is `uid` and where do I find `uid`?
   A: Refer to [Cross Reference](../spec/docfx_flavored_markdown.md#cross-reference) section in [DFM](../spec/docfx_flavored_markdown.md).
3. Q: How do I quickly find `uid` in the website?
   A: In the generated website, hit F12 to view source, and look at the title of an API. You can find `uid` in `data-uid` attribute.
