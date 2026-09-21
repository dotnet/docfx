# OpenAPI features not yet supported

This page describes the unsupported features and known fidelity issues in the
[OpenAPI 3 REST documentation reader](rest-api-docs.md#openapi-3-documents),
which uses `Microsoft.OpenApi` and `Microsoft.OpenApi.YamlReader` **3.10.2**.
Swagger 2.0 JSON continues to use its existing reader and is not affected by these
OpenAPI 3 limitations.

OpenAPI 3.0 and 3.1 JSON/YAML documents are accepted, but this is not full
OpenAPI or JSON Schema conformance. Parsing a document successfully does not mean
every feature is rendered. The list below describes known boundaries, not a
commitment to a particular release or an exhaustive conformance matrix.

## Features that produce an error

These cases produce an input error rather than falling back to the Swagger reader.
The affected document is not generated.

| Feature | Current behavior | Reason or alternative |
| --- | --- | --- |
| Standalone external schema/component fragments | `UnsupportedExternalFragment` | This integration loads complete OpenAPI documents, not standalone fragments. Put shared components in a complete local document and reference its component path. |
| References without a fragment identifier | `UnsupportedExternalFragment` | Use a supported component reference such as `components.yaml#/components/schemas/Pet`. |
| HTTP/HTTPS or network-share references | Rejected; no network fetching | Use local referenced documents. Server URLs and ordinary documentation links are not restricted by this rule. |
| Dynamic schema references (`$dynamicRef`) | `UnsupportedOpenApiSchema` | Dynamic scope is not implemented. Ordinary `$ref`, including recursive references, is supported. |
| Boolean schemas in schema maps or composition arrays | `UnsupportedBooleanSchema` | The pinned SDK drops these values in certain positions. See [boolean schemas](#boolean-schemas). |
| Certain OpenAPI 3.0 primitive compositions | `UnsupportedOpenApiComposition` | The pinned SDK can lose exclusive alternatives or branch examples. See [primitive compositions](#primitive-compositions). |
| Object or array values of `const` | SDK reader error: `Expected scalar value` | These valid OpenAPI 3.1 schema values are not supported by the pinned SDK's scalar-based `const` reader. |
| OpenAPI 3.2 and other unsupported specification versions | Version error | Only OpenAPI 3.0 and 3.1 are enabled, even if the SDK can read newer versions. |

### Standalone external fragments

A file containing only a schema is a valid OpenAPI reference target, but is not
supported by this integration:

```yaml
# schemas/Pet.yaml
type: object
properties:
  name:
    type: string
```

For the supported form, put the schema in a complete component document:

```yaml
# components.yaml
openapi: 3.1.0
info:
  title: Shared components
  version: '1.0'
paths: {}
components:
  schemas:
    Pet:
      type: object
      properties:
        name:
          type: string
```

Then use `$ref: './components.yaml#/components/schemas/Pet'`.
Local component documents can mix JSON and YAML, and references between them can
form cycles. This limitation is in the current Docfx integration; it does not mean
standalone fragments are invalid OpenAPI.

### Boolean schemas

The ordinary schema `type: boolean` is supported. A *boolean schema* is different:
`true` accepts any JSON value, while `false` accepts no value. OpenAPI 3.1 allows
both forms.

Direct media-type schemas such as `schema: true` and `schema: false` are supported.
The following positions are rejected because the pinned SDK does not preserve them:

- Entries in `components.schemas`, `properties`, `patternProperties`, `$defs` and
  `dependentSchemas`.
- Branches in `allOf`, `anyOf` and `oneOf`.

For example, removing the `false` branch below would change a schema that accepts
no values into one that accepts strings:

```yaml
allOf:
  - false
  - type: string
```

The check applies to both entry documents and referenced documents. Boolean values
inside examples and extension data are not schemas and are not rejected by this check.

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
Constrained alternatives and OpenAPI 3.1 compositions are not blanket-rejected.

## Features without dedicated documentation UI

These features produce a warning when encountered. Supported parts of the document
can still be generated; treating warnings as errors can make the warning a build
failure. Their presence is not an indication that the following details are rendered.

| Feature | Information not currently rendered |
| --- | --- |
| Callbacks | Callback operations, parameters and payloads associated with an API operation. |
| Webhooks | Top-level webhook operations and their requests/responses. |
| Security schemes and requirements | API key, HTTP/Bearer, OAuth2 and OpenID Connect configuration, operation requirements and scopes. |
| Response Link Objects | Relationships to subsequent operations and mappings from response values to their parameters. |

Response Link Objects are not ordinary Markdown links or Docfx cross-references;
those continue to work. Missing security documentation does not disable or change
authentication in the API itself.

## Known schema fidelity issues

> [!WARNING]
> Numeric and boolean `const` values are not yet rejected or preserved correctly.
> They can be displayed as strings, changing the meaning of the constraint.
> Keeping the original input does not make that rendered constraint correct.

The following behavior has been verified with the pinned SDK and Docfx model
conversion:

| Input | Current result |
| --- | --- |
| `{"const": "ok"}` | Preserves the string `"ok"`. |
| `{"const": 42}` | Converts the number to the string `"42"`. |
| `{"const": true}` | Converts the boolean to the string `"True"`. |
| `{"const": {"status": "ok"}}` or `{"const": [1, 2]}` | Produces a reader error. |
| `{"const": null}` | Preserves the null constraint. |
| `{"type": ["string", "null"], "default": null}` | Preserves the explicit null default. |

`default` is an annotation, whereas `const` requires an exact value. They are not
interchangeable. Do not change a numeric or boolean `const` to a string merely to
make it render; that would change the API contract.

The entry document's original text is retained in the raw model for reference.
It does not repair lost types or constraints in the generated HTML, and does not
provide an archive of every external source document.

## SDK implementation notes

The following links identify the fixed SDK version behind the known behavior:

- [`JsonNodeHelper.CreateMap/CreateList`](https://github.com/microsoft/OpenAPI.NET/blob/v3.10.2/src/Microsoft.OpenApi/Reader/JsonNodeHelper.cs)
  only pass object nodes to schema readers in the affected map/list positions.
- The [OpenAPI 3.0 schema reader](https://github.com/microsoft/OpenAPI.NET/blob/v3.10.2/src/Microsoft.OpenApi/Reader/V3/OpenApiSchemaDeserializer.cs)
  performs the primitive-alternative folding described above.
- The [OpenAPI 3.1 schema reader](https://github.com/microsoft/OpenAPI.NET/blob/v3.10.2/src/Microsoft.OpenApi/Reader/V31/OpenApiSchemaDeserializer.cs)
  reads `const` through `GetScalarValue`, causing the non-string limitations.
- The automatic [`OpenApiWorkspaceLoader`](https://github.com/microsoft/OpenAPI.NET/blob/v3.10.2/src/Microsoft.OpenApi/Reader/Services/OpenApiWorkspaceLoader.cs)
  reuses the entry format and loads recursively before joining workspaces. Docfx
  instead loads complete local documents once, detects each format, and registers
  them with SDK workspaces before resolving references. It does not add a separate
  JSON Pointer or schema resolver.
