// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;

namespace Microsoft.Docs.Build;

internal class JsonSchemaProvider
{
    private readonly PackagePath _template;
    private readonly Package _package;
    private readonly JsonSchemaLoader _jsonSchemaLoader;

    private readonly ConcurrentDictionary<string, JsonSchemaValidator?> _schemas = new(StringComparer.OrdinalIgnoreCase);

    private static readonly HashSet<string> s_outputAbsoluteUrlYamlMime = new(StringComparer.OrdinalIgnoreCase)
    {
        "Architecture",
        "TSType",
        "TSEnum",
    };

    public JsonSchemaProvider(Config config, PackageResolver packageResolver, JsonSchemaLoader jsonSchemaLoader)
    {
        var template = config.Template;
        var templateFetchOptions = PackageFetchOptions.DepthOne;
        if (template.Type == PackageType.None)
        {
            template = new("_themes");
            templateFetchOptions |= PackageFetchOptions.IgnoreDirectoryNonExistedError;
        }

        _package = packageResolver.ResolveAsPackage(template, templateFetchOptions);
        _template = template;
        _jsonSchemaLoader = jsonSchemaLoader;
    }

    public JsonSchemaProvider(Config config, Package package, JsonSchemaLoader jsonSchemaLoader)
    {
        _template = config.Template;
        _package = package;
        _jsonSchemaLoader = jsonSchemaLoader;
    }

    public static bool OutputAbsoluteUrl(string? mime) => mime != null && s_outputAbsoluteUrlYamlMime.Contains(mime);

    public static bool IsConceptual(string? mime) => "Conceptual".Equals(mime, StringComparison.OrdinalIgnoreCase);

    public static bool IsLandingData(string? mime) => "LandingData".Equals(mime, StringComparison.OrdinalIgnoreCase);

    public RenderType GetRenderType(ContentType contentType, SourceInfo<string?> mime)
    {
        return contentType switch
        {
            ContentType.Redirection => RenderType.Content,
            ContentType.Page => GetRenderType(mime),
            ContentType.Toc => GetTocRenderType(),
            _ => RenderType.Component,
        };
    }

    public RenderType GetRenderType(SourceInfo<string?> mime)
    {
        if (mime == null || IsConceptual(mime) || IsLandingData(mime))
        {
            return RenderType.Content;
        }
        try
        {
            return GetSchema(mime).RenderType;
        }
        catch (Exception ex) when (DocfxException.IsDocfxException(ex, out var _))
        {
            return RenderType.Content;
        }
    }

    public JsonSchemaValidator GetSchemaValidator(SourceInfo<string?> mime)
    {
        var name = mime.Value ?? throw Errors.Yaml.SchemaNotFound(mime).ToException();

        return _schemas.GetOrAdd(name, GetSchemaCore) ?? throw Errors.Yaml.SchemaNotFound(mime).ToException();
    }

    private JsonSchema GetSchema(SourceInfo<string?> mime)
    {
        return GetSchemaValidator(mime).Schema;
    }

    private JsonSchemaValidator? GetSchemaCore(string mime)
    {
        var jsonSchema = IsLandingData(mime)
            ? _jsonSchemaLoader.LoadSchema(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "data/docs/landing-data.json")))
            : _jsonSchemaLoader.TryLoadSchema(_package, new PathString($"ContentTemplate/schemas/{mime}.schema.json"));

        if (jsonSchema is null)
        {
            return null;
        }

        return new JsonSchemaValidator(jsonSchema, forceError: true);
    }

    private RenderType GetTocRenderType()
    {
        // Refer to https://ceapex.visualstudio.com/Engineering/_workitems/edit/537091
        // There is a toc.schema.json file existing in templates.docs.msft repo whose 'renderType' is set to 'component'
        // (https://github.com/microsoft/templates.docs.msft/blob/master/ContentTemplate/schemas/Toc.schema.json#L7)
        // This will break the PDF process because PDF asks it to be 'content' (so that docfx can generate 'toc.html' which is necessary for building PDF)
        // To avoid unexpected breaking PDF process through sync templates, special logic dealing with PDF cases goes first.
        if (_template.Url.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase) ||
            _template.Path.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
        {
            return RenderType.Content;
        }

        try
        {
            return GetSchema(new SourceInfo<string?>("toc")).RenderType;
        }
        catch (Exception ex) when (DocfxException.IsDocfxException(ex, out var _))
        {
            return RenderType.Component;
        }
    }
}
