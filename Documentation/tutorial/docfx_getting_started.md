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

As a prerequisite, you will need [Visual Studio 2015](https://www.visualstudio.com/downloads/download-visual-studio-vs) to use *DocFX* in IDE.

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

4. Build from source code
----------------

As a prerequisite, you will need [Latest .NET Command Line Interface](https://github.com/dotnet/cli) to run `build.cmd`

*Step1.* `git clone https://github.com/dotnet/docfx.git` to get the latest code.

*Step2.* Run `build.cmd` under root folder.

*Step3.* Add `artifacts` folder to nuget source by in IDE:
  > Tools > NuGet Package Manager > Package Manager Settings > Package Sources

*Step4.* Follow steps in #2, #3, #4 to use *DocFX* in command-line, IDE or DNX.

5. A seed project to play with *DocFX*
-------------------------
Here is a seed project https://github.com/docascode/docfx-seed. It contains

1. A basic C# project under `src`.
2. Several conceptual files under `articles`.
3. An overwrite file to add extra content to API under `specs`.
4. `toc.yml` under root folder. It renders as the navbar of the website.
5. `docfx.json` under root folder. It is the configuration file that `docfx` depends upon.

> Tip:
  It is a good practice to seperate files with different type into different folders.

6. Q&A
-------------------------
1. Q: How do I quickly reference APIs from other APIs or conceptual files?
   A: Use `@uid` syntax.
2. Q: What is `uid` and where do I find `uid`?
   A: Refer to [Cross Reference](../spec/docfx_flavored_markdown.md#cross-reference) section in [DFM](../spec/docfx_flavored_markdown.md).
3. Q: How do I quickly find `uid` in the website?
   A: In the generated website, hit F12 to view source, and look at the title of an API. You can find `uid` in `data-uid` attribute.
