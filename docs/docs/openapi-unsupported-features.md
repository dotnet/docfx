# OpenAPI features not yet supported

This page describes the unsupported features and known fidelity issues in the
[OpenAPI 3 REST documentation reader](rest-api-docs.md#openapi-3-documents),
which uses `Microsoft.OpenApi` and `Microsoft.OpenApi.YamlReader` **3.10.2**.
Swagger 2.0 JSON continues to use its existing reader and is not affected by these
OpenAPI 3 limitations.

OpenAPI 3.0, 3.1 and 3.2 JSON/YAML documents are accepted, but this is not full
OpenAPI or JSON Schema conformance. Parsing a document successfully does not mean
every feature is rendered. The list below describes known boundaries, not a
commitment to a particular release or an exhaustive conformance matrix.

## Features that produce an error

These cases produce an input error rather than falling back to the Swagger reader.
The affected document is not generated.

| Feature | Current behavior | Reason or alternative |
| --- | --- | --- |
| Cross-file and network `$ref` targets | `UnsupportedExternalReference` | Put components in the current document and use references such as `#/components/schemas/Pet`. |
| Future specification versions | Version error | Only OpenAPI 3.0, 3.1 and 3.2 are enabled. |
| Dynamic schema references (`$dynamicRef`) | `UnsupportedOpenApiSchema` | Dynamic scope is not implemented. Ordinary `$ref`, including recursive references, is supported. |
| Certain OpenAPI 3.0 primitive compositions | `UnsupportedOpenApiComposition` | The pinned SDK can lose exclusive alternatives or branch examples. See [primitive compositions](#primitive-compositions). |

### Primitive compositions

In some OpenAPI 3.0 cases, the pinned SDK combines primitive alternatives into a
type union. This is not always equivalent to `oneOf`, which requires exactly one
matching branch:

```yaml
oneOf:
  - type: integer
  - type: number
```

The value `3` matches both branches and must fail this `oneOf`. Displaying it as
`integer | number` would lose that restriction. Type-only `oneOf` branches with
duplicate types have a similar problem. The SDK can also discard examples attached
to primitive branches during this conversion.

The reader rejects these known lossy OpenAPI 3.0 forms. Disjoint type-only
alternatives, such as `string` and `integer`, can become equivalent unions.
Constrained alternatives and OpenAPI 3.1/3.2 compositions are not blanket-rejected.

## Features without dedicated documentation UI

These features produce a warning when encountered. Supported parts of the document
can still be generated; treating warnings as errors can make the warning a build
failure. Their presence is not an indication that the following details are rendered.

| Feature | Information not currently rendered |
| --- | --- |
| Callbacks | Callback operations, parameters and payloads associated with an API operation. |
| Webhooks | Top-level webhook operations and their requests/responses. |
| Security schemes and requirements | API key, HTTP/Bearer, OAuth2 and OpenID Connect configuration, operation requirements and scopes. |
| Media-type encoding | `encoding`, `itemEncoding` and `prefixEncoding` details. |
| Tag summary, hierarchy and kind | Tags retain flat grouping; `summary`, `parent` and `kind` have no dedicated UI. |
| Response Link Objects | Relationships to subsequent operations and mappings from response values to their parameters. |

Response Link Objects are not ordinary Markdown links or Docfx cross-references;
those continue to work. Missing security documentation does not disable or change
authentication in the API itself.

## Supported schema values and OpenAPI 3.2 features

Typed `const` values, explicit and implicit YAML null defaults, and boolean schemas
are supported. Boolean schemas work in components, properties and composition arrays
as well as inline. Docfx preserves these values around known SDK reader limitations.
Values inside examples and extension data remain literal data.

OpenAPI 3.2 support includes `QUERY`, additional HTTP methods, reusable media types,
streaming `itemSchema`, and examples using `dataValue` or `serializedValue`.
See the [OpenAPI 3.2 example](openapi-32-example.yml) for generated output.

## SDK implementation notes

The following links identify the fixed SDK version behind the known behavior:

- [`JsonNodeHelper.CreateMap/CreateList`](https://github.com/microsoft/OpenAPI.NET/blob/v3.10.2/src/Microsoft.OpenApi/Reader/JsonNodeHelper.cs)
  only pass object nodes to schema readers in map/list positions. Docfx normalizes
  boolean schemas to equivalent objects before parsing.
- The [OpenAPI 3.0 schema reader](https://github.com/microsoft/OpenAPI.NET/blob/v3.10.2/src/Microsoft.OpenApi/Reader/V3/OpenApiSchemaDeserializer.cs)
  performs the primitive-alternative folding described above.
- The [OpenAPI 3.1 schema reader](https://github.com/microsoft/OpenAPI.NET/blob/v3.10.2/src/Microsoft.OpenApi/Reader/V31/OpenApiSchemaDeserializer.cs)
  reads `const` through `GetScalarValue` and models it as a string. Docfx retains
  each original JSON value separately during SDK parsing and reference resolution,
  then restores it during model conversion.
- The automatic [`OpenApiWorkspaceLoader`](https://github.com/microsoft/OpenAPI.NET/blob/v3.10.2/src/Microsoft.OpenApi/Reader/Services/OpenApiWorkspaceLoader.cs)
  reuses the entry format and loads recursively before joining workspaces. Docfx
  instead loads complete local documents once, detects each format, and registers
  them with SDK workspaces before resolving references. It does not add a separate
  JSON Pointer or schema resolver.
