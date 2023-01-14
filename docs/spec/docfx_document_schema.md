# DocFX Document Schema v1.0 Specification

## 1. Introduction
DocFX supports different [document processors](../tutorial/howto_build_your_own_type_of_documentation_with_custom_plug-in.md) to handle different kinds of input. For now, if the data model changes a bit, a new document processor is needed, even most of the work in processors are the same.

DocFX Document Schema (abbreviated to *THIS schema* below) is introduced to address this problem. This schema is a JSON media type for defining the structure of a DocFX document. This schema is intended to **annotate**, **validate** and **interpret** the document data. 

## 2. Conventions and Terminology
The key words "MUST", "MUST NOT", "REQUIRED", "SHALL", "SHALL NOT", "SHOULD", "SHOULD NOT", "RECOMMENDED", "MAY", and "OPTIONAL" in this document are to be interpreted as described in [RFC 2119](https://tools.ietf.org/html/rfc2119).

## 3. Overview
DocFX Document Schema is in [JSON](http://www.json.org/) format. It borrows most syntax from [JSON Schema](http://json-schema.org/), while it also introduces some other syntax to manipulate the data.

### 3.1 Annotation
*THIS schema* is a JSON based format for the structure of a DocFX document.

### 3.2 Validation
[JSON schema validation](http://json-schema.org/latest/json-schema-validation.html) already defines many keywords. This schema starts from supporting limited keyword like `type`, `properties`.

### 3.3 Interpretation
Besides annotate and validate the input document model, *THIS schema* also defines multiple interpretations for each property of the document model.
For example, a property named `summary` contains value in Markdown format, *THIS schema* can define a `markup` interpretation for the `summary` property, so that the property can be marked using [DFM](../spec/docfx_flavored_markdown.md) syntax.

## 4. General Considerations
* *THIS schema* leverages JSON schema definition, that is to say, keywords defined in JSON schema keeps its meaning in *THIS schema* when it is supported by *THIS schema*.

## 5. Detailed Specification

### Format
The files describing DocFX document model in accordance with the DocFX document schema specification are represented as JSON objects and conform to the JSON standards. YAML, being a superset of JSON, can be used as well to represent a DocFX document schema specification file.

All field names in the specification are **case sensitive**.

This schema exposes two types of fields. Fixed fields, which have a declared name, and Patterned fields, which declare a regex pattern for the field name. Patterned fields can have multiple occurrences as long as each has a unique name.

By convention, the schema file is suffixed with `.schema.json`.

### Data Types
Primitive data types in *THIS schema* are based on [JSON schema Draft 6 4.2 Instance](http://json-schema.org/latest/json-schema-core.html#rfc.section.4.2)

### Schema
For a given field, `*` as the starting character in *Description* cell stands for **required**.

#### Schema Object
This is the root document object for *THIS schema*.

##### Fixed Field

| Field Name      | Type   | Description
|-----------------|--------|----------
| $schema         | string | `*`The version of the schema specification, for example, `https://dotnet.github.io/docfx/schemas/v1.0/schema.json#`.
| version         | string | `*`The version of current schema object.
| id              | string | It is best practice to include an `id` property as an unique identifier for each schema.
| title           | string | The title of current schema, `LandingPage`, for example. In DocFX, this value can be used to determine what kind of documents apply to this schema, If not specified, file name before `schema.json` of this schema is used. Note that `.` is not allowed.
| description     | string  | A short description of current schema.
| type            | string | `*`The type of the root document model MUST be `object`.
| properties      | [Property Definitions Object](#property-definitions-object) | An object to hold the schema of all the properties.
| metadata        | string | In `json-pointer` format as defined in http://json-schema.org/latest/json-schema-validation.html#rfc.section.8.3.9. The format for JSON pointer is defined by https://tools.ietf.org/html/rfc6901, referencing to the metadata object. Metadata object is the object to define the metadata for current document, and can be also set through `globalMetadata` or `fileMetadata` in DocFX. The default value for metadata is empty which stands for the root object.

##### Patterned Field

| Field Name | Type | Description
|------------|------|----------
| ^x-        | Any  | Allows extensions to *THIS schema*. The field name MUST begin with x-, for example, x-internal-id. The value can be null, a primitive, an array or an object.

#### Property Definitions Object
It is an object where each key is the name of a property and each value is a schema to describe that property. 

##### Patterned Field

| Field Name      | Type   | Description
|-----------------|--------|----------
| {name}       | [Property Object](#property-object) | The schema object for the `{name}` property

#### Property Object
An object to describe the schema of the value of the property.

##### Fixed Field

| Field Name      | Type   | Description
|-----------------|--------|----------
| title        | string | The title of the property.
| description  | string | A lengthy explanation about the purpose of the data described by the schema.
| default      | what `type` defined | The default value for current field.
| type         | string | The type of the root document model. Refer to [type keyword](#61-type) for detailed description.
| properties   | [Property Definitions Object](#property-definitions-object) | An object to hold the schema of all the properties if `type` for the model is `object`. Omitting this keyword has the same behavior as an empty object.
| items        | [Property Object](#property-object) | An object to hold the schema of the items if `type` for the model is `array`. Omitting this keyword has the same behavior as an empty schema.
| reference    | string | Defines whether current property is a reference to the actual value of the property. Refer to [reference](#62-reference) for detailed explanation.
| contentType  | string | Defines the content type of the property. Refer to [contentType](#63-contenttype) for detailed explanation.
| tags       | array  | Defines the tags of the property. Refer to [tags](#64-tags) for detailed explanation.
| mergeType      | string | Defines how to merge the property. Omitting this keyword has the same behavior as `merge`. Refer to [mergeType](#65-mergetype) for detailed explanation.
| xrefProperties  | array | Defines the properties of current object when it is cross referenced by others. Each item is the name of the property in the instance. Refer to [xrefProperties](#66-xrefproperties) for detailed description of how to leverage this property.

##### Patterned Field

| Field Name | Type | Description
|------------|------|----------
| ^x-        | Any  | Allows extensions to *THIS schema*. The field name MUST begin with x-, for example, x-internal-id. The value can be null, a primitive, an array or an object.


## 6. Keywords in detail

### 6.1 type

Same as in JSON schema: http://json-schema.org/latest/json-schema-validation.html#rfc.section.6.25

> The value of this keyword MUST be either a string or an array. If it is an array, elements of the array MUST be strings and MUST be unique.
>
> String values MUST be one of the six primitive types ("null", "boolean", "object", "array", "number", or "string"), or "integer" which matches any number with a zero fractional part.
>
> An instance validates if and only if the instance is in any of the sets listed for this keyword.

### 6.2 reference
It defines whether current property is a reference to the actual value of the property. The values MUST be one of the following:

| Value      | Description
|------------|-------------
| `none`     | It means the property is not a reference.
| `file`     | It means current property stands for a file path that contains content to be included.

### 6.3 contentType
It defines how applications interpret the property. If not defined, the behavior is similar to `default` value. The values MUST be one of the following:

| Value      | Description
|------------|-------------
| `default`  | It means that no interpretion will be done to the property.
| `uid`      | `type` MUST be `string`. With this value, the property name MUST be `uid`. It means the property defines a unique identifier inside current document model.
| `href`     | `type` MUST be `string`. It means the property defines a file link inside current document model. Application CAN help to validate if the linked file exists, and update the file link if the linked file changes its output path.
| `xref`     | `type` MUST be `string`. It means the property defines a UID link inside current document model. Application CAN help to validate if the linked UID exists, and resolve the UID link to the corresponding file output link.
| `file`     | `type` MUST be `string`. It means the property defines a file path inside current document model. Application CAN help to validate if the linked file exists, and resolve the path to the corresponding file output path. The difference between `file` and `href` is that `href` is always URL encoded while `file` is not.
| `markdown` | `type` MUST be `string`. It means the property is in [DocFX flavored Markdown](../spec/docfx_flavored_markdown.md) syntax. Application CAN help to transform it into HTML format.

### 6.4 tags
The value of this keyword MUST be an `array`, elements of the array MUST be strings and MUST be unique. It provides hints for applications to decide how to interpret the property, for example, `localizable` tag can help Localization team to interpret the property as *localizable*.

### 6.5 mergeType
The value of this keyword MUST be a string. It specifies how to merge two values of the given property. One use scenario is how DocFX uses the [overwrite files](../tutorial/intro_overwrite_files.md) to overwrite the existing values. In the below table, we use `source` and `target` to stands for the two values for merging.

The value MUST be one of the following:

| Value      | Description
|------------|-------------
| `key`      | If `key` for `source` equals to the one for `target`, these two values are ready to merge.
| `merge`    | The default behavior. For `array`, items in the list are merged by `key` for the item. For `string` or any value type, `target` replaces `source`. For `object`, merge each property along with its own `merge` value.
| `replace`  | `target` replaces `source`.
| `ignore`   | `source` is not allowed to be merged.

### 6.6 xrefProperties
The value of this keyword MUST be an array of `string`. Each `string` value is the property name of current object that will be exported to be [Cross Referenced](docfx_flavored_markdown.md#cross-reference) by others.
To leverage this feature, a new `xref` syntax with `template` attribute is support:
```html
<xref uid="{uid}" template="{path_of_partial_template}" />
```
For the parital template, the input model is the object containing properties `xrefProperties` defines.

For example, in the sample schema defined by [7. Samples](#7-samples), ` "xrefProperties": [ "title", "description" ],`, `title` and `description` are `xrefProperties` for uid `webapp`. A partial template to render this xref, for example, named `partials/overview.tmpl`, looks like:
```mustache
{{title}}: {{{description}}}
```
When someone references this uid using `<xref uid="webapp" template="partials/overview.tmpl"`, `docfx` expand this `xref` into the following html:
```html
Web Apps Documentation: <p>This is description</p>
```
In this way, users can not only *cross reference* others to get the target url, but also *cross reference* other properties as they like.

A common usage of this is the **Namespace** page in ManagedReference. The **Namespace** page shows a table of its **Classes** with the `summary` of the **Class**, with the help of `xrefProperties`, the source of truth `summary` is always from **Class**. For the **Namespace** page, it can, for example:
1. Define a `class.tr.tmpl` template: `<tr><td>{{name}}</td><td>{{{summary}}}</td></tr>`
2. The namespace `namespace.tmpl` template, use `xref` to render its children classes: 
    ```mustache
    {{#children}}
      <xref uid="{{uid}}" template="class.tr.tmpl" />
    {{/children}}
    ```

## 7. Samples
Here's an sample of the schema. Assume we have the following YAML file:
```yaml
### YamlMime:LandingPage
title: Web Apps Documentation
description: This is description
uid: webapp
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
  - content: "[Bind an existing SSL certificate to your application](app-service-web-tutorial-custom-SSL.md)"
```

In this sample, we want to use the JSON schema to describe the overall model structure. Further more, the `href` is a file link. It need to be resolved from the relative path to the final href. The `content` property need to be marked up as a Markdown string. The `metadata` need to be tagged for further custom operations. We want to use `section`'s `title` as the key for overwrite `section` array.

Here's the schema to describe these operations:

```json
{
    "$schema": "https://dotnet.github.io/docfx/schemas/v1.0/schema.json#",
    "version": "1.0.0",
    "id": "https://github.com/dotnet/docfx/schemas/landingpage.schema.json",
    "title": "LandingPage",
    "description": "The schema for landing page",
    "type": "object",
    "xrefProperties": [ "title", "description" ],
    "properties": {
        "metadata": {
            "type": "object",
            "tags": [ "metadata" ]
        },
        "uid": {
            "type": "string",
            "contentType": "uid"
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
                                    "contentType": "href"
                                },
                                "text": {
                                    "type": "string",
                                    "tags": [ "localizable" ]
                                },
                                "content": {
                                    "type": "string",
                                    "contentType": "markdown"
                                }
                            }
                        }
                    },
                    "title": {
                        "type": "string",
                        "mergeType": "key"
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

## 8. Q & A
1. DocFX fills `_global` metadata into the processed data model, should the schema reflect this behavior?
    * Decision: *NOT* include, this schema is for **input model**, use another schema for output model.
2. Is it necessary to prefix `d-` to every field that DocFX introduces in?
    * If keep `d-`
        * Pros:
            1. `d-` makes it straightforward that these keywords are introduced by DocFX
            2. Keywords DocFX introduces in will never duplicate with the one preserved by JSON schema
        * Cons:
            1. `d-` prefix provides a hint that these keywords are not *first class* keywords
            2. Little chance that keywords DocFX defines duplicate with what JSON schema defines, after all, JSON schema defines a finite set of reserved keywords.
            3. For example[Swagger spec](http://swagger.io/) is also based on JSON schema and the fields it introduces in has no prefix. 
    * Decision: *Remove* `d-` prefix.

