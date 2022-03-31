// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using HtmlReaderWriter;
using Microsoft.Docs.Validation;
using Newtonsoft.Json.Linq;

namespace Microsoft.Docs.Build;

internal class JsonSchemaTransformer
{
    private readonly DocumentProvider _documentProvider;
    private readonly MarkdownEngine _markdownEngine;
    private readonly LinkResolver _linkResolver;
    private readonly XrefResolver _xrefResolver;
    private readonly ErrorBuilder _errors;
    private readonly MonikerProvider _monikerProvider;
    private readonly JsonSchemaProvider _jsonSchemaProvider;
    private readonly Input _input;

    private readonly MemoryCache<FilePath, Watch<(JToken, JsonSchema, JsonSchemaMap, int)>> _schemaDocumentsCache = new();

    private readonly Scoped<ConcurrentBag<(SourceInfo<string> uid, string? propertyPath, JsonSchema, int? min, int? max)>> _uidReferenceCountList = new();
    private readonly Scoped<ConcurrentBag<(SourceInfo<string> xref, string? docsetName, string? schemaType, string? propertyPath)>> _xrefList = new();

    private static readonly ThreadLocal<Stack<SourceInfo<string>>> s_recursionDetector = new(() => new());

    public JsonSchemaTransformer(
        DocumentProvider documentProvider,
        MarkdownEngine markdownEngine,
        LinkResolver linkResolver,
        XrefResolver xrefResolver,
        ErrorBuilder errors,
        MonikerProvider monikerProvider,
        JsonSchemaProvider jsonSchemaProvider,
        Input input)
    {
        _documentProvider = documentProvider;
        _markdownEngine = markdownEngine;
        _linkResolver = linkResolver;
        _xrefResolver = xrefResolver;
        _errors = errors;
        _monikerProvider = monikerProvider;
        _jsonSchemaProvider = jsonSchemaProvider;
        _input = input;
    }

    public void PostValidate(bool isPartialBuild)
    {
        if (isPartialBuild)
        {
            return;
        }

        foreach (var (uid, propertyPath, schema, minReferenceCount, maxReferenceCount) in _uidReferenceCountList.Value)
        {
            var references = _xrefList.Value.Where(item => item.xref == uid).Select(item => item.xref.Source).ToArray();

            if (minReferenceCount != null && references.Length < minReferenceCount)
            {
                _errors.Add(Errors.JsonSchema.MinReferenceCountInvalid(uid, minReferenceCount, references, propertyPath));
            }

            if (maxReferenceCount != null && references.Length > maxReferenceCount)
            {
                _errors.Add(Errors.JsonSchema.MaxReferenceCountInvalid(uid, maxReferenceCount, references, propertyPath));
            }
        }
    }

    public ExternalXref[] GetValidateExternalXrefs()
    {
        return _xrefList.Value
            .Where(item => item.docsetName != null)
            .GroupBy(item => (item.xref.Value, item.propertyPath))
            .Select(xrefGroup => new ExternalXref
            {
                Uid = xrefGroup.Key.Value,
                PropertyPath = xrefGroup.Key.propertyPath,
                Count = xrefGroup.Count(),
                DocsetName = xrefGroup.First().docsetName,
                SchemaType = xrefGroup.First().schemaType,
            })
            .OrderBy(externalXref => externalXref.Uid)
            .ThenBy(xref => xref.PropertyPath)
            .ToArray();
    }

    public JToken TransformContent(ErrorBuilder errors, FilePath file)
    {
        var (token, schema, schemaMap, uidCount) = ValidateContent(errors, file);
        var xrefmap = new JObject();
        var result = TransformContentCore(errors, schemaMap, file, schema, schema, token, uidCount, "", xrefmap, preserveSourceInfo: false);
        if (xrefmap.Count > 0)
        {
            result["_xrefmap"] = xrefmap;
        }
        return result;
    }

    public JToken TransformMetadata(ErrorBuilder errors, FilePath file, JToken token, JsonSchemaValidator schemaValidator)
    {
        var schema = schemaValidator.Schema;
        var schemaMap = new JsonSchemaMap();
        var schemaErrors = schemaValidator.Validate(token, file, schemaMap);
        errors.AddRange(schemaErrors);

        return TransformContentCore(errors, schemaMap, file, schema, schema, token, 0, "", new(), preserveSourceInfo: true);
    }

    public IReadOnlyList<InternalXrefSpec> LoadXrefSpecs(ErrorBuilder errors, FilePath file)
    {
        var (token, schema, schemaMap, uidCount) = ValidateContent(errors, file);
        var xrefSpecs = new List<InternalXrefSpec>();
        LoadXrefSpecsCore(errors, schemaMap, file, schema, schema, token, xrefSpecs, uidCount);
        return xrefSpecs;
    }

