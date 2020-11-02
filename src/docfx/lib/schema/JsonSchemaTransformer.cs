// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using HtmlReaderWriter;
using Newtonsoft.Json.Linq;

namespace Microsoft.Docs.Build
{
    internal class JsonSchemaTransformer
    {
        private readonly DocumentProvider _documentProvider;
        private readonly MarkdownEngine _markdownEngine;
        private readonly LinkResolver _linkResolver;
        private readonly XrefResolver _xrefResolver;
        private readonly ErrorBuilder _errors;
        private readonly MonikerProvider _monikerProvider;
        private readonly TemplateEngine _templateEngine;
        private readonly Input _input;

        private readonly MemoryCache<FilePath, (JToken, JsonSchema, JsonSchemaMap)> _schemaDocumentsCache
                   = new MemoryCache<FilePath, (JToken, JsonSchema, JsonSchemaMap)>();

        private readonly ConcurrentDictionary<FilePath, int> _uidCountCache = new ConcurrentDictionary<FilePath, int>(ReferenceEqualsComparer.Default);
        private readonly ConcurrentDictionary<(FilePath, string), (IXrefSpec? spec, JObject? specObj)> _mustacheXrefSpec =
            new ConcurrentDictionary<(FilePath, string), (IXrefSpec?, JObject?)>();

        private readonly ConcurrentBag<(SourceInfo<string> uid, string? propertyPath, JsonSchema schema, int? minReferenceCount, int? maxReferenceCount)>
            _uidReferenceCountList = new ConcurrentBag<(SourceInfo<string>, string?, JsonSchema, int?, int?)>();

        private readonly ConcurrentBag<(SourceInfo<string> xref, string? docsetName, string? schemaType)> _xrefList =
            new ConcurrentBag<(SourceInfo<string>, string?, string?)>();

        private static readonly ThreadLocal<Stack<SourceInfo<string>>> t_recursionDetector
                          = new ThreadLocal<Stack<SourceInfo<string>>>(() => new Stack<SourceInfo<string>>());

        public JsonSchemaTransformer(
            DocumentProvider documentProvider,
            MarkdownEngine markdownEngine,
            LinkResolver linkResolver,
            XrefResolver xrefResolver,
            ErrorBuilder errors,
            MonikerProvider monikerProvider,
            TemplateEngine templateEngine,
            Input input)
        {
            _documentProvider = documentProvider;
            _markdownEngine = markdownEngine;
            _linkResolver = linkResolver;
            _xrefResolver = xrefResolver;
            _errors = errors;
            _monikerProvider = monikerProvider;
            _templateEngine = templateEngine;
            _input = input;
        }

        public void PostValidate()
        {
            foreach (var (uid, propertyPath, schema, minReferenceCount, maxReferenceCount) in _uidReferenceCountList)
            {
                var references = _xrefList.Where(item => item.xref.Value.Equals(uid.Value)).Select(item => item.xref.Source).ToArray();

                if (minReferenceCount != null && references.Length < minReferenceCount)
                {
                    _errors.WithCustomRule(schema).Add(Errors.JsonSchema.MinReferenceCountInvalid(uid, minReferenceCount, references, propertyPath));
                }

                if (maxReferenceCount != null && references.Length > maxReferenceCount)
                {
                    _errors.WithCustomRule(schema).Add(Errors.JsonSchema.MaxReferenceCountInvalid(uid, maxReferenceCount, references, propertyPath));
                }
            }
        }

        public ExternalXref[] GetValidateExternalXrefs()
        {
            return _xrefList.Where(item => item.docsetName != null).GroupBy(item => item.xref.Value).Select(xrefGroup =>
            {
                return new ExternalXref
                    { Uid = xrefGroup.Key, Count = xrefGroup.Count(), DocsetName = xrefGroup.First().docsetName, SchemaType = xrefGroup.First().schemaType };
            }).OrderBy(externalXref => externalXref.Uid).ToArray();
        }

        public JToken GetMustacheXrefSpec(FilePath file, string uid)
        {
            var (_, specObj) = _mustacheXrefSpec.TryGetValue((file, uid), out var value) ? value : (null, null);

            // Ensure these well known properties does not fallback to mustache parent variable scope
            return specObj ?? new JObject { ["uid"] = uid, ["name"] = null, ["href"] = null };
        }

