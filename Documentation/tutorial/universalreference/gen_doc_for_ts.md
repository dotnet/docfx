# Generate API Documentation for TypeScript

## 1. Prerequisite

* [DocFX](https://dotnet.github.io/docfx/tutorial/docfx_getting_started.html#2-use-docfx-as-a-command-line-tool)
* [Node.js](https://nodejs.org/en/download/) (includes npm)
* [Git](https://git-scm.com/)

## 2. Steps

### 2.1 Prepare Source Code
Prepare the TypeScript source code for generating document. In this tutorial, we take [azure-iot-device](https://github.com/Azure/azure-iot-sdk-node/tree/master/device/core) as an example.
```
git clone https://github.com/Azure/azure-iot-sdk-node.git
```


### 2.2 Generate Metadata for a package
We use [typedoc](http://typedoc.org/) tool and [type2docfx](https://www.npmjs.com/package/type2docfx) to generate YAML files.

First, let's install the tools globally.
```
npm install -g typedoc type2docfx
```

#### 2.2.1 TypeDoc to parse source code into a JSON format output
Go to the folder where package.json file locate.
Run
```
typedoc --json api.json azure-iot-sdk-node/device/core/src --module commonjs --includeDeclarations --ignoreCompilerErrors --excludeExternals
```

The parameter may differ for your needs. You can use `typedoc -h` to explore more options.


#### 2.2.2 Type2docfx to extract the JSON format output into YAML files
Find the output `api.json` file and run:
```
type2docfx api.json yml
```
The `yml` stands for the output folder, you can specify the folder as the content publishing folder in Section 2.3. And you can explore more option by `type2docfx -h`. With `--sourceUrl, --sourceBranch, and --basePath` parameters, you can generate yaml files referencing to the source code in Github, which will help developer to find the corresponding source code easily.

> [!NOTE]
>
> All sources under `node_modules` path will be automatically ignored.

### 2.3 Build Document
Create the configuration `docfx.json` for DocFX:
```json
{
  "build": {
    "content": [
      {
        "files": ["**/*.yml"],
        "src": "yml",
        "dest": "api"
      }
    ],
    "dest": "_site"
  }
}
```

More information about `docfx.json` can be found in [user manual](https://dotnet.github.io/docfx/tutorial/docfx.exe_user_manual.html). Run:
```
docfx docfx.json --serve
```
Now you can see your generated pages: http://localhost:8080/api/azure-iot-device/Client.html#azure_iot_device_Client

## 3. Know issues
1. Some types can't link to the property correctly now. They displays in plain text and prefixed with `@`. 
