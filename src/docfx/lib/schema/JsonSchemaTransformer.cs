// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
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

        private readonly ConcurrentDictionary<FilePath, int> _uidCountCache = new ConcurrentDictionary<FilePath, int>(ReferenceEqualsComparer.Default);
        private readonly ConcurrentDictionary<(FilePath, string), JObject?> _mustacheXrefSpec = new ConcurrentDictionary<(FilePath, string), JObject?>();

        private static readonly ThreadLocal<Stack<SourceInfo<string>>> t_recursionDetector
                          = new ThreadLocal<Stack<SourceInfo<string>>>(() => new Stack<SourceInfo<string>>());

        public JsonSchemaTransformer(
            DocumentProvider documentProvider,
            MarkdownEngine markdownEngine,
            LinkResolver linkResolver,
            XrefResolver xrefResolver,
            ErrorBuilder errors,
            MonikerProvider monikerProvider,
            TemplateEngine templateEngine)
        {
            _documentProvider = documentProvider;
            _markdownEngine = markdownEngine;
            _linkResolver = linkResolver;
            _xrefResolver = xrefResolver;
            _errors = errors;
            _monikerProvider = monikerProvider;
            _templateEngine = templateEngine;
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
            return TransformContentCore(errors, definitions, file, schema, token, uidCount);
        }

        public IReadOnlyList<InternalXrefSpec> LoadXrefSpecs(ErrorBuilder errors, JsonSchema schema, FilePath file, JToken token)
        {
            var xrefSpecs = new List<InternalXrefSpec>();
            var definitions = new JsonSchemaDefinition(schema);
            var uidCount = _uidCountCache.GetOrAdd(file, GetFileUidCount(definitions, schema, token));
            LoadXrefSpecsCore(errors, file, schema, definitions, token, xrefSpecs, uidCount);
            return xrefSpecs;
        }

        private void LoadXrefSpecsCore(
            ErrorBuilder errors,
            FilePath file,
            JsonSchema schema,
            JsonSchemaDefinition definitions,
            JToken node,
            List<InternalXrefSpec> xrefSpecs,
            int uidCount)
        {
            schema = definitions.GetDefinition(schema);
            switch (node)
            {
                case JObject obj:
                    if (IsXrefSpec(obj, schema, out var uid))
                    {
                        xrefSpecs.Add(LoadXrefSpec(errors, definitions, file, schema, uid, obj, uidCount));
                    }

                    foreach (var (key, value) in obj)
                    {
                        if (value != null && schema.Properties.TryGetValue(key, out var propertySchema))
                        {
                            LoadXrefSpecsCore(errors, file, propertySchema, definitions, value, xrefSpecs, uidCount);
                        }
                    }
                    break;
                case JArray array when schema.Items.schema != null:
                    foreach (var item in array)
                    {
                        LoadXrefSpecsCore(errors, file, schema.Items.schema, definitions, item, xrefSpecs, uidCount);
                    }
                    break;
            }
        }

        private InternalXrefSpec LoadXrefSpec(
            ErrorBuilder errors,
            JsonSchemaDefinition definitions,
            FilePath file,
            JsonSchema schema,
            SourceInfo<string> uid,
            JObject obj,
            int uidCount)
        {
            var href = GetXrefHref(file, uid, uidCount, obj.Parent == null);
            var monikers = _monikerProvider.GetFileLevelMonikers(errors, file);
            var xref = new InternalXrefSpec(uid, href, file, monikers, obj.Parent?.Path);

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
                    () => LoadXrefProperty(definitions, file, uid, value, propertySchema, uidCount),
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
                    if (IsXrefSpec(obj, schema, out _))
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

        private static bool IsXrefSpec(JObject obj, JsonSchema schema, out SourceInfo<string> uid)
        {
            uid = default;

            // A xrefspec MUST be named uid, and the schema contentType MUST also be uid
            if (obj.TryGetValue<JValue>("uid", out var uidValue) && uidValue.Value is string tempUid &&
                schema.Properties.TryGetValue("uid", out var uidSchema) && uidSchema.ContentType == JsonSchemaContentType.Uid)
            {
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
            JsonSchema schema,
            int uidCount)
        {
            var recursionDetector = t_recursionDetector.Value!;
            if (recursionDetector.Contains(uid))
            {
                throw Errors.Link.CircularReference(uid, uid, recursionDetector, uid => $"{uid} ({uid.Source})").ToException();
            }

            try
            {
                recursionDetector.Push(uid);
                return TransformContentCore(_errors, definitions, file, schema, value, uidCount);
            }
            finally
            {
                Debug.Assert(recursionDetector.Count > 0);
                recursionDetector.Pop();
            }
        }

        private JToken TransformContentCore(ErrorBuilder errors, JsonSchemaDefinition definitions, FilePath file, JsonSchema schema, JToken token, int uidCount)
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
                        newArray.Add(TransformContentCore(errors, definitions, file, schema.Items.schema, item, uidCount));
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
                            newObject[key] = TransformContentCore(errors, definitions, file, propertySchema, value, uidCount);
                        }
                        else
                        {
                            newObject[key] = value;
                        }
                    }
                    return newObject;

                case JValue value:
                    return TransformScalar(errors, schema, file, value);

                default:
                    throw new NotSupportedException();
            }
        }

        private JToken TransformScalar(ErrorBuilder errors, JsonSchema schema, FilePath file, JValue value)
        {
            if (value.Type == JTokenType.Null || schema.ContentType is null)
            {
                return value;
            }

            var sourceInfo = JsonUtility.GetSourceInfo(value);
            var content = new SourceInfo<string>(value.Value<string>(), sourceInfo);

            switch (schema.ContentType)
            {
                case JsonSchemaContentType.Href:
                    var (error, link, _) = _linkResolver.ResolveLink(content, file, file);
                    errors.AddIfNotNull(error);
                    return link;

                case JsonSchemaContentType.Markdown:

                    var mime = _documentProvider.GetMime(file);
                    var rootSchema = _templateEngine.GetSchema(_documentProvider.GetMime(file));

                    // todo: use BuildPage.CreateHtmlContent() when we only validate markdown properties' bookmarks
                    return _markdownEngine.ToHtml(errors, content, file, MarkdownPipelineType.Markdown, null, rootSchema.ContentFallback);

                case JsonSchemaContentType.InlineMarkdown:

                    mime = _documentProvider.GetMime(file);
                    rootSchema = _templateEngine.GetSchema(_documentProvider.GetMime(file));

                    // todo: use BuildPage.CreateHtmlContent() when we only validate markdown properties' bookmarks
                    return _markdownEngine.ToHtml(errors, content, file, MarkdownPipelineType.InlineMarkdown, null, rootSchema.ContentFallback);

                // TODO: remove JsonSchemaContentType.Html after LandingData is migrated
                case JsonSchemaContentType.Html:

                    return HtmlUtility.TransformHtml(content, (ref HtmlReader reader, ref HtmlWriter writer, ref HtmlToken token) =>
                    {
                        HtmlUtility.TransformLink(ref token, null, href =>
                        {
                            var (htmlError, htmlLink, _) = _linkResolver.ResolveLink(new SourceInfo<string>(href, content), file, file);
                            errors.AddIfNotNull(htmlError);
                            return htmlLink;
                        });
                    });

                case JsonSchemaContentType.Uid:
                case JsonSchemaContentType.Xref:
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
