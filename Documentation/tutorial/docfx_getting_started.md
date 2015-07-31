Getting Started with `docfx`
===============

Getting Started
---------------

`docfx` is an API documentation generator for .NET, currently support C# and VB, as similar to JSDoc or Sphnix. It has the ability to extract triple slash comments out from your source code. What's more, it has syntax to link additional files to API to add additional remarks. `docfx` will scan your source code and your additional conceptual files and generate a complete HTML documentation website for you. The website is currently written in AngularJS, but `docfx` provides the flexibility for you to customize the website through specifying templates.

* Click "View Source" for an API to route to the source code in GitHub (your API must be pushed to GitHub)
* `docfx` provide DNX version for cross platform use. **NOTSUPPORTED** FOR NOW: DNX version of `docfx` can only generate YAML metadata files.
* `docfx` can be used within Visual Studio seamlessly. **NOTE** offical `docfx.msbuild` nuget package is on the way to publish, you can now build your own with source code and use it locally.

Use `docfx` under Visual Studio IDE
---------------
As a prerequisite, you will need [Visual Studio 2015](https://www.visualstudio.com/downloads/download-visual-studio-vs) to use `docfx` in IDE.
### Quick Start
* `git clone` to get the latest code.
* Run `build.cmd` under root folder
* Add `artifacts` folder to nuget source by in IDE:
  > Tools > Nuget Package Manager > Package Manager Settings > Package Sources
* Create an empty ASP.NET website. (Actually any project will work, we use ASP.NET website as it has built-in *PREVIEW* feature so that we can preview the generated website easily.)
* Install Nuget Package `docfx.msbuild` prerelase version.
	
  Under *Package Manager Console*
	> `Install-Package docfx.msbuild -Pre`
* You will notice that inside the project, default files *xdoc.json*, *toc.yml* are automatically added.
* Preview the website, and navigate to `/_site` url.

Use `docfx` under DNX
----------------
As a prerequisite, you will need to install [DNVM](http://docs.asp.net/en/latest/getting-started/installing-on-windows.html#install-the-net-version-manager-dnvm) and [DNX](http://docs.asp.net/en/latest/getting-started/installing-on-windows.html#install-the-net-execution-environment-dnx).
###Quick Start
* `dnvm upgrade` to get the latest dnvm.
* `dnu commands install docfx` to install `docfx` as a command
* `docfx Documentation/xdoc.json` to build generate `docfx` project into a website under `_site` folder!

Please refer to [`docfx` User Manual](docfx.exe_user_manual.md) for detailed description of the usage of *xdoc.json*.

Adding conceptual content
-------------------------
The components of the project you care about now are as follows:
```
api/
article/
  about.md
  index.md
toc.yml
```
* api/ is reserved for your API section. *Note that the api names are the property names specified in `xdoc.json`, `projects` section.*
* article/ is where you put all of the self-authored topics.
* toc.yml is the main table of contents file that generates the navbar you will see on the top. It looks like this:

```
- uid  : Home
  href: articles/index.md
- uid  : About
  href: articles/about.md
- uid  : Api Documentation
  href: api
```
Say I want to add another tutorial section, here's how I would go about it:
* Create another folder under article called tutorial, and some additional markdown files as well as another toc.yaml file like so:

```
api/
article/
  tutorial/
    overview.md
    getting_started.md
    lessons.md
    creating_your_first_website.md
    adding_your_own_content.md
    incorporating_code_snippets.md
    publishing_to_github_pages.md
    toc.yml
  about.md
  index.md
toc.yml
```
* The root toc.yml file will have an additional entry for the tutorial section, and the system will know it's a section because there's a toc.yml in the tutorial folder.

```
- uid  : Home
  href: articles/index.md
- uid  : About
  href: articles/about.md
- uid  : Tutorial
  href: articles/tutorial
- uid  : Api Documentation
  href: api
  homepage: lessons.md
```

> NOTE that *homepage* is the index page of sections.

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

###Including code samples from source
Use below syntax to include code samples in markdown files.

````
```csharp
{{'<code_source_file_path>'[<start_line>-<end_line>]}}
```
````