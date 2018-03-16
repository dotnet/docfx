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


### 2.2 Generate Metadata
We use [ts-reference-ci-scripts](https://www.npmjs.com/package/ts-reference-ci-scripts) tool to generate YAML files.
```
npm install ts-reference-ci-scripts -g
```

Now we can extract metadata from TypeScript source code:
```
ts-reference-ci-scripts azure-iot-sdk-node/device/core --destPathWithoutSuffix yml
```
This scirpt will find all `package.json` under `azure-iot-sdk-node/device/core` folder, and generate metadata from TypeScript source code under it.

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