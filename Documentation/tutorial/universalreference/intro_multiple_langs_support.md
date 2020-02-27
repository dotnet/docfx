# Introduction to Multiple Languages Support

## 1. Introduction

DocFX supports generating API documentation for C# and VB natively. However, it's not limited to these. DocFX is designed to support any language. When generating documents for a language, two steps are required: generating metadata and building documents from the metadata.

## 2. Workflow

### 2.1 Generate Metadata

As different programming language has different tool written with different language to generate API documentation, this step is not included in DocFX core. We call the tool used as **Metadata Tool**.

As [UniversalReferenceDocumentProcessor](https://github.com/dotnet/docfx/tree/dev/src/Microsoft.DocAsCode.Build.UniversalReference) is used to process these metadata files, the metadata tool should generate files according the processor's input schema. The files should:
1. be in YAML format and end with `.yml` or `.yaml`
2. have YamlMime `### YamlMime:UniversalReference` as the first line
3. conform to the [data model defined for UniversalReference](https://github.com/dotnet/docfx/tree/dev/src/Microsoft.DocAsCode.DataContracts.UniversalReference)

Usually, a TOC should be generated along with YAML files for easy navigation among files.

### 2.2 Build Document

The YAML files generated are used as input to DocFX. DocFX will build these YAML files into HTML pages.

## 3. Supported Languages
* [JavaScript](gen_doc_for_js.md)
* TypeScript (coming soon ...)
* Python (coming soon ...)
