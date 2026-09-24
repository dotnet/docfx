# REST API docs

Docfx generates REST API documentation from Swagger 2.0 JSON and OpenAPI 3.0, 3.1 and 3.2
JSON or YAML files, with documented feature limitations.
OpenAPI documents are read using [OpenAPI.NET](https://github.com/microsoft/OpenAPI.NET).
Swagger 2.0 continues to use the existing compatibility reader.

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

## OpenAPI 3 documents

See the [OpenAPI 3.0 example](openapi-3-example.yml) for a generated API page with
parameters, a request body, response examples and reusable schemas.
The [OpenAPI 3.2 example](openapi-32-example.yml) demonstrates streaming responses,
additional HTTP methods, typed constants, null defaults and boolean schemas.

Include the entry documents in `build.content`, for example:

```json
{
  "build": {
    "content": [{
      "files": ["api/service.yaml", "api/other.json"]
    }]
  }
}
```

Both `.yaml` and `.yml` are supported. Referenced OpenAPI documents can mix JSON and YAML and
do not need to be listed as separate entry documents. References are loaded through
Docfx's file abstraction; HTTP/HTTPS and network-share references are not fetched.
An invalid document or unresolved reference produces an input error, not a fallback
to the Swagger reader. OpenAPI 3.0, 3.1 and 3.2 are supported.

Operations reuse the existing Markdown, overwrite, cross-reference, tag and
operation splitting pipeline. Parameters, request bodies and response content
include their media types, schemas and examples. Example payloads are literal data,
not Markdown or documents whose `$ref` properties should be resolved.

Operation servers override path servers, which override document servers. Server
variables use their declared defaults; an omitted server defaults to `/`.
The root UID follows the existing authority/base-path/title/version convention
using the first document server. Operation UIDs append the operation ID. If an
operation has no ID, Docfx generates a stable, filename-safe ID from its HTTP method
and path. Explicit IDs must be unique. Tags used by operations need not be declared
at document level.

Schema documentation preserves alternatives and intersections rather than merging
`allOf`/`anyOf`/`oneOf` properties into a single object. OpenAPI 3.1/3.2 boolean schemas,
type unions and recursive references are displayed without expanding cycles. Schema
reference siblings are shown as an intersection with the target, not an override.
The SDK can normalize disjoint primitive alternatives into equivalent type unions.
This is documentation generation, not full JSON Schema validation or full OpenAPI
conformance. Callbacks, webhooks, security configuration and response links do not
have dedicated rendered UI. The original input remains available in the raw model.

OpenAPI 3.1/3.2 `const` values retain their JSON types, including numbers, booleans,
objects, arrays and null. Explicit and empty YAML null defaults are supported.
Boolean schemas work inline, in components and properties, and in composition arrays:
`true` accepts any value, while `false` accepts no value.

OpenAPI 3.2 `QUERY` and additional HTTP methods are rendered as operations. Streaming
media types display `itemSchema` under **Stream item**. Examples support structured
`dataValue` and literal `serializedValue`, as well as `value` and `externalValue`.

### Known OpenAPI.NET 3.10.2 limitations

See [OpenAPI features not yet supported](openapi-unsupported-features.md) for the
current input errors, features without dedicated UI, examples and SDK source references.

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
