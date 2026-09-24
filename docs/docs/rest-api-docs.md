# REST API docs

Docfx generates REST API documentation from [Swagger 2.0](http://swagger.io/specification/) files.

To add REST API docs, include the swagger JSON file to the `build` config in `docfx.json`:

```json
{
  "build": {
    "content": [{
      "files": [ "**/*.swagger.json" ] // <-- Include swagger JSON files
    }]
  }
}
```

Each swagger file produces one output HTML file.


## Organize REST APIs using Tags

APIs can be organized using the [Tag Object](http://swagger.io/specification/#tagObject). An API can be associated with one or more tags. Untagged APIs are put in the _Other apis_ section.

This example defines the `Basic` and `Advanced` tags and organize APIs using the two tags. The [`x-bookmark-id`](http://swagger.io/specification/#vendorExtensions) property specifies the URL fragment for the tag.

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

The above example produces the following layout:

```
Basic
├─ get_contacts
Advanced
├─ get_contacts
├─ set_contacts
Other APIs
├─ delete_contacts
```
