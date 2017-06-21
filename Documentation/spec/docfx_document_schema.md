# DocFX Document Schema Design Specification

## 1. Introduction
DocFX supports different [document processors](..\tutorial\howto_build_your_own_type_of_documentation_with_custom_plug-in.md) to handle different kinds of input. For now, if the data model changes a bit, a new document processor is needed, even most works in processors are the same.

DocFX Document Schema (abbreviated to `this schema` below) is introduced to address ths problem. This schema is a JSON media type for defining the structure of a DocFX document. This schema is intended to define manipulation, documentation, validation of the document data.

## 2. Conventions and Terminology
The key words "MUST", "MUST NOT", "REQUIRED", "SHALL", "SHALL NOT", "SHOULD", "SHOULD NOT", "RECOMMENDED", "MAY", and "OPTIONAL" in this document are to be interpreted as described in [RFC 2119](https://tools.ietf.org/html/rfc2119).

## 3. Overview
DocFX Document Schema is in [JSON](http://www.json.org/) format. It borrows most syntax from [JSON Schema](http://json-schema.org/), while it also introduces some other syntax to manuplating the data.

### 3.1 Validation
This schema can describe the structure of a DocFX document. 
[JSON schema validation](http://json-schema.org/latest/json-schema-validation.html) already defines many keywords. This schema starts from supporting limited keyword like `type`, `properties`.

### 3.2 Manipulation
This schema can describe how to handle each property of the document.

Some handler is predefined in DocFX, like markup a string using [DFM](..\spec\docfx_flavored_markdown.md), mark a property as an uid, include content from another source, etc. 

Some handler can be injected into the schema parsing process to accomplish some custom steps.

**TODO**: add definition for handler.

### 3.3 Integration with DocFX
Schema is passed to DocFX as part of the template. It MUST be put in the `schemas` sub folder under template folder. The file name is treated as the schema name, also known as the document type of a template.

For example, to add a schema for a document type `ManagedReference`, a schema file should added like `{templateFolder}\schemas\ManagedReference.json`.

Schema describes the object model. The format of the object model is not constrained. YAML and JSON should both be supported.

**TODO**: change to use YAML Mime to determine which schema to use.

## 4. General Considerations
* This schema will reuse keywords in JSON schema, but will not change the defition of the existing one.
* To distinguish with JSON schema, the new keywords introduced in this schema is prefixed with `d-`. `d` stands for DocFX.
* This schema can validate and manipulate single elements in place, but can not move the element to another place.

## 5. Validation keywords

### 5.1 type

Same as in JSON schema: http://json-schema.org/latest/json-schema-validation.html#rfc.section.6.25

> The value of this keyword MUST be either a string or an array. If it is an array, elements of the array MUST be strings and MUST be unique.
>
> String values MUST be one of the six primitive types ("null", "boolean", "object", "array", "number", or "string"), or "integer" which matches any number with a zero fractional part.
>
> An instance validates if and only if the instance is in any of the sets listed for this keyword.


### 5.2 properties

Same as in JSON schema: http://json-schema.org/latest/json-schema-validation.html#rfc.section.6.18

> The value of "properties" MUST be an object. Each value of this object MUST be a valid JSON Schema.
>
> This keyword determines how child instances validate for objects, and does not directly validate the immediate instance itself.
>
> Validation succeeds if, for each name that appears in both the instance and as a name within this keyword's value, the child instance for that name successfully validates against the corresponding schema.
>
> Omitting this keyword has the same behavior as an empty object.

### 5.3 items

Same as in JSON schema: http://json-schema.org/latest/json-schema-validation.html#rfc.section.6.9

> The value of "items" MUST be either a valid JSON Schema or an array of valid JSON Schemas.
>
> This keyword determines how child instances validate for arrays, and does not directly validate the immediate instance itself.
>
> If "items" is a schema, validation succeeds if all elements in the array successfully validate against that schema.
>
> If "items" is an array of schemas, validation succeeds if each element of the instance validates against the schema at the same position, if any.
>
> Omitting this keyword has the same behavior as an empty schema.

## 6. Manipulation keywords

### 6.1 d-contentType
The value of `d-contentType` MUST be a string. It defines how DocFX will manipulate the property and affect the build context.

The following values are allowed:
* `uid`: If the instance is a string, it defines a UID in the build context for reference.
* `href`: If the instance is a string and in relative path, it will be converted into path from docset root, and DocFX will update the path afterward.
* `uidReference`: If the instance is a string or an array of string, it references to a UID (some UIDS) in the build context.
* `markdown`: If the instance is a string, it will be marked up and updated.
* `includeFile`: if the instance is a string and in relative path, content of the targeted file will replace it in place.
* `includeMarkdownFile`: This is same as `includeFile`, in spite of that the content will be marked up before filled in.

**TODO** add some explanation for instance: http://json-schema.org/latest/json-schema-core.html#rfc.section.4.2

### 6.2 d-tags
The value of this keyword MUST be either a string or an array. If it is an array, elements of the array MUST be strings and MUST be unique.

If a processor need to do some operations on a specified property, while the location of the property differs with schemas, it's hard to share the code.
In this case, a tag can be defined to mark the property, then the processor can fetch the instances with defined tag from schema parser, and read or modify it.

### 6.3 d-overwrite
The value of "overwrite" MUST be a string. It specifies how DocFX uses the [overwrite files](..\tutorial\intro_overwrite_files.md) to overwrite the existing values.

The following values are allowed:
* `replace`: The value will be overwitten by the one specified in overwrite files.
* `key`: If the instance is a string, it will be used as the key of the current object. It can be used to identify the object to overwrite in an array.
* `merge`: If the instance is an object, it will be merged with the one in overwrite files. If there are duplicated keys, the one in overwrite files will overwrite the existing one.
* `ignore`: Mark the current instance cannot be overwritten.

For `object` and `array` type, the default behavior is `merge`. For other types, the default behavior is `replace`.

> [!Note]
> After applying the overwrite files, the model should be checked again to ensure that all the validation rules still satisfy.

## 7. Extension Keywords

While this schema defines some keywords for DocFX use, additional keywords can be added above it. 

The extension keywords are RECOMMENDED to has the form `{an charactor other than "d"}-{keyword}`, so that it can be easily distinguished from keywords defined in this schema and JSON schema. They can have any valid JSON format value.

## 8. Samples
Here's an sample of the schema. Assume we have the following YAML file:
```yaml
title: Web Apps Documentation
metadata:
  title: Azure Web Apps Documentation - Tutorials, API Reference
  meta.description: Learn how to use App Service Web Apps to build and host websites and web applications.
  services: app-service
  author: apexprodleads
  manager: carolz
  ms.service: app-service
  ms.tgt_pltfrm: na
  ms.devlang: na
  ms.topic: landing-page
  ms.date: 01/23/2017
  ms.author: carolz
sections:
- title: 5-Minute Quickstarts
  children:
  - text: .NET
    href: app-service-web-get-started-dotnet.md
  - text: Node.js
    href: app-service-web-get-started-nodejs.md
  - text: PHP
    href: app-service-web-get-started-php.md
  - text: Java
    href: app-service-web-get-started-java.md
  - text: Python
    href: app-service-web-get-started-python.md
  - text: HTML
    href: app-service-web-get-started-html.md
- title: Step-by-Step Tutorials
  children:
  - content: "Create an application using [.NET with Azure SQL DB](app-service-web-tutorial-dotnet-sqldatabase.md) or [Node.js with MongoDB](app-service-web-tutorial-nodejs-mongodb-app.md)"
  - content: "[Map an existing custom domain to your application](app-service-web-tutorial-custom-domain.md)"
  - html: "[Bind an existing SSL certificate to your application](app-service-web-tutorial-custom-SSL.md)"
```

In this sample, we want to use the JSON schema to describe the overall model structure. Further more, the `href` property need to be resolved from the relative path to the final href. The `content` property need to be marked up as a Markdown string. The `metadata` need to be tagged for further custom operations. We want to use `setion`'s `title` as the key for overwrite `section` array.

Here's the schema to describe these operations:

```json
{
    "type": "object",
    "properties": {
        "metadata": {
            "type": "object",
            "d-tags": "metadata"
        },
        "sections": {
            "type": "array",
            "items": {
                "type": "object",
                "properties": {
                    "children": {
                        "type": "array",
                        "items": {
                            "type": "object",
                            "properties": {
                                "href": {
                                    "type": "string",
                                    "d-contentType": "href"
                                },
                                "text": {
                                    "type": "string"
                                },
                                "content": {
                                    "type": "string",
                                    "d-contentType": "markup"
                                }
                            }
                        }
                    },
                    "title": {
                        "type": "string",
                        "d-overwrite": "key"
                    }
                }
            }
        },
        "title": {
            "type": "string"
        }
    }
}
```