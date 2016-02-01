Getting Started with `docfx`
===============

1. Getting Started
---------------

`docfx` is an API documentation generator for .NET, currently support C# and VB, as similar to JSDoc or Sphinx. It has the ability to extract triple slash comments out from your source code. What's more, it has syntax to link additional files to API to add additional remarks. `docfx` will scan your source code and your additional conceptual files and generate a complete HTML documentation website for you. `docfx` provides the flexibility for you to customize the website through templates. We currently have several embedded templates, including websites containing pure static html pages and also website managed by AngularJS. Of cause, if you are interested in creating your own website with your own styles, you can follow [how to create custom template](howto_create_custom_template.md) to create custom templates.

* Click "View Source" for an API to route to the source code in GitHub (your API must be pushed to GitHub).
* `docfx` provide DNX version for cross platform use.
* `docfx` can be used within Visual Studio seamlessly.
* **Docfx Flavored Markdown(DFM)** is introduced to write conceptual files. DFM is **100%** compatible with *Github Flavored Markdown(GFM)* and add several new features including *file inclusion*, *cross reference*, and *yaml header*. For detailed description about DFM, please refer to [DFM](../spec/docfx_flavored_markdown.md).


2. Use `docfx.exe` directly
-----------------------
Download and unzip [docfx.zip] from https://github.com/dotnet/docfx/releases to run `docfx.exe` directly!

### 2.1 Quick Start
**Step1.** Run
```
docfx init -q
```

A `docfx_project` default project will be generated.

**Step2.** Run
```
docfx docfx_project\docfx.json --serve
```

And you can view the generated website on http://localhost:8080.


3. Use `docfx` under Visual Studio IDE
---------------
As a prerequisite, you will need [Visual Studio 2015](https://www.visualstudio.com/downloads/download-visual-studio-vs) to use `docfx` in IDE.
### 3.1 Quick Start
**Step1.** Open Visual Studio and create a .csproj as your documentation project. You can use the **ASP.NET website project template** as it has built-in *PREVIEW* feature which we could leverage to preview the generated website easily.

**Step2.** Install Nuget Package `docfx.msbuild` within *Package Manager Console*:
```
Install-Package docfx.msbuild
```
**Step3.** You will notice that *docfx.json* and *toc.yml* are automatically added to your project. Modify *docfx.json* to include projects and conceptual files that you want to generate documentations. For detailed syntax about *docfx.json*, please refer to @user_manual.

**Step4.** Right click on the website project, and click View => **View in browser**, navigate to `/_site/tutorial/docfx_getting_started.html` sub url, and start navigating through web pages!

### 3.2 Build from source code
**Step1.** `git clone` to get the latest code.

**Step2.** Run `build.cmd` under root folder

**Step3.** Add `artifacts` folder to nuget source by in IDE:
  > Tools > Nuget Package Manager > Package Manager Settings > Package Sources

**Step4.** Follow **Step1**~**Step4** in above **Quick Start** section to generate your documentation!

4. Use `docfx` under DNX
----------------
As a prerequisite, you will need to install [DNVM](http://docs.asp.net/en/latest/getting-started/installing-on-windows.html#install-the-net-version-manager-dnvm) and [DNX](http://docs.asp.net/en/latest/getting-started/installing-on-windows.html#install-the-net-execution-environment-dnx).
### 4.1 Quick Start
* `SET DNX_FEED=https://www.myget.org/F/aspnetrelease/api/v2/` as we depends on the release version of aspnet 1.0.0-rc1.
* `dnvm upgrade` to get the latest dnvm.
* Add feed https://www.myget.org/F/aspnetrelease/api/v2/ to Nuget.config
  > For Windows, the nuget config file is  **%AppData%\NuGet\NuGet.config**.

  > For Linux/OSX, the nuget config file is **~/.config/NuGet/NuGet.config**.

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
* `dnu commands install docfx` to install `docfx` as a command
* `docfx Documentation/docfx.json` to build generate `docfx` project into a website under `_site` folder!

Please refer to [`docfx` User Manual](docfx.exe_user_manual.md) for detailed description of the usage of *docfx.json*.


5. A seed project to play with `docfx`
-------------------------
Here is a seed project https://github.com/docascode/docfx-seed. It contains
1. A basic cs project under `src`
2. Several conceptual files under `articles`
3. An override file to add extra content to API under `specs`
4. `toc.yml` under root folder. It will be rendered as the navbar of the website.
5. `docfx.json` under root folder. It is the configuration file that `docfx` depends on.

> Tips
  It is a good practice to seperate files with different type into different folders.

6. Q&A
-------------------------
1. Q: How to quickly reference APIs from other APIs or conceptual files?
   A: Use `@uid` syntax.
2. Q: What is `uid` and where to find `uid`?
   A: Refer to `Cross Reference` section in [DFM](../spec/docfx_flavored_markdown.md).
3. Q: Where to find `uid`?
   A: In the generated website, F12 to view source, `uid` is the value from `data-uid` attribute.