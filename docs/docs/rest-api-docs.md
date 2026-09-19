# REST API docs

Docfx generates REST API documentation from Swagger 2.0 JSON and OpenAPI 3.0 JSON or YAML files,
with **OpenAPI 3.1 core support and documented SDK limitations**.
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
to the Swagger reader. Only OpenAPI 3.0 and 3.1 are supported, even if the installed
library can read newer versions.

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
`allOf`/`anyOf`/`oneOf` properties into a single object. OpenAPI 3.1 inline boolean schemas,
type unions and recursive references are displayed without expanding cycles. Schema
reference siblings are shown as an intersection with the target, not an override.
The SDK can normalize disjoint primitive alternatives into equivalent type unions.
This is documentation generation, not full JSON Schema validation or full OpenAPI
conformance. Callbacks, webhooks, security configuration and response links do not
have dedicated rendered UI. The original input remains available in the raw model.

### Known OpenAPI.NET 3.10.2 limitations

This integration pins `Microsoft.OpenApi` and `Microsoft.OpenApi.YamlReader` to
**3.10.2**. It deliberately reports errors instead of generating misleading documentation
for the following valid inputs:

- Boolean schemas in `components.schemas`, `properties`, `patternProperties`,
  `$defs` or `dependentSchemas`, and boolean branches in `allOf`, `anyOf` or `oneOf`,
  produce `UnsupportedBooleanSchema`. The SDK's
  [`JsonNodeHelper.CreateMap/CreateList`](https://github.com/microsoft/OpenAPI.NET/blob/v3.10.2/src/Microsoft.OpenApi/Reader/JsonNodeHelper.cs)
  only pass JSON objects to the schema reader, dropping these boolean values.
  Docfx checks root and external sources before reading; it does not rewrite schemas.
  Boolean example payloads and extension data are unaffected.
- Standalone external schema/component fragments without an OpenAPI document envelope
  are not yet supported. `UnsupportedExternalFragment` identifies this integration limit,
  not an invalid OpenAPI specification. Keep referenced definitions in a complete
  OpenAPI 3.0/3.1 component document for this version of the integration.
- Dynamic schema references (`$dynamicRef`) produce `UnsupportedOpenApiSchema`;
  they are not replaced with ordinary references.
- The SDK's [OpenAPI 3.0 primitive-union folding](https://github.com/microsoft/OpenAPI.NET/blob/v3.10.2/src/Microsoft.OpenApi/Reader/V3/OpenApiSchemaDeserializer.cs#L423-L529)
  can lose exclusivity for type-only `oneOf` branches with duplicate types or
  overlapping `integer`/`number` types, and can discard branch examples.
  These known lossy forms produce `UnsupportedOpenApiComposition`. Disjoint
  type-only alternatives and constrained alternatives remain supported.

The SDK's automatic
[`OpenApiWorkspaceLoader`](https://github.com/microsoft/OpenAPI.NET/blob/v3.10.2/src/Microsoft.OpenApi/Reader/Services/OpenApiWorkspaceLoader.cs)
reuses the entry document's format for external documents and loads recursively before
joining workspaces. Docfx therefore loads local documents once, detects each file's
format, and registers them with SDK workspaces before resolving references. This
supports mixed-format and cyclic document graphs without adding a separate JSON
Pointer or schema resolver.

The typed SDK model is not a lossless JSON Schema representation (for example, explicit
null defaults and some `const` forms). Use the preserved original source for exact
schema syntax. This integration does not advertise OpenAPI 3.2 support.

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