        public JToken TransformContent(ErrorBuilder errors, FilePath file)
        {
            var (token, schema, schemaMap) = ValidateContent(errors, file);
            var uidCount = _uidCountCache.GetOrAdd(file, GetFileUidCount(schemaMap, token));
            return TransformContentCore(
                errors.WithCustomRule(schema),
                schemaMap,
                file,
                schema,
                token,
                uidCount,
                "");
        }

        public IReadOnlyList<InternalXrefSpec> LoadXrefSpecs(ErrorBuilder errors, FilePath file)
        {
            var (token, schema, schemaMap) = ValidateContent(errors, file);
            var xrefSpecs = new List<InternalXrefSpec>();
            var uidCount = _uidCountCache.GetOrAdd(file, GetFileUidCount(schemaMap, token));
            LoadXrefSpecsCore(errors, file, schema, schemaMap, token, xrefSpecs, uidCount);
            return xrefSpecs;
        }

        private (JToken token, JsonSchema schema, JsonSchemaMap schemaMap) ValidateContent(ErrorBuilder errors, FilePath file)
        {
            return _schemaDocumentsCache.GetOrAdd(file, file => ValidateContentCore(errors, file));
        }

        private (JToken token, JsonSchema schema, JsonSchemaMap schemaMap) ValidateContentCore(ErrorBuilder errors, FilePath file)
        {
            var token = file.Format switch
            {
                FileFormat.Json => _input.ReadJson(errors, file),
                FileFormat.Yaml => _input.ReadYaml(errors, file),
                _ => throw new NotSupportedException(),
            };
            var mime = _documentProvider.GetMime(file);
            var schemaValidator = _templateEngine.GetSchemaValidator(mime);
            var schemaMap = new JsonSchemaMap(IsContentTransform);
            var schemaErrors = schemaValidator.Validate(token, file, schemaMap);
            errors.AddRange(schemaErrors);
            return (token, schemaValidator.Schema, schemaMap);
        }

        private static bool IsContentTransform(JToken token, JsonSchema schema)
        {
            return schema.ContentType != null || (token is JObject obj && obj.ContainsKey("uid"));
        }

        private void LoadXrefSpecsCore(
            ErrorBuilder errors,
            FilePath file,
            JsonSchema rootSchema,
            JsonSchemaMap schemaMap,
            JToken node,
            List<InternalXrefSpec> xrefSpecs,
            int uidCount,
            string? propertyPath = null)
        {
            switch (node)
            {
                case JObject obj:
                    if (IsXrefSpec(obj, schemaMap, out var uid, out var schema, out var uidSchema))
                    {
                        xrefSpecs.Add(LoadXrefSpec(
                            errors, schemaMap, file, rootSchema, schema, uidSchema, uid, obj, uidCount, propertyPath));
                    }

                    foreach (var (key, value) in obj)
                    {
                        if (value != null)
                        {
                            LoadXrefSpecsCore(
                                errors,
                                file,
                                rootSchema,
                                schemaMap,
                                value,
                                xrefSpecs,
                                uidCount,
                                JsonUtility.AddToPropertyPath(propertyPath, key));
                        }
                    }
                    break;
                case JArray array:
                    foreach (var item in array)
                    {
                        LoadXrefSpecsCore(errors, file, rootSchema, schemaMap, item, xrefSpecs, uidCount, propertyPath);
                    }
                    break;
            }
        }

        private string? GetSchemaType(JsonSchema rootSchema, JsonSchema schema, JsonSchema uidSchema, JObject obj, FilePath file)
        {
            var schemaType = uidSchema.SchemaType;
            if (schemaType is null)
            {
                if (schema.SchemaTypeProperty != null)
                {
                    schemaType = obj.TryGetValue(schema.SchemaTypeProperty, out var type) && type is JValue typeValue && typeValue.Value is string typeString ?
                        typeString : null;
                }
                else if (schema == rootSchema)
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
            var href = GetXrefHref(file, uid, uidCount, obj.Parent == null);
            var monikers = _monikerProvider.GetFileLevelMonikers(errors, file);
            var schemaType = GetSchemaType(rootSchema, schema, uidSchema, obj, file);

            var xref = new InternalXrefSpec(
                uid, href, file, monikers, obj.Parent?.Path, JsonUtility.AddToPropertyPath(propertyPath, "uid"), uidSchema.UidGlobalUnique, schemaType);

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

                if (!schemaMap.TryGetSchema(value, out var propertySchema))
                {
                    xref.XrefProperties[xrefProperty] = new Lazy<JToken>(() => value);
                    continue;
                }

                xref.XrefProperties[xrefProperty] = new Lazy<JToken>(
                    () => LoadXrefProperty(
                        schemaMap, file, uid, value, rootSchema, uidCount, JsonUtility.AddToPropertyPath(propertyPath, xrefProperty)),
                    LazyThreadSafetyMode.PublicationOnly);
            }

            return xref;
        }

        private int GetFileUidCount(JsonSchemaMap schemaMap, JToken node)
        {
            var count = 0;
            switch (node)
            {
                case JObject obj:
                    if (IsXrefSpec(obj, schemaMap, out _, out _, out _))
                    {
                        count++;
                    }

                    foreach (var (key, value) in obj)
                    {
                        if (value != null)
                        {
                            count += GetFileUidCount(schemaMap, value);
                        }
                    }
                    break;
                case JArray array:
                    foreach (var item in array)
                    {
                        count += GetFileUidCount(schemaMap, item);
                    }
                    break;
            }
            return count;
        }

        private static bool IsXrefSpec(
            JObject obj,
            JsonSchemaMap schemaMap,
            out SourceInfo<string> uid,
            [MaybeNullWhen(false)] out JsonSchema schema,
            [MaybeNullWhen(false)] out JsonSchema uidSchema)
        {
            // A xrefspec MUST be named uid, and the schema contentType MUST also be uid
            if (obj.TryGetValue<JValue>("uid", out var uidValue) && uidValue.Value is string tempUid &&
                schemaMap.TryGetSchema(uidValue, out uidSchema) && uidSchema.ContentType == JsonSchemaContentType.Uid &&
                schemaMap.TryGetSchema(obj, out schema))
            {
                uid = new SourceInfo<string>(tempUid, uidValue.GetSourceInfo());
                return true;
            }

            uid = default;
            uidSchema = schema = default;
            return false;
        }

        private string GetXrefHref(FilePath file, string uid, int uidCount, bool isRootLevel)
        {
            var siteUrl = _documentProvider.GetSiteUrl(file);
            return !isRootLevel && uidCount > 1 ? UrlUtility.MergeUrl(siteUrl, "", $"#{Regex.Replace(uid, @"\W", "_")}") : siteUrl;
        }

