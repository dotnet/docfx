# DocFX Document Schema Design Specification

## 1. Introduction
DocFX supports different [document processors](..\tutorial\howto_build_your_own_type_of_documentation_with_custom_plug-in.md) to handle different kind of input. For now, if the data model changes a bit, a new document processor is needed, even most works in processors are the same.

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

Some handler can be injected into the schema parsing process to acomplish some custom steps.

## 4. General Considerations
* This schema will reuses keywords in JSON schema, but will not change the defition of the existing one.
* This schema can validate and manipulate single elements in place, but will not reconstruct the object.

## 5. Validation keywords

### 5.1 type

Same as in JSON schema: http://json-schema.org/latest/json-schema-validation.html#rfc.section.6.25

> The value of this keyword MUST be either a string or an array. If it is an array, elements of the array MUST be strings and MUST be unique.

> String values MUST be one of the six primitive types ("null", "boolean", "object", "array", "number", or "string"), or "integer" which matches any number with a zero fractional part.

> An instance validates if and only if the instance is in any of the sets listed for this keyword.


### 5.2 properties

Same as in JSON schema: http://json-schema.org/latest/json-schema-validation.html#rfc.section.6.18

> The value of "properties" MUST be an object. Each value of this object MUST be a valid JSON Schema.

> This keyword determines how child instances validate for objects, and does not directly validate the immediate instance itself.

> Validation succeeds if, for each name that appears in both the instance and as a name within this keyword's value, the child instance for that name successfully validates against the corresponding schema.

> Omitting this keyword has the same behavior as an empty object.

## 6. Manipulation keywords

### 6.1 behavior
The value of "behavior" MUST be a string. It defines how DocFX will manipulate the property and affect the build context. The following value is allowed:
* `uid`: If the instance is a string, it will be marked as a UID (stands for unique identifier) in the current model, and add it to the document build context. The schema parser will also maintain a mapping between uid and the corresponding object, so that OverwriteDocumentProcessor can use UID to get an object from the schema parser, and merge the necessary keys.
* `href`: If the instance is a string and in relative path, it will be converted into path from docset root, and DocFX will update the path afterward.
* `uidReference`: If the instance is a string, it wlll be taken as a UID, and a depdency on it will be logeged in document build context.
* `markup`: If the instance is a string, it will be marked up and updated.
* `include`: if the instance is a string and in relative path, content of the targeted file will replace it in place.
* `markupInclude`: This is same as `include`, in spite of that the content will be marked up before fill in.

### 6.2 tags
The value of this keyword MUST be either a string or an array. If it is an array, elements of the array MUST be strings and MUST be unique.

If a processor need to do some operations on a specified property, while the location of the property differs with schemas, it's hard to share the code.
In this case, a tag can be defined to mark the property, then the processor can fetch the instances with defined tag from schema parser, and read or modify it.

## 7. Integration with DocFX
Schema is passed to DocFX as part of the template. It MUST be put in the `schemas` sub folder under template folder. The file name is treated as the schema name, also known as the document type of a template.
For example, to add a schema for a document type `ManagedReference`, a schema file should added like `{templateFolder}\schemas\ManagedReference.json`.