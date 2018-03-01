# Introduction to Multiple Languages Support

## 1. Introduction

DocFX supports generate API documentation for C# and VB natively. However, it doesn't limit to these. DocFX is designed to support any languages. When generating document for a language, 2 steps is required: generating metadata and build documents from metadata.

## 2. Workflow

### 2.1 Generate Metadata

As different programming language has different tool written with different language to generate API documentation, this step is not included in DocFX core. We call the tool used as **Metadata Tool**.

As [UniversalReferenceDocumentProcessor](https://github.com/dotnet/docfx/tree/dev/src/Microsoft.DocAsCode.Build.UniversalReference) is used to process these metadata files, metadata tool should generate files according the processor's input schema. It needs:
1. in YAML format and ends with `.yml` or `.yaml`
2. has YamlMime `### YamlMime:UniversalReference` at the first line
3. confirms to the [data model defined for UniversalReference](https://github.com/dotnet/docfx/tree/dev/src/Microsoft.DocAsCode.DataContracts.UniversalReference)

Usually, a TOC should generate along with YAML files for easy navigation among files.

### 2.2 Build Document

The YAML files generated is used as input of DocFX. DocFX will build these YAML files to HTML pages.

## 3. Supported Languages
* [JavaScript](gen_doc_for_js.md)
* TypeScript (comming soon ...)
* Python (coming soon ...)