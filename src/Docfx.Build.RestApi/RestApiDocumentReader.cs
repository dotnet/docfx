// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Docfx.Build.Common;
using Docfx.Build.RestApi.Swagger;
using Docfx.DataContracts.RestApi;
using Docfx.Exceptions;
using Newtonsoft.Json.Linq;

namespace Docfx.Build.RestApi;

internal static class RestApiDocumentReader
{
    internal static RestApiRootItemViewModel Parse(string raw, string format) => Read(DocumentInput.FromText(raw, format));

    internal static RestApiRootItemViewModel Read(DocumentInput input, string fileName = null)
    {
        if (input.Header is { Kind: "openapi", Version: var version })
        {
            return OpenApiDocumentReader.Parse(input.ReadAllText(), input.Format, new Uri(Path.GetFullPath(input.Path)), version);
        }
        if (input.Format != "json" || input.Header is not { Kind: "swagger", Version: "2.0" })
        {
            throw new DocfxException($"Unable to identify REST API document '{input.Path}'.");
        }
        var raw = input.ReadAllText();
        using var reader = input.OpenRead();
        var swagger = SwaggerJsonParser.Parse(input.Path, reader);
        swagger.Raw = raw;
        // Preserve Swagger 2.0 diagnostics, including extension objects under a path.
        foreach (var (route, item) in swagger.Paths ?? [])
        {
            foreach (var (method, operation) in item.Metadata)
            {
                if (operation is JObject obj && !obj.ContainsKey("operationId"))
                {
                    throw new DocfxException($"operationId should exist in operation '{method}' of path '{route}' for swagger file '{fileName ?? input.Path}'");
                }
            }
        }
        return SwaggerModelConverter.FromSwaggerModel(swagger);
    }
}