    private (JToken token, JsonSchema schema, JsonSchemaMap schemaMap, int uidCount) ValidateContent(ErrorBuilder errors, FilePath file)
    {
        return _schemaDocumentsCache.GetOrAdd(file, file => new(() => ValidateContentCore(errors, file))).Value;
    }

    private (JToken token, JsonSchema schema, JsonSchemaMap schemaMap, int uidCount) ValidateContentCore(ErrorBuilder errors, FilePath file)
    {
        var token = file.Format switch
        {
            FileFormat.Json => _input.ReadJson(errors, file),
            FileFormat.Yaml => _input.ReadYaml(errors, file),
            _ => throw new NotSupportedException(),
        };
        var mime = _documentProvider.GetMime(file);
        var schemaValidator = _jsonSchemaProvider.GetSchemaValidator(mime);
        var schemaMap = new JsonSchemaMap();
        var schemaErrors = schemaValidator.Validate(token, file, schemaMap);
        errors.AddRange(schemaErrors);

        var uidCount = GetFileUidCount(schemaMap, token, schemaValidator.Schema);
        return (token, schemaValidator.Schema, schemaMap, uidCount);
    }

    private void LoadXrefSpecsCore(
        ErrorBuilder errors,
        JsonSchemaMap schemaMap,
        FilePath file,
        JsonSchema rootSchema,
        JsonSchema schema,
        JToken node,
        List<InternalXrefSpec> xrefSpecs,
        int uidCount,
        string? propertyPath = null)
    {
        switch (node)
        {
            case JObject obj:
                if (IsXrefSpec(schemaMap, obj, schema, out var uid, out var uidSchema))
                {
                    xrefSpecs.Add(LoadXrefSpec(
                        errors, schemaMap, file, rootSchema, schema, uidSchema, uid, obj, uidCount, propertyPath));
                }

                foreach (var (key, value) in obj)
                {
                    if (value != null && schemaMap.GetPropertySchema(schema, obj, key) is var subschema && subschema != null)
                    {
                        LoadXrefSpecsCore(
                            errors,
                            schemaMap,
                            file,
                            rootSchema,
                            subschema,
                            value,
                            xrefSpecs,
                            uidCount,
                            JsonUtility.AddToPropertyPath(propertyPath, key));
                    }
                }
                break;
            case JArray array:
                foreach (var (item, subschema) in schemaMap.ForEachJArray(schema, array))
                {
                    if (subschema != null)
                    {
                        LoadXrefSpecsCore(errors, schemaMap, file, rootSchema, subschema, item, xrefSpecs, uidCount, propertyPath);
                    }
                }
                break;
        }
    }

    private string? GetSchemaType(string? schemaType, string? schemaPropertyType, string? propertyPath, JObject obj, FilePath file)
    {
        if (schemaType is null)
        {
            if (schemaPropertyType != null)
            {
                return obj.TryGetValue(schemaPropertyType, out var type) && type is JValue typeValue && typeValue.Value is string typeString
                    ? typeString : null;
            }
            else if (string.IsNullOrEmpty(propertyPath))
            {
                schemaType = _documentProvider.GetMime(file);
            }
        }
        return schemaType;
    }

    private InternalXrefSpec LoadXrefSpec(
        ErrorBuilder errors,
        JsonSchemaMap schemaMap,
        FilePath file,
        JsonSchema rootSchema,
        JsonSchema schema,
        JsonSchema uidSchema,
        SourceInfo<string> uid,
        JObject obj,
        int uidCount,
        string? propertyPath)
    {
        var href = GetXrefHref(file, uid, uidCount, string.IsNullOrEmpty(propertyPath));
        var monikers = _monikerProvider.GetFileLevelMonikers(errors, file);
        var schemaType = GetSchemaType(uidSchema.SchemaType, schema.SchemaTypeProperty, propertyPath, obj, file);

        var xref = new InternalXrefSpec(uid, href, file, monikers)
        {
            PropertyPath = JsonUtility.AddToPropertyPath(propertyPath, "uid"),
            UidGlobalUnique = uidSchema.UidGlobalUnique,
            SchemaType = schemaType,
        };

        foreach (var xrefProperty in schema.XrefProperties)
        {
            if (xrefProperty == "uid")
            {
                continue;
            }

            if (!obj.TryGetValue(xrefProperty, out var value))
            {
                xref.XrefProperties[xrefProperty] = new Lazy<JToken>(() => JValue.CreateNull());
                continue;
            }

            var subschema = schemaMap.GetPropertySchema(schema, obj, xrefProperty);
            xref.XrefProperties[xrefProperty] = new Lazy<JToken>(
                () => LoadXrefProperty(
                    schemaMap, file, uid, value, rootSchema, subschema, uidCount, JsonUtility.AddToPropertyPath(propertyPath, xrefProperty)),
                LazyThreadSafetyMode.PublicationOnly);
        }

        return xref;
    }

