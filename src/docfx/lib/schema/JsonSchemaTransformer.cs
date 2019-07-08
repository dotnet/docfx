// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Newtonsoft.Json.Linq;

namespace Microsoft.Docs.Build
{
    internal class JsonSchemaTransformer
    {
        private readonly JsonSchema _schema;
        private readonly JsonSchemaDefinition _definitions;
        private readonly ConcurrentDictionary<Document, Dictionary<string, Lazy<JToken>>> _properties;

        public JsonSchemaTransformer(JsonSchema schema)
        {
            _schema = schema;
            _definitions = new JsonSchemaDefinition(schema);
            _properties = new ConcurrentDictionary<Document, Dictionary<string, Lazy<JToken>>>();
        }

        public (List<Error> errors, JToken token) TransformContent(Document file, Context context, JToken token)
        {
            var errors = new List<Error>();
            var transformedToken = TransformToken(file, context, _schema, token, errors);
            return (errors, transformedToken);
        }

        public (List<Error> errors, Dictionary<string, (bool, Dictionary<string, Lazy<JToken>>)> properties) TransformXref(Document file, Context context, JToken token)
        {
            var errors = new List<Error>();
            var xrefPropertiesGroupByUid = new Dictionary<string, (bool, Dictionary<string, Lazy<JToken>>)>();
            var uidJsonPaths = new HashSet<string>();
            var cachedProperties = _properties.GetOrAdd(file, new Dictionary<string, Lazy<JToken>>());

            TransformXref(_schema, token);

            return (errors, xrefPropertiesGroupByUid);

            void TransformXref(JsonSchema schema, JToken node)
            {
                schema = _definitions.GetDefinition(schema);
                switch (node)
                {
                    case JObject obj:
                        var uid = obj.TryGetValue("uid", out var uidValue) && uidValue is JValue uidJValue && uidJValue.Value is string uidStr ? uidStr : null;

                        if (uid == null)
                        {
                            TransformObjectXref(obj);
                            break;
                        }

                        if (uidJsonPaths.Add(uidValue.Path) && xrefPropertiesGroupByUid.ContainsKey(uid))
                        {
                            // TODO: should throw warning and take the first one order by json path
                            errors.Add(Errors.UidConflict(uid));
                            TransformObjectXref(obj);
                            break;
                        }

                        if (!xrefPropertiesGroupByUid.TryGetValue(uid, out _))
                        {
                            xrefPropertiesGroupByUid[uid] = (obj.Parent == null, new Dictionary<string, Lazy<JToken>>());
                        }

                        TransformObjectXref(obj, (propertySchema, key, value) =>
                        {
                            if (schema.XrefProperties.Contains(key))
                            {
                                xrefPropertiesGroupByUid[uid].Item2[key] = new Lazy<JToken>(
                                   () =>
                                   {
                                       return TransformToken(file, context, propertySchema, value, errors);
                                   }, LazyThreadSafetyMode.PublicationOnly);
                                return true;
                            }

                            return false;
                        });
                        break;

                    case JArray array:
                        foreach (var item in array)
                        {
                            if (schema.Items != null)
                                TransformXref(schema.Items, item);
                        }
                        break;
                }

                void TransformObjectXref(JObject obj, Func<JsonSchema, string, JToken, bool> action = null)
                {
                    foreach (var (key, value) in obj)
                    {
                        if (schema.Properties.TryGetValue(key, out var propertySchema))
                        {
                            if (action?.Invoke(propertySchema, key, value) ?? false)
                                continue;

                            TransformXref(propertySchema, value);
                        }
                    }
                }
            }
        }

        private JToken TransformToken(Document file, Context context, JsonSchema schema, JToken token, List<Error> errors)
        {
            return TransformToken(schema, token);

            JToken TransformToken(JsonSchema subSchema, JToken node)
            {
                subSchema = _definitions.GetDefinition(subSchema);
                switch (node)
                {
                    // transform array and object is not supported yet
                    case JArray array:
                        var newArray = new JArray();
                        foreach (var item in array)
                        {
                            var newItem = TransformToken(subSchema.Items, item);
                            newArray.Add(newItem);
                        }

                        return newArray;

                    case JObject obj:
                        var newObject = new JObject();
                        foreach (var (key, value) in obj)
                        {
                            if (subSchema.Properties.TryGetValue(key, out var propertySchema))
                            {
                                newObject[key] = TransformToken(propertySchema, value);
                            }
                            else
                            {
                                newObject[key] = value;
                            }
                        }
                        return newObject;

                    case JValue value:
                        return TransformScalar(subSchema, file, context, value, errors);
                }

                throw new NotSupportedException();
            }
        }

        private JToken TransformScalar(JsonSchema schema, Document file, Context context, JValue value, List<Error> errors)
        {
            if (value.Type == JTokenType.Null || schema.ContentType == JsonSchemaContentType.None)
            {
                return value;
            }

            var sourceInfo = JsonUtility.GetSourceInfo(value);
            var content = new SourceInfo<string>(value.Value<string>(), sourceInfo);

            switch (schema.ContentType)
            {
                case JsonSchemaContentType.Href:
                    var (error, link, _) = context.DependencyResolver.ResolveRelativeLink(file, content, file);
                    errors.AddIfNotNull(error);
                    content = new SourceInfo<string>(link, content);
                    break;

                case JsonSchemaContentType.Markdown:
                    var (markupErrors, html) = MarkdownUtility.ToHtml(
                        context,
                        content,
                        file,
                        MarkdownPipelineType.Markdown);

                    errors.AddRange(markupErrors);
                    content = new SourceInfo<string>(html, content);
                    break;

                case JsonSchemaContentType.InlineMarkdown:
                    var (inlineMarkupErrors, inlineHtml) = MarkdownUtility.ToHtml(
                        context,
                        content,
                        file,
                        MarkdownPipelineType.InlineMarkdown);

                    errors.AddRange(inlineMarkupErrors);
                    content = new SourceInfo<string>(inlineHtml, content);
                    break;

                // TODO: remove JsonSchemaContentType.Html after LandingData is migrated
                case JsonSchemaContentType.Html:
                    var htmlWithLinks = HtmlUtility.TransformLinks(content, (href, _) =>
                    {
                        var (htmlError, htmlLink, _) = context.DependencyResolver.ResolveRelativeLink(
                            file, new SourceInfo<string>(href, content), file);
                        errors.AddIfNotNull(htmlError);
                        return htmlLink;
                    });

                    content = new SourceInfo<string>(htmlWithLinks, content);
                    break;

                case JsonSchemaContentType.Xref:

                    var (xrefError, xrefLink, _, xrefSpec) = context.DependencyResolver.ResolveAbsoluteXref(content, file);

                    if (xrefSpec is InternalXrefSpec internalSpec)
                    {
                        xrefSpec = internalSpec.ToExternalXrefSpec(context, forXrefMapOutput: false);
                    }
                    errors.AddIfNotNull(xrefError);

                    if (xrefSpec != null)
                    {
                        var specObj = JsonUtility.ToJObject(xrefSpec);
                        JsonUtility.SetSourceInfo(specObj, content);
                        return specObj;
                    }

                    content = new SourceInfo<string>(null, content);
                    break;
            }

            value = new JValue(content.Value);
            JsonUtility.SetSourceInfo(value, content);
            return value;
        }
    }
}
