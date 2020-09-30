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

        private readonly ConcurrentDictionary<FilePath, int> _uidCountCache = new ConcurrentDictionary<FilePath, int>(ReferenceEqualsComparer.Default);
        private readonly ConcurrentDictionary<(FilePath, string), JObject?> _mustacheXrefSpec = new ConcurrentDictionary<(FilePath, string), JObject?>();

        private readonly ConcurrentBag<(SourceInfo<string> uid, string? propertyPath, int? minReferenceCount, int? maxReferenceCount)> _uidReferenceCountList =
            new ConcurrentBag<(SourceInfo<string>, string?, int?, int?)>();

        private readonly ConcurrentBag<SourceInfo<string>> _xrefList = new ConcurrentBag<SourceInfo<string>>();

        private static readonly ThreadLocal<Stack<SourceInfo<string>>> t_recursionDetector
                          = new ThreadLocal<Stack<SourceInfo<string>>>(() => new Stack<SourceInfo<string>>());

        public JsonSchemaTransformer(
            DocumentProvider documentProvider,
            MarkdownEngine markdownEngine,
            LinkResolver linkResolver,
            XrefResolver xrefResolver,
            ErrorBuilder errors,
            MonikerProvider monikerProvider)
        {
            _documentProvider = documentProvider;
            _markdownEngine = markdownEngine;
            _linkResolver = linkResolver;
            _xrefResolver = xrefResolver;
            _errors = errors;
            _monikerProvider = monikerProvider;
        }

        public void PostValidate()
        {
            foreach (var (uid, propertyPath, minReferenceCount, maxReferenceCount) in _uidReferenceCountList)
            {
                var references = _xrefList.Where(xref => xref.Value.Equals(uid.Value)).Select(xref => xref.Source).ToArray();

                if (minReferenceCount != null && references.Length < minReferenceCount)
                {
                    _errors.Add(Errors.JsonSchema.ReferenceCountInvalid(uid, $">= {minReferenceCount}", references, propertyPath));
                }

                if (maxReferenceCount != null && references.Length > maxReferenceCount)
                {
                    _errors.Add(Errors.JsonSchema.ReferenceCountInvalid(uid, $"<= {maxReferenceCount}", references, propertyPath));
                }
            }
        }

        public JToken GetMustacheXrefSpec(FilePath file, string uid)
        {
            var result = _mustacheXrefSpec.TryGetValue((file, uid), out var value) ? value : null;

            // Ensure these well known properties does not fallback to mustache parent variable scope
            return result ?? new JObject { ["uid"] = uid, ["name"] = null, ["href"] = null };
        }

        public JToken TransformContent(ErrorBuilder errors, JsonSchema schema, FilePath file, JToken token)
        {
            var definitions = new JsonSchemaDefinition(schema);
            var uidCount = _uidCountCache.GetOrAdd(file, GetFileUidCount(definitions, schema, token));
            return TransformContentCore(
                errors.WithCustomRule(schema),
                definitions,
                file,
                schema,
                schema,
                token,
                uidCount,
                string.Empty);
        }

        public IReadOnlyList<InternalXrefSpec> LoadXrefSpecs(
            ErrorBuilder errors,
            JsonSchema schema,
            FilePath file,
            JToken token)
        {
            var xrefSpecs = new List<InternalXrefSpec>();
            var definitions = new JsonSchemaDefinition(schema);
            var uidCount = _uidCountCache.GetOrAdd(file, GetFileUidCount(definitions, schema, token));
            LoadXrefSpecsCore(errors, file, schema, schema, definitions, token, xrefSpecs, uidCount);
            return xrefSpecs;
        }

        private void LoadXrefSpecsCore(
            ErrorBuilder errors,
            FilePath file,
            JsonSchema rootSchema,
            JsonSchema schema,
            JsonSchemaDefinition definitions,
            JToken node,
            List<InternalXrefSpec> xrefSpecs,
            int uidCount,
            string? propertyPath = null)
        {
            schema = definitions.GetDefinition(schema);
            switch (node)
            {
                case JObject obj:
                    if (IsXrefSpec(obj, schema, out var uid, out var uidSchema))
                    {
                        xrefSpecs.Add(LoadXrefSpec(
                            errors, definitions, file, rootSchema, schema, uidSchema, uid, obj, uidCount, propertyPath));
                    }

                    foreach (var (key, value) in obj)
                    {
                        if (value != null && schema.Properties.TryGetValue(key, out var propertySchema))
                        {
                            LoadXrefSpecsCore(
                                errors,
                                file,
                                rootSchema,
                                propertySchema,
                                definitions,
                                value,
                                xrefSpecs,
                                uidCount,
                                JsonUtility.AddToPropertyPath(propertyPath, key));
                        }
                    }
                    break;
                case JArray array when schema.Items.schema != null:
                    foreach (var item in array)
                    {
                        LoadXrefSpecsCore(errors, file, rootSchema, schema.Items.schema, definitions, item, xrefSpecs, uidCount, propertyPath);
                    }
                    break;
            }
        }

        private InternalXrefSpec LoadXrefSpec(
            ErrorBuilder errors,
            JsonSchemaDefinition definitions,
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
            var xref = new InternalXrefSpec(uid, href, file, monikers, obj.Parent?.Path);

            if (uidSchema.UIDGlobalUnique)
            {
                xref.UIDGlobalUnique = true;
            }

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

                if (!schema.Properties.TryGetValue(xrefProperty, out var propertySchema))
                {
                    xref.XrefProperties[xrefProperty] = new Lazy<JToken>(() => value);
                    continue;
                }

                xref.XrefProperties[xrefProperty] = new Lazy<JToken>(
                    () => LoadXrefProperty(
                        definitions, file, uid, value, rootSchema, propertySchema, uidCount, JsonUtility.AddToPropertyPath(propertyPath, xrefProperty)),
                    LazyThreadSafetyMode.PublicationOnly);
            }

            return xref;
        }

        private int GetFileUidCount(JsonSchemaDefinition definitions, JsonSchema schema, JToken node)
        {
            schema = definitions.GetDefinition(schema);
            var count = 0;
            switch (node)
            {
                case JObject obj:
                    if (IsXrefSpec(obj, schema, out _, out _))
                    {
                        count++;
                    }

                    foreach (var (key, value) in obj)
                    {
                        if (value != null && schema.Properties.TryGetValue(key, out var propertySchema))
                        {
                            count += GetFileUidCount(definitions, propertySchema, value);
                        }
                    }
                    break;
                case JArray array when schema.Items.schema != null:
                    foreach (var item in array)
                    {
                        count += GetFileUidCount(definitions, schema.Items.schema, item);
                    }
                    break;
            }
            return count;
        }

        private static bool IsXrefSpec(JObject obj, JsonSchema schema, out SourceInfo<string> uid, [MaybeNullWhen(false)] out JsonSchema outSchema)
        {
            uid = default;
            outSchema = default;

            // A xrefspec MUST be named uid, and the schema contentType MUST also be uid
            if (obj.TryGetValue<JValue>("uid", out var uidValue) && uidValue.Value is string tempUid &&
                schema.Properties.TryGetValue("uid", out var uidSchema) && uidSchema.ContentType == JsonSchemaContentType.Uid)
            {
                outSchema = uidSchema;
                uid = new SourceInfo<string>(tempUid, uidValue.GetSourceInfo());
                return true;
            }
            return false;
        }

        private string GetXrefHref(FilePath file, string uid, int uidCount, bool isRootLevel)
        {
            var siteUrl = _documentProvider.GetSiteUrl(file);
            return !isRootLevel && uidCount > 1 ? UrlUtility.MergeUrl(siteUrl, "", $"#{Regex.Replace(uid, @"\W", "_")}") : siteUrl;
        }

        private JToken LoadXrefProperty(
            JsonSchemaDefinition definitions,
            FilePath file,
            SourceInfo<string> uid,
            JToken value,
            JsonSchema rootSchema,
            JsonSchema schema,
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
                    definitions,
                    file,
                    rootSchema,
                    schema,
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
            JsonSchemaDefinition definitions,
            FilePath file,
            JsonSchema rootSchema,
            JsonSchema schema,
            JToken token,
            int uidCount,
            string? propertyPath)
        {
            schema = definitions.GetDefinition(schema);
            switch (token)
            {
                // transform array and object is not supported yet
                case JArray array:
                    if (schema.Items.schema is null)
                    {
                        return array;
                    }

                    var newArray = new JArray();
                    foreach (var item in array)
                    {
                        newArray.Add(TransformContentCore(errors, definitions, file, rootSchema, schema.Items.schema, item, uidCount, propertyPath));
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
                        else if (schema.Properties.TryGetValue(key, out var propertySchema))
                        {
                            newObject[key] = TransformContentCore(
                                errors,
                                definitions,
                                file,
                                rootSchema,
                                propertySchema,
                                value,
                                uidCount,
                                JsonUtility.AddToPropertyPath(propertyPath, key));
                        }
                        else
                        {
                            newObject[key] = value;
                        }
                    }
                    return newObject;

                case JValue value:
                    return TransformScalar(errors.With(e => e.WithPropertyPath(propertyPath)), rootSchema, schema, file, value, propertyPath);

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
                    var (error, link, _) = _linkResolver.ResolveLink(content, file, file);
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

                    if (schema.ContentType == JsonSchemaContentType.Uid && (schema.MinReferenceCount != null || schema.MaxReferenceCount != null))
                    {
                        _uidReferenceCountList.Add((
                            new SourceInfo<string>(value.Value<string>(), value.GetSourceInfo()),
                            propertyPath,
                            schema.MinReferenceCount,
                            schema.MaxReferenceCount));
                    }
                    else
                    {
                        _xrefList.Add(new SourceInfo<string>(value.Value<string>(), value.GetSourceInfo()));
                    }

                    if (!_mustacheXrefSpec.ContainsKey((file, content)))
                    {
                        // the content here must be an UID, not href
                        var (xrefError, xrefSpec, href) = _xrefResolver.ResolveXrefSpec(
                            content, file, file, _monikerProvider.GetFileLevelMonikers(ErrorBuilder.Null, file));
                        errors.AddIfNotNull(xrefError);

                        var xrefSpecObj = xrefSpec is null ? null : JsonUtility.ToJObject(xrefSpec.ToExternalXrefSpec(href));

                        // Ensure these well known properties does not fallback to mustache parent variable scope
                        if (xrefSpecObj != null)
                        {
                            xrefSpecObj["uid"] ??= null;
                            xrefSpecObj["name"] ??= xrefSpecObj["uid"] ?? null;
                            xrefSpecObj["href"] ??= null;
                        }

                        _mustacheXrefSpec.TryAdd((file, content), xrefSpecObj);
                    }
                    return value;
            }

            return value;
        }
    }
}