        private JToken LoadXrefProperty(
            JsonSchemaMap schemaMap,
            FilePath file,
            SourceInfo<string> uid,
            JToken value,
            JsonSchema rootSchema,
            int uidCount,
            string propertyPath)
        {
            var recursionDetector = t_recursionDetector.Value!;
            if (recursionDetector.Contains(uid))
            {
                throw Errors.Link.CircularReference(uid, uid, recursionDetector, uid => $"{uid} ({uid.Source})").ToException();
            }

            try
            {
                recursionDetector.Push(uid);
                return TransformContentCore(
                    _errors.WithCustomRule(rootSchema),
                    schemaMap,
                    file,
                    rootSchema,
                    value,
                    uidCount,
                    propertyPath);
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
            JToken token,
            int uidCount,
            string? propertyPath)
        {
            switch (token)
            {
                // transform array and object is not supported yet
                case JArray array:
                    var newArray = new JArray();
                    foreach (var item in array)
                    {
                        newArray.Add(TransformContentCore(errors, schemaMap, file, rootSchema, item, uidCount, propertyPath));
                    }

                    return newArray;

                case JObject obj:
                    var newObject = new JObject();
                    foreach (var (key, value) in obj)
                    {
                        if (value is null)
                        {
                            continue;
                        }
                        else
                        {
                            newObject[key] = TransformContentCore(
                                errors,
                                schemaMap,
                                file,
                                rootSchema,
                                value,
                                uidCount,
                                JsonUtility.AddToPropertyPath(propertyPath, key));
                        }
                    }
                    return newObject;

                case JValue value when schemaMap.TryGetSchema(token, out var schema):
                    return TransformScalar(errors.With(e => e.WithPropertyPath(propertyPath)), rootSchema, schema, file, value, propertyPath);

                case JValue value:
                    return value;

                default:
                    throw new NotSupportedException();
            }
        }

        private JToken TransformScalar(
            ErrorBuilder errors,
            JsonSchema rootSchema,
            JsonSchema schema,
            FilePath file,
            JValue value,
            string? propertyPath)
        {
            if (value.Type == JTokenType.Null || schema.ContentType is null)
            {
                return value;
            }

            var sourceInfo = JsonUtility.GetSourceInfo(value) ?? new SourceInfo(file);
            var content = new SourceInfo<string>(value.Value<string>(), sourceInfo);

            switch (schema.ContentType)
            {
                case JsonSchemaContentType.Href:

                    // Output absolute URL starting from Architecture
                    var absoluteUrl = _documentProvider.GetMime(file) == "Architecture";
                    var (error, link, _) = _linkResolver.ResolveLink(content, file, file, absoluteUrl);
                    errors.AddIfNotNull(error);
                    return link;

                case JsonSchemaContentType.Markdown:

                    // todo: use BuildPage.CreateHtmlContent() when we only validate markdown properties' bookmarks
                    return _markdownEngine.ToHtml(errors, content, sourceInfo, MarkdownPipelineType.Markdown, null, rootSchema.ContentFallback);

                case JsonSchemaContentType.InlineMarkdown:

                    // todo: use BuildPage.CreateHtmlContent() when we only validate markdown properties' bookmarks
                    return _markdownEngine.ToHtml(errors, content, sourceInfo, MarkdownPipelineType.InlineMarkdown, null, rootSchema.ContentFallback);

                // TODO: remove JsonSchemaContentType.Html after LandingData is migrated
                case JsonSchemaContentType.Html:

                    return HtmlUtility.TransformHtml(content, (ref HtmlReader reader, ref HtmlWriter writer, ref HtmlToken token) =>
                    {
                        HtmlUtility.TransformLink(ref token, null, href =>
                        {
                            var source = new SourceInfo<string>(href, content.Source?.WithOffset(href.Source));
                            var (htmlError, htmlLink, _) = _linkResolver.ResolveLink(source, file, file);
                            errors.AddIfNotNull(htmlError);
                            return htmlLink;
                        });
                    });

                case JsonSchemaContentType.Uid:
                case JsonSchemaContentType.Xref:

                    var (spec, specObj) = _mustacheXrefSpec.GetOrAdd((file, content), _ =>
                    {
                        // the content here must be an UID, not href
                        var (xrefError, xrefSpec, href) = _xrefResolver.ResolveXrefSpec(
                            content, file, file, _monikerProvider.GetFileLevelMonikers(ErrorBuilder.Null, file));

                        errors.AddIfNotNull(xrefError);

                        if (xrefSpec != null &&
                            schema.XrefType != null &&
                            !schema.XrefType.Equals(xrefSpec.SchemaType, StringComparison.OrdinalIgnoreCase))
                        {
                            errors.Add(Errors.Xref.XrefTypeInvalid(content, schema.XrefType, xrefSpec.SchemaType));
                        }

                        var xrefSpecObj = xrefSpec is null ? null : JsonUtility.ToJObject(xrefSpec.ToExternalXrefSpec(href));

                        // Ensure these well known properties does not fallback to mustache parent variable scope
                        if (xrefSpecObj != null)
                        {
                            xrefSpecObj["uid"] ??= null;
                            xrefSpecObj["name"] ??= xrefSpecObj["uid"] ?? null;
                            xrefSpecObj["href"] ??= null;
                        }
                        return (xrefSpec, xrefSpecObj);
                    });

                    if (schema.ContentType == JsonSchemaContentType.Uid && (schema.MinReferenceCount != null || schema.MaxReferenceCount != null))
                    {
                        _uidReferenceCountList.Add((
                            new SourceInfo<string>(value.Value<string>(), value.GetSourceInfo()),
                            propertyPath,
                            rootSchema,
                            schema.MinReferenceCount,
                            schema.MaxReferenceCount));
                    }
                    else if (schema.ContentType == JsonSchemaContentType.Xref)
                    {
                        _xrefList.Add((
                            new SourceInfo<string>(value.Value<string>(), value.GetSourceInfo()),
                            (spec is ExternalXrefSpec externalXref && schema.ValidateExternalXrefs) ? externalXref.DocsetName : null,
                            spec?.SchemaType));
                    }
                    return value;
            }

            return value;
        }
    }
}
