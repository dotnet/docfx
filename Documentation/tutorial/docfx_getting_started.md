Getting Started with `docfx`
===============

Getting Started
---------------

`docfx` is an API documentation generator for .NET, currently support C# and VB, as similar to JSDoc or Sphnix. It has the ability to extract triple slash comments out from your source code. What's more, it has syntax to link additional files to API to add additional remarks. `docfx` will scan your source code and your additional conceptual files and generate a complete HTML documentation website for you. `docfx` provides the flexibility for you to customize the website through templates. We currently have several embeded templates, including websites containing pure static html pages and also website managed by AngularJS. Of cause, if you are interested in creating your own website with your own styles, you can follow [how to create custom template](howto_create_custom_template.md) to create custom templates.

* Click "View Source" for an API to route to the source code in GitHub (your API must be pushed to GitHub)
* `docfx` provide DNX version for cross platform use.
* `docfx` can be used within Visual Studio seamlessly. **NOTE** offical `docfx.msbuild` nuget package is now in pre-release version. You can also build your own with source code and use it locally.
* We support **Docfx Flavored Markdown(DFM)** for writing conceptual files. DFM is **100%** compatible with *Github Flavored Markdown(GFM)* and add several new features including *file inclusion*, *cross reference*, and *yaml header*. For detailed description about DFM, please refer to [DFM](../spec/docfx_flavored_markdown.md).

Use `docfx.exe` directly
-----------------------
Download and unzip [docfx.zip](artifacts/docfx.v0.3.10.zip) to run `docfx.exe` directly!

### Quick Start
**Step1** Run
```
docfx.exe init
```
and follow the instructions to generate a `docfx.json` config file.

**Step2.** Run
```
docfx.exe
```
and `docfx.exe` will automatically read the `docfx.json` in current folder and generate a documentation website for you in `output folder` which you defined in `init` phase. By default, the website is under `_site` folder.


**TODO**: we are on the way to publish `docfx.exe` independently into our on-building website!

Use `docfx` under Visual Studio IDE
---------------
As a prerequisite, you will need [Visual Studio 2015](https://www.visualstudio.com/downloads/download-visual-studio-vs) to use `docfx` in IDE.
### Quick Start
**Step1.** Open Visual Studio and create a .csproj as your documenation project. You can use the **ASP.NET website project template** as it has built-in *PREVIEW* feature which we could leverage to preview the generated website easily.

**Step2.** Install Nuget Package `docfx.msbuild` prerelease version under *Package Manager Console*:
```
Install-Package docfx.msbuild -Pre
```
**Step3.** You will notice that *docfx.json* and *toc.yml* are automatically added to your project. Modify *docfx.json* to include projects and conceptual files that you want to generate documentations. For detailed syntax about *docfx.json*, please refer to [docfx.exe User Mannual](docfx.exe_user_manual.md).

**Step4.** Right click on the website project, and click View => **View in browser**, navigate to `/_site/tutorial/docfx_getting_started.html` sub url, and start navigating through web pages!

### Build from source code
**Step1.** `git clone` to get the latest code.

**Step2.** Run `build.cmd` under root folder

**Step3.** Add `artifacts` folder to nuget source by in IDE:
  > Tools > Nuget Package Manager > Package Manager Settings > Package Sources

**Step4.** Follow **Step1**~**Step4** in above **Quick Start** section to generate your documentation!

Use `docfx` under DNX
----------------
As a prerequisite, you will need to install [DNVM](http://docs.asp.net/en/latest/getting-started/installing-on-windows.html#install-the-net-version-manager-dnvm) and [DNX](http://docs.asp.net/en/latest/getting-started/installing-on-windows.html#install-the-net-execution-environment-dnx).
###Quick Start
* `dnvm upgrade` to get the latest dnvm.
* `dnu commands install docfx` to install `docfx` as a command
* `docfx Documentation/docfx.json` to build generate `docfx` project into a website under `_site` folder!

Please refer to [`docfx` User Manual](docfx.exe_user_manual.md) for detailed description of the usage of *docfx.json*.

Adding conceptual content
-------------------------
The layout of the project file structure you care about now are as follows:
```
|- api/
|- article/
|    |- about.md
|    |- index.md
|- toc.yml
```
* api/ is reserved for your API section. *Note that the api names are the property names specified in `xdoc.json`, `projects` section.*
* article/ is where you put the self-authored topics, which we call `conceptual` topics.
* `toc.yml` is the main table of contents file that generates the navbar you will see on the top. It looks like this:

```
- name  : Home
  href: articles/index.md
- name  : About
  href: articles/about.md
- name  : Api Documentation
  href: api/
```
Say I want to add another tutorial section, here's how I would go about it:
* Create another folder under article called tutorial, and some additional markdown files as well as another toc.yml file like so:

```
|- api/
|- article/
|-   |- tutorial/
|-   |-    |- overview.md
|-   |-    |- getting_started.md
|-   |-    |- lessons.md
|-   |-    |- creating_your_first_website.md
|-   |-    |- adding_your_own_content.md
|-   |-    |- incorporating_code_snippets.md
|-   |-    |- publishing_to_github_pages.md
|-   |-    |- toc.yml
|-   |- about.md
|-   |- index.md
|- toc.yml
```
* The root toc.yml file will have an additional entry for the tutorial section, and the system will know it's a section because there's a toc.yml in the tutorial folder.

```
- uid  : Home
  href: articles/index.md
- uid  : About
  href: articles/about.md
- uid  : Tutorial
  href: articles/tutorial/
  homepage: articles/tutorial/lessons.md
- uid  : Api Documentation
  href: api/
```

> NOTE that *homegage* is the default page of sections. And **folder** must be ended with `/`.

* In the tutorial section's toc.yml file I have the following:

```
- uid  : Overview
  href: overview.md
- uid  : Getting Started
  href: getting_started.md
- uid  : Lessons
  href: lessons.md
  items:
    - uid  : Creating Your First Website
      href: creating_your_first_website.md
    - uid  : Adding Your Own Content
      href: adding_your_own_content.md
    - uid  : Incorporating Code Snippets
      href: incorporating_code_snippets.md
    - uid  : Publishing to GitHub Pages
      href: publishing_to_github_pages.md
```

That's it, preview your website and you should see the newly created tutorial section.

A couple of additional details:
* You can use an external link for href.

Adding markdown to API reference
--------------------------------
To make authoring your API reference section easier we allow you to directly add a markdown section to any of your APIs. Create a markdown file and add the following header:

```
---
uid: system.string
---
your markdown goes here.
```
Where "uid" is the fully qualified name of your API.
> *NOTE*: a valid YAML header **MUST** be started by a blank line, YAML content **MUST** be wrapped by three dashes `---`, and YAML content **MUST** start with `uid: `.

When you build the project again, we will add the markdown section you just created into the reference content of the API. It will be added to the location right after summary and before declarations.

We recommend that all of your API reference markdown files go in the api/ folder, although technically you can put the markdown file anywhere.

###Linking to another API
Currently for linking to another API you simply add an "@" followed by the fully qualified name of your API. Currently we will need to preprocess this syntax so adding this will require a rebuild.