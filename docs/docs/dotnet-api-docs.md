# Add .NET API Docs


Step2. Generate metadata for the C# project
----------------------
Execute `docfx metadata` under `D:\docfx_walkthrough\docfx_project`. `docfx metadata` is a subcommand registered in `docfx`, and it reads configuration in the `metadata` section of `docfx.json`. `[ "src/**.csproj" ]` in `metadata/src/files` tells `docfx` to search for `csproj` files in the `src` subfolder to generate metadata.

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

This generates several `YAML` files in the `api` folder. The `YAML` file contains the data model extracted from the C# source code file. YAML is the metadata format used in `docfx`.

```
|- HelloDocfx.Class1.InnerClass.yml
|- HelloDocfx.Class1.yml
|- HelloDocfx.yml
|- toc.yml
```

## Place members in TOC
