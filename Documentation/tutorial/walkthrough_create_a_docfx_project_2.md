Walkthrough Part II: Adding API Documentation to the Website
==========================

After completing [Walkthrough Part I: Generate a Simple Documentation Website](walkthrough_create_a_docfx_project.md), we build a website from a set of `.md` files. We call it **Conceptual Documentation**. In this walkthrough, we will learn to build website from .NET source code, which is called **API Documentation**. We will also integrate **Conceptual Documentation** and **API Documentation** into one website, so that we can navigate from **Conceptual** to **API**, or **API** to **Conceptual** seamlessly. Download the files used in this walkthrough [here](artifacts/walkthrough2.zip).

After completing walkthourgh part I, our `D:\docfx_walkthrough\docfx_project` folder is in the following structure:

```
|- index.md
|- toc.yml
|- articles
|    |- intro.md
|    |- details1.md
|    |- details2.md
|    |- details3.md
|    |- toc.yml
|- images
     |- details1_image.png
|- api
     |- index.md
     |- toc.yml
```

Step1. Add a C# project
---------------------------
1. Create a subfolder `src` under `D:\docfx_walkthrough\docfx_project`. Open *Visual Studio Community 2015* or above and create a **C# Class Library** `HelloDocfx` under folder `src`. In the `Class1.cs`, add some comments and methods to this class, as similar to:

```csharp
namespace HelloDocfx
{
    /// <summary>
    /// Hello this is **Class1** from *HelloDocfx*
    /// </summary>
    public class Class1
    {
        private InnerClass _class;
        public int Value { get; }

        /// <summary>
        /// This is a ctor
        /// </summary>
        /// <param name="value">The value of the class</param>
        public Class1(int value)
        {
            Value = value;
        }

        public double ConvertToDouble()
        {
            return Value;
        }

        /// <summary>
        /// A method referencing a inner class
        /// </summary>
        /// <param name="name">The name</param>
        /// <param name="inner">A inner class with type <seealso cref="InnerClass"/></param>
        public void SetInnerClass(string name, InnerClass inner)
        {
            inner.Name = name;
            _class = inner;
        }

        public class InnerClass
        {
            public string Name { get; set; }
        }
    }
}
```

Step2. Generate metadata for the C# project
----------------------
Call `docfx metadata` under `D:\docfx_walkthrough\docfx_project`. `docfx metadata` is a sub-command registered in `docfx`, it reads configuration in the `metadata` section from `docfx.json`. `[ "src/**.csproj" ]` in `metadata/src/files` tells `docfx` to search all the `csproj` from `src` subfolder to generate metadata.

```json
"metadata": [
    {
      "src": [
        {
          "files": [
            "src/**.csproj"
          ],
          "exclude": [
            "**/bin/**",
            "**/obj/**",
            "_site/**"
          ]
        }
      ],
      "dest": "api"
    }
  ]
```

Several `YAML` files will be generated into `api` folder. The `YAML` file contains the data model extracted from C# source code file. YAML is the metadata format used in `docfx`. [General Metadata Spec](http://dotnet.github.io/docfx/spec/metadata_format_spec.html) defines the general schema and [.NET Metadata Spec](http://dotnet.github.io/docfx/spec/metadata_dotnet_spec.html) defines the metadata schema for .NET languages that `docfx` can consume.
```
|- HelloDocfx.Class1.InnerClass.yml
|- HelloDocfx.Class1.yml
|- HelloDocfx.yml
|- toc.yml
```

Step3. Build and preview our website
----------------------------------------------------
Run command `docfx`. `docfx` reads `docfx.json` and execute subcommands defined in the config file one by one. Our `docfx.json` defines `metadata` and `build`, so by running `docfx`, we are actually excuting `docfx metadata` and `docfx build`, and thus generate the website.

Run `docfx serve _site`, and the website is now:
![Step3](images/walkthrough2_step3.png).

Conclusion
---------
In this walkthrough, we build a website containing both **Conceptual Documentation** and **API Documentation**. And the upcoming series of advanced walkthroughs, we will learn advanced concepts in `docfx`, such as *cross reference* between articles, *external reference* to other documentations, etc. We will also learn to customize our websites, from theme to layout to metadata extraction.

Read more
---------
* [Walkthrough Part I: Generate a Simple Documentation Website](walkthrough_create_a_docfx_project.md)

* [Walkthrough Advanced: Customize Your Website](advanced_walkthrough.md)