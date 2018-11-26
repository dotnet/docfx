# Generate API Documentation for JavaScript

## 1. Prerequisite

* [DocFX](https://dotnet.github.io/docfx/tutorial/docfx_getting_started.html#2-use-docfx-as-a-command-line-tool)
* [Node.js](https://nodejs.org/en/download/) (includes npm)

## 2. Steps

### 2.1 Prepare Source Code
Prepare the JavaScript source code for generating document. In this tutorial, we take [azure-batch](https://www.npmjs.com/package/azure-batch) as an example
```
npm install azure-batch
```

### 2.2 Generate Metadata
We use [Node2DocFX](https://www.npmjs.com/package/node2docfx) tool to generate YAML files.
```
npm install node2docfx
```

Create the `node2docfx.json` for the tool configuration:
```json
{
  "source": {
    "include": ["node_modules/azure-batch/lib"]
  },
  "destination": "yml"
}
```
With this config, the tool will read source code under `node_modules/azure-batch/lib`, and extract metadata to YAML files under `yml` folder:
```
node node_modules/node2docfx/node2docfx.js node2docfx.json
```

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
Now you can see your generated pages: http://localhost:8080/api/Account.html