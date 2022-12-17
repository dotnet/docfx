Introduction to REST API Documentation
===============================

Introduction
-------------

DocFX supports generating documentation from REST APIs following [Swagger specification](http://swagger.io/specification/) version 2.0.

The Swagger RESTful API files *MUST* end with `.json`.

One Swagger API file generates one HTML file. For example, a file `contacts.swagger.json` generates file naming `contacts.html`.

Basic structure
--------------
A single Swagger API file is considered as a unique REST **File** containing multiple **API**s. The **UID**(Unique IDentifier) for the **File** is defined as the combination of `host`, `basePath`, `info.title` and `info.version` with `/` as separator. For example, the following Swagger API file has **UID** equals to `microsoft.com/docfx/Contacts/1.6`:

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
    }
  }
}
```

> [!Tip]
> It is recommended that user provides a well-formed `operationId` name.
> We suggest that the `operationId` is one word in camelCase or snake_case.

A REST API **File** could also contain multiple tags. The tag is a [Tag Object](http://swagger.io/specification/#tagObject), which is optional and used by [Operation Object](http://swagger.io/specification/#operationObject). The **UID**(Unique IDentifier) for this tag is defined as the combination of **UID** of the **File**, `tag`, and `name` of the [Tag Object](http://swagger.io/specification/#tagObject). For example, the following tag `Basic` has **UID** `microsoft.com/docfx/Contacts/1.6/tag/Basic`:
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
  "tags": [
    {
      "name": "Basic",
      "description": "Basic description"
    },
    {
      "name": "Advanced",
      "description": "Advanced description"
    }
  ]
}
```

HTML layout
--------------
The generated HTML file lists all the **API**s inside the **File** in the order defined in the Swagger REST file.

You can use *Overwrite File*s to add more information to the **File** and **API**, and use tags to organize the sections of the **API**s.

### *Overwrite File*s
*Overwrite File*s are Markdown files with multiple *Overwrite Section*s starting with YAML header block. A valid YAML header for an *Overwrite Section* *MUST* take the form of valid [YAML](http://www.yaml.org/spec/1.2/spec.html) set between triple-dashed lines and start with property `uid`. Here is a basic example of an *Overwrite Section*:

```md
---
uid: microsoft.com/docfx/Contacts/1.6
---
Further description for `microsoft.com/docfx/Contacts/1.6`
```

The `uid` value *MUST* match the `uid` of the **File** or **API** that you want to overwrite. The content following YAML header is the additional Markdown description for the **File** or **API**. By default, it is transformed to HTML and appended below the description of the **File** or **API**.

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

### Tags to organize the sections of APIs
You can organize the sections of APIs by using tags in Swagger file, following definitions in [Tag Object](http://swagger.io/specification/#tagObject). 

Each API can be specified with one or multiple tags, or not speficied with any tag.
- If all APIs are *not* tagged, each API will not be included in any sections.
- If the API is specified with *one* tag only, it will show inside this one tag section.
- If the API is specified with *multiple* tags, it will show inside multiple tag sections.
- If some APIs are specified with tags while some other APIs are not, the untagged APIs will be organized into one auto generated `Other apis` section.

Specific bookmark could be added to tag section using `x-bookmark-id`, which is Swagger schema extensions following [Specification Extensions](http://swagger.io/specification/#vendorExtensions). If no `x-bookmark-id` is specified, `name` of the tag will be the default bookmark.

For example, the following swagger file defines `Basic` and `Advanced` tags.
1. Sections in the layout:
   - `set_contacts` API is tagged with `Advanced` only, then it will only show inside `Advanced` tag section.
   - `get_contacts` API is tagged with both `Basic` and `Advanced`, then it will show inside both of the tag sections.
   - `delete_contacts` API is not tagged, it will show inside "Other apis" section.
2. Bookmarks:
   - Bookmark of `Basic` tag is `BasicBookmark`, which is defined by `x-bookmark-id`.
   - Bookmark of `Advanced` tag is `Advanced`, which use `name` by default.

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
  "tags": [
    {
      "name": "Basic",
      "x-bookmark-id": "BasicBookmark",
      "description": "Basic description"
    },
    {
      "name": "Advanced",
      "description": "Advanced description"
    }
  ],
  "paths": {
    "/contacts": {
      "get": {
        "operationId": "get_contacts",
        "tags": [
          "Basic",
          "Advanced"
        ]
      },      
      "set": {
        "operationId": "set_contacts",
        "tags": [
          "Advanced"
        ]
      },      
      "delete": {
        "operationId": "delete_contacts"
      }
    }
  }
}
```

For the example above, the simple html layout will be:
```html
<h2 id="BasicBookmark">Basic</h2>
  <h3 data-uid="microsoft.com/docfx/Contacts/1.6/get_contacts">get_contacts</h3>
<h2 id="Advanced">Advanced</h2>
  <h3 data-uid="microsoft.com/docfx/Contacts/1.6/get_contacts">get_contacts</h3>
  <h3 data-uid="microsoft.com/docfx/Contacts/1.6/set_contacts">set_contacts</h3>
<h2 id="other-apis">Other APIs</h2>
  <h3 data-uid="microsoft.com/docfx/Contacts/1.6/delete_contacts">delete_contacts</h3>
```


#### Overwrite the tags
1. More information could be added to the tag as following:
   ```md
   ---
   uid: microsoft.com/docfx/Contacts/1.6/tag/Basic
   ---

   Additional comments for `microsoft.com/docfx/Contacts/1.6/tag/Basic`

   ```

2. The `description` of the tag could be overwritten as following:
   ```md
   ---
   uid: microsoft.com/docfx/Contacts/1.6/tag/Basic
   description: *content
   ---

   Overwrite description for `microsoft.com/docfx/Contacts/1.6/tag/Basic`

   ```

### Add other metadata
You can define your own metadata with YAML header. This functionality is quite useful when your own template is used.

When the key of the metadata is already preserved by DocFX, for example, `summary`, the value of `summary` will be overwritten. You can also overwrite complex types, for example, `description` of a `parameter`. Make sure the data structure of the provided metadata is consistent with the one defined in DocFX, otherwise, DocFX is unable to cast the value and fails.

When the key of the metadata is not preserved by DocFX, for example, `not_predefined`. The metadata is kept and can be used in the template.

## Split extensibility
By default, one *REST* API file generates one HTML file. For example, petstore.json generates petstore.html. We provide `rest.tagpage` and `rest.operationpage` plugins to split the original *REST* API page into smaller pages.

1. With `rest.tagpage` plugin enabled, operations with the same tag are grouped into one page.
2. With `rest.operationpage` plugin enabled, each operation is splitted into single page.
3. With both `rest.tagpage` and `rest.operationpage` plugins enabled, the *REST* model will be splitted to tag level first, then split to operation level.

Refer [Plugins dashboard](../templates-and-plugins/plugins-dashboard.yml) for more details.
