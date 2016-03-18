Introduction to REST API Documentation
===============================

Introduction
-------------

DocFX now supports generating documentation from REST APIs following [Swagger specification](http://swagger.io/specification/) version 2.0.

The Swagger RESTful API files *MUST* end with `.swagger.json` or `.swagger2.json` or `_swagger.json` or `_swagger2.json` so that these files are recognized as REST API files in DocFX.

One Swagger API file generates one HTML file. For example. a file `contacts.swagger.json` generates file naming `contacts.html`.

Basic structure
--------------
A single Swagger API file is considered as a unique REST **File** containing multiple **API**s. The **UID**(Unique IDentifier) for the **File** is defined as the combination of `host`, `basePath`, `info.title` and `info.version` with `/` as seperator. For example, the following Swagger API file has **UID** equals to `microsoft.com/docfx/Contacts/1.6`:

```json
{
  "swagger": "2.0",
  "info": {
    "title": "Contacts",
    "version": "1.6"
  },
  "host": "microsoft.com",
  "basePath": "/docfx",
  "schemes": [
    "https"
  ]
}
```

A REST API **File** contains multiple **API**s as its children. An **API** is an [Operation Object](http://swagger.io/specification/#operationObject) defined in [Path Item Object](http://swagger.io/specification/#pathItemObject). The **UID**(Unique IDentifier) for this **API** is defined as the combination of the **UID** of the **File** and the `operationId` of the [Operation Object](http://swagger.io/specification/#operationObject). For example, the following `get_contacts` operation has **UID** equal to `microsoft.com/docfx/Contacts/1.6/get_contacts`:
```json
{
  "swagger": "2.0",
  "info": {
    "title": "Contacts",
    "version": "1.6"
  },
  "host": "microsoft.com",
  "basePath": "/docfx",
  "schemes": [
    "https"
  ],
  "paths": {
    "/contacts": {
      "get": {
        "parameters": [
        ],
        "responses": {
        },
        "operationId": "get_contacts"
      }
    },
  }
}
```

> It is recommended that user provides a well-formed `operationId` name. We suggest that the `operationId` is one word in camelCase or snake_case.

HTML layout
--------------
By default, the generated HTML file lists all the **API**s inside the **File** in the order defined in the Swagger REST file.

You can use *Overwrite File*s to redefine the layout of the **API**s and add more information to the **File** and **API**.

### *Overwrite File*s
*Overwrite File*s are Markdown files with multiple *Overwrite Section*s starting with YAML header block. A valid YAML header for an *Overwrite Section* *MUST* take the form of valid [YAML](http://www.yaml.org/spec/1.2/spec.html) set between triple-dashed lines and start with property `uid`. Here is a basic example of an *Overwrite Section*:

```md
---
uid: microsoft.com/docfx/Contacts/1.6
---
Further description for `microsoft.com/docfx/Contacts/1.6`
```

The `uid` value *MUST* match the `uid` of the **File** or **API** that you want to overwrite. The content following YAML header is the additional Markdown description for the **File** or **API**. By default, it is transformed to HTML and appended below the description of the **File** or **API**.

### Redefine the layout
You can redefine the **API**s layout in the HTML page using `sections` property. For example, the following *Overwrite Section* specifies that only the two **API**s `get_contacts` and `add_contacts` are shown in the HTML page in order:

```md
---
uid: microsoft.com/docfx/Contacts/1.6
sections:
  - microsoft.com/docfx/Contacts/1.6/get_contacts
  - microsoft.com/docfx/Contacts/1.6/add_contacts
---
Further description for `microsoft.com/docfx/Contacts/1.6`
```

### Add footer
You can also define the `footer` of an **File** or **API** using the following syntax:

```md
---
uid: microsoft.com/docfx/Contacts/1.6
footer: *content
---
Footer for `microsoft.com/docfx/Contacts/1.6`
```

`*content` is the keyword representing the Markdown content following YAML header. The value for `*content` is always transformed from Markdown content to HTML. In the above example, the value for `*content` is `<p>Footer for <code>microsoft.com/docfx/Contacts/1.6</code></p>`. In this way, the value of `footer` for **API** `microsoft.com/docfx/Contacts/1.6` is set to `<p>Footer for <code>microsoft.com/docfx/Contacts/1.6</code></p>`. We leverage [Anchors](http://www.yaml.org/spec/1.2/spec.html#id2765878) syntax in YAML specification for `*content`.

If `footer` is set, the content from `footer` will be appended to the last section of the **File** or **API**. It is usually used to define **See Also** or **Additional Resources** for the documentation.

### Add other metadata
You can define your own metadata with YAML header. This functionality is quite useful when your own template is used.

When the key of the metadata is already preserved by DocFX, for example, `summary`, the value of `summary` will be overwritten. You can also overwrite complex types, for example, `description` of a `parameter`. Make sure the data structure of the provided metadata is consistent with the one defined in DocFX, otherwise, DocFX is unable to cast the value and fails.

When the key of the metadata is not preserved by DocFX, for example, `not_predefined`. The metadata is kept and can be used in the template.