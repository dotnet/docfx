// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Docfx.Build.RestApi.Swagger;
using Docfx.Common;
using Docfx.DataContracts.RestApi;
using Docfx.Exceptions;
using Docfx.Plugins;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;

namespace Docfx.Build.RestApi;

// Ownership detection and reader dispatch live here. Downstream code consumes REST view models.
internal static class RestApiDocumentReader
{
    internal sealed record Header(string Version, bool IsSwagger = false);

    internal static bool IsSupportedFile(string path)
    {
        var format = Format(path);
        if (format == null) return false;
        try
        {
            using var reader = EnvironmentContext.FileAbstractLayer.OpenReadText(path);
            return ReadHeader(reader, format) != null;
        }
        catch (Exception ex) when (ex is IOException or JsonException or YamlException)
        {
            Logger.LogVerbose($"Could not identify REST API document '{path}': {ex.Message}");
            return false;
        }
    }

    internal static RestApiRootItemViewModel Read(string path, string fileName)
    {
        var raw = EnvironmentContext.FileAbstractLayer.ReadAllText(path);
        var format = Format(path);
        var header = ReadHeader(new StringReader(raw), format);
        if (header is { IsSwagger: true })
        {
            var swagger = SwaggerJsonParser.Parse(path);
            swagger.Raw = raw;
            // Preserve legacy diagnostics, including extension objects under a path.
            foreach (var (route, item) in swagger.Paths ?? [])
            {
                foreach (var (method, operation) in item.Metadata)
                {
                    if (operation is JObject obj && !obj.ContainsKey("operationId"))
                    {
                        throw new DocfxException($"operationId should exist in operation '{method}' of path '{route}' for swagger file '{fileName}'");
                    }
                }
            }
            return OpenApi2ModelConverter.Convert(swagger);
        }
        return OpenApiDocumentReader.Parse(raw, format, new Uri(Path.GetFullPath(path)), header?.Version);
    }

    internal static string Format(string path) => Path.GetExtension(path).ToLowerInvariant() switch
    {
        ".json" => "json",
        ".yaml" or ".yml" => "yaml",
        _ => null
    };

    // Read only root markers, without allocating an object tree. The legacy JSON probe
    // also validates the complete JSON syntax to retain its existing ownership behavior.
    internal static Header ReadHeader(TextReader source, string format)
    {
        if (format == "json")
        {
            using var reader = new JsonTextReader(source) { DateParseHandling = DateParseHandling.None };
            if (!reader.Read() || reader.TokenType != JsonToken.StartObject) return null;
            Header swagger = null;
            while (reader.Read())
            {
                if (reader.TokenType == JsonToken.EndObject && reader.Depth == 0) return swagger;
                if (reader.TokenType != JsonToken.PropertyName || reader.Depth != 1) continue;
                var key = (string)reader.Value;
                if (!reader.Read()) return null;
                if (key == "openapi" && reader.TokenType == JsonToken.String) return new Header((string)reader.Value);
                if (key == "swagger" && reader.Value is "2.0") swagger = new Header("2.0", IsSwagger: true);
                reader.Skip();
            }
            return null;
        }
        var parser = new Parser(source);
        parser.Consume<StreamStart>();
        if (!parser.TryConsume<DocumentStart>(out _) || !parser.TryConsume<MappingStart>(out _)) return null;
        while (!parser.Accept<MappingEnd>(out _))
        {
            if (!parser.TryConsume<Scalar>(out var key)) return null;
            if (key.Value == "openapi" && parser.TryConsume<Scalar>(out var version)) return new Header(version.Value);
            parser.SkipThisAndNestedEvents();
        }
        return null;
    }
}