    private int GetFileUidCount(JsonSchemaMap schemaMap, JToken node, JsonSchema schema)
    {
        var count = 0;
        switch (node)
        {
            case JObject obj:
                if (IsXrefSpec(schemaMap, obj, schema, out _, out _))
                {
                    count++;
                }

                foreach (var (key, value) in obj)
                {
                    if (value != null && schemaMap.GetPropertySchema(schema, obj, key) is var subschema && subschema != null)
                    {
                        count += GetFileUidCount(schemaMap, value, subschema);
                    }
                }
                break;
            case JArray array:
                foreach (var (item, subschema) in schemaMap.ForEachJArray(schema, array))
                {
                    if (subschema != null)
                    {
                        count += GetFileUidCount(schemaMap, item, subschema);
                    }
                }
                break;
        }
        return count;
    }

    private static bool IsXrefSpec(
        JsonSchemaMap schemaMap, JObject obj, JsonSchema schema, out SourceInfo<string> uid, [MaybeNullWhen(false)] out JsonSchema uidSchema)
    {
        // A xrefspec MUST be named uid, and the schema contentType MUST also be uid
        if (obj.TryGetValue<JValue>("uid", out var uidValue) && uidValue.Value is string tempUid)
        {
            uidSchema = schemaMap.GetPropertySchema(schema, obj, "uid");
            if (uidSchema?.ContentType == JsonSchemaContentType.Uid)
            {
                uid = new SourceInfo<string>(tempUid, uidValue.GetSourceInfo());
                return true;
            }
        }

        uid = default;
        uidSchema = default;
        return false;
    }

    private string GetXrefHref(FilePath file, string uid, int uidCount, bool isRootLevel)
    {
        var siteUrl = _documentProvider.GetSiteUrl(file);
        return !isRootLevel && uidCount > 1 ? UrlUtility.MergeUrl(siteUrl, "", $"#{UrlUtility.GetBookmark(uid)}") : siteUrl;
    }

    private JToken LoadXrefProperty(
        JsonSchemaMap schemaMap,
        FilePath file,
        SourceInfo<string> uid,
        JToken value,
        JsonSchema rootSchema,
        JsonSchema? schema,
        int uidCount,
        string propertyPath)
    {
        var recursionDetector = s_recursionDetector.Value!;
        if (recursionDetector.Contains(uid))
        {
            throw Errors.Link.CircularReference(uid, uid, recursionDetector, uid => $"{uid} ({uid.Source})").ToException();
        }

        try
        {
            recursionDetector.Push(uid);
            return TransformContentCore(
                _errors,
                schemaMap,
                file,
                rootSchema,
                schema,
                value,
                uidCount,
                propertyPath,
                new JObject(),
                preserveSourceInfo: false);
        }
        finally
        {
            Debug.Assert(recursionDetector.Count > 0);
            recursionDetector.Pop();
        }
    }

    private JToken TransformContentCore(
        ErrorBuilder errors,
        JsonSchemaMap schemaMap,
        FilePath file,
        JsonSchema rootSchema,
        JsonSchema? schema,
        JToken token,
        int uidCount,
        string? propertyPath,
        JObject xrefmap,
        bool preserveSourceInfo)
    {
        switch (token)
        {
            // transform array and object is not supported yet
            case JArray array:
                var newArray = new JArray();
                foreach (var (item, subschema) in schemaMap.ForEachJArray(schema, array))
                {
                    newArray.Add(TransformContentCore(
                        errors, schemaMap, file, rootSchema, subschema, item, uidCount, propertyPath, xrefmap, preserveSourceInfo));
                }

                return PreserveSourceInfo(array, newArray);

            case JObject obj:
                var newObject = new JObject();
                foreach (var (key, value) in obj)
                {
                    if (value != null)
                    {
                        newObject[key] = TransformContentCore(
                            errors,
                            schemaMap,
                            file,
                            rootSchema,
                            schemaMap.GetPropertySchema(schema, obj, key),
                            value,
                            uidCount,
                            JsonUtility.AddToPropertyPath(propertyPath, key),
                            xrefmap,
                            preserveSourceInfo);
                    }
                }
                return PreserveSourceInfo(obj, newObject);

            case JValue value when schema != null:
                return PreserveSourceInfo(value, TransformScalar(
                    errors.With(e => e with { PropertyPath = propertyPath }),
                    rootSchema,
                    schema,
                    file,
                    value,
                    propertyPath,
                    xrefmap));

            case JValue value:
                return value;

            default:
                throw new NotSupportedException();
        }

        T PreserveSourceInfo<T>(T originalToken, T token) where T : JToken
        {
            return preserveSourceInfo ? (T)JsonUtility.SetSourceInfo(token, JsonUtility.GetSourceInfo(originalToken)) : token;
        }
    }

