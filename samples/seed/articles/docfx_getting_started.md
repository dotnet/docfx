Getting Started with `docfx`
===============

Getting Started
---------------

This is a seed. ![Seed](images/seed.jpg)

`docfx` is an API documentation generator for .NET, currently support C# and VB. It has the ability to extract triple slash comments out from your source code. What's more, it has syntax to link additional files to API to add additional remarks. `docfx` will scan your source code and your additional conceptual files and generate a complete HTML documentation website for you. `docfx` provides the flexibility for you to customize the website through templates. We currently have several embedded templates, including websites containing pure static html pages and also website managed by AngularJS.

* Click "View Source" for an API to route to the source code in GitHub (your API must be pushed to GitHub)
* `docfx` provide DNX version for cross platform use.
* `docfx` can be used within Visual Studio seamlessly. **NOTE** offical `docfx.msbuild` nuget package is now in pre-release version. You can also build your own with source code and use it locally.
* We support **Docfx Flavored Markdown(DFM)** for writing conceptual files. DFM is **100%** compatible with *Github Flavored Markdown(GFM)* and add several new features including *file inclusion*, *cross reference*, and *yaml header*.