    private JToken TransformScalar(
        ErrorBuilder errors,
        JsonSchema rootSchema,
        JsonSchema schema,
        FilePath file,
        JValue value,
        string? propertyPath,
        JObject xrefmap)
    {
        if (value.Type == JTokenType.Null || schema.ContentType is null)
        {
            return value;
        }

        var stringValue = value.Value<string>();
        if (stringValue is null)
        {
            return value;
        }

        var sourceInfo = JsonUtility.GetSourceInfo(value) ?? new SourceInfo(file);
        var content = new SourceInfo<string>(stringValue, sourceInfo);

        // Prefer JToken declared file because globalMetadata and fileMetadata is defined in docfx.yml,
        // link resolve should be relative to docfx.yml in that case.
        var referencingFile = sourceInfo.File;

        switch (schema.ContentType)
        {
            case JsonSchemaContentType.Href:
                var (linkErrors, link, _) = _linkResolver.ResolveLink(content, referencingFile, file, new HyperLinkNode
                {
                    HyperLinkType = HyperLinkType.Default,
                    IsVisible = true,  // workaround to skip 'link-text-missing' validation
                    UrlLink = stringValue,
                    SourceInfo = sourceInfo,
                });

                errors.AddRange(linkErrors);
                return link;

            case JsonSchemaContentType.Markdown:

                // todo: use BuildPage.CreateHtmlContent() when we only validate markdown properties' bookmarks
                var html = _markdownEngine.ToHtml(errors, content, sourceInfo, MarkdownPipelineType.Markdown, null, rootSchema.ContentFallback);
                return html;

            case JsonSchemaContentType.InlineMarkdown:

                // todo: use BuildPage.CreateHtmlContent() when we only validate markdown properties' bookmarks
                return _markdownEngine.ToHtml(errors, content, sourceInfo, MarkdownPipelineType.InlineMarkdown, null, rootSchema.ContentFallback);

            // TODO: remove JsonSchemaContentType.Html after LandingData is migrated
            case JsonSchemaContentType.Html:

                return HtmlUtility.TransformHtml(content, (ref HtmlReader reader, ref HtmlWriter writer, ref HtmlToken token) =>
                    HtmlUtility.TransformLink(ref token, null, link =>
                    {
                        var source = new SourceInfo<string>(link.Href, content.Source?.WithOffset(link.Href.Source));
                        var (htmlErrors, htmlLink, _) = _linkResolver.ResolveLink(source, referencingFile, file, tagName: link.TagName);
                        errors.AddRange(htmlErrors);
                        return htmlLink;
                    }));

            case JsonSchemaContentType.Uid:
            case JsonSchemaContentType.Xref:

                // the content here must be an UID, not href
                var (xrefError, xrefSpec, href) = _xrefResolver.ResolveXrefSpec(
                    content, referencingFile, file, _monikerProvider.GetFileLevelMonikers(ErrorBuilder.Null, file));

                errors.AddIfNotNull(xrefError);

                if (xrefSpec != null && schema.XrefType != null && !schema.XrefType.Contains(xrefSpec.SchemaType))
                {
                    errors.Add(Errors.Xref.XrefTypeInvalid(content, StringUtility.Join(schema.XrefType), xrefSpec.SchemaType));
                }

                if (xrefSpec != null && !xrefmap.ContainsKey(content))
                {
                    xrefmap[content] = JsonUtility.ToJObject(xrefSpec.ToExternalXrefSpec(href));
                }

                if (schema.ContentType == JsonSchemaContentType.Uid && (schema.MinReferenceCount != null || schema.MaxReferenceCount != null))
                {
                    Watcher.Write(() => _uidReferenceCountList.Value.Add((
                        content,
                        propertyPath,
                        rootSchema,
                        schema.MinReferenceCount,
                        schema.MaxReferenceCount)));
                }
                else if (schema.ContentType == JsonSchemaContentType.Xref)
                {
                    Watcher.Write(() => _xrefList.Value.Add((
                        content,
                        (xrefSpec is ExternalXrefSpec externalXref && schema.ValidateExternalXrefs) ? externalXref.DocsetName : null,
                        xrefSpec?.SchemaType,
                        propertyPath)));
                }
                return value;
        }

        return value;
    }
}
