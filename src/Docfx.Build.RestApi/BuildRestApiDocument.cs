// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Composition;

using Docfx.Build.Common;
using Docfx.DataContracts.RestApi;
using Docfx.Plugins;

using Newtonsoft.Json.Linq;

namespace Docfx.Build.RestApi;

[Export(nameof(RestApiDocumentProcessor), typeof(IDocumentBuildStep))]
public class BuildRestApiDocument : BuildReferenceDocumentBase
{
    private static readonly HashSet<string> MarkupKeys = ["description"];

    public override string Name => nameof(BuildRestApiDocument);

    protected override void BuildArticle(IHostService host, FileModel model)
    {
        var restApi = (RestApiRootItemViewModel)model.Content;
        BuildItem(host, restApi, model);
        if (restApi.Children != null)
        {
            foreach (var item in restApi.Children)
            {
                BuildItem(host, item, model);
            }
        }
        if (restApi.Tags != null)
        {
            foreach (var tag in restApi.Tags)
            {
                BuildTag(host, tag, model);
            }
        }
    }

    public static RestApiItemViewModelBase BuildItem(IHostService host, RestApiItemViewModelBase item, FileModel model, Func<string, bool> filter = null)
    {
        var documents = model.Type == DocumentType.Overwrite ? host.LookupByUid(item.Uid) : [model];
        var openApi3 = documents?.Any(document => document.Content is RestApiRootItemViewModel root &&
            root.Metadata.GetValueOrDefault("specificationVersion") is string version && version.StartsWith("3.", StringComparison.Ordinal)) == true;
        item.Summary = Markup(host, item.Summary, model, filter);
        item.Description = Markup(host, item.Description, model, filter);
        if (model.Type != DocumentType.Overwrite)
        {
            item.Conceptual = Markup(host, item.Conceptual, model, filter);
            item.Remarks = Markup(host, item.Remarks, model, filter);
        }

        if (openApi3)
        {
            MarkupOpenApiMetadata(item.Metadata);
        }
        else if (item is RestApiRootItemViewModel rootModel)
        {
            // Mark up recursively for swagger root except for children and tags
            foreach (var jToken in rootModel.Metadata.Values.OfType<JToken>())
            {
                MarkupRecursive(jToken, host, model, filter);
            }
        }

        var childModel = item as RestApiChildItemViewModel;
        if (childModel?.Parameters != null)
        {
            foreach (var param in childModel.Parameters)
            {
                param.Description = Markup(host, param.Description, model, filter);

                if (openApi3)
                {
                    MarkupOpenApiMetadata(param.Metadata);
                }
                else
                {
                    foreach (var jToken in param.Metadata.Values.OfType<JToken>())
                    {
                        MarkupRecursive(jToken, host, model, filter);
                    }
                }
            }
        }
        if (childModel?.Responses != null)
        {
            foreach (var response in childModel.Responses)
            {
                response.Description = Markup(host, response.Description, model, filter);

                if (openApi3)
                {
                    MarkupOpenApiMetadata(response.Metadata);
                }
                else
                {
                    foreach (var jToken in response.Metadata.Values.OfType<JToken>())
                    {
                        MarkupRecursive(jToken, host, model, filter);
                    }
                }
            }
        }
        return item;

        void MarkupOpenApiMetadata(Dictionary<string, object> metadata)
        {
            MarkupDescription(metadata.GetValueOrDefault("info"));
            MarkupDescription(metadata.GetValueOrDefault("externalDocs"));
            foreach (var server in GetChildren(metadata.GetValueOrDefault("servers"))) MarkupDescription(server);
            foreach (var schema in GetChildren(metadata.GetValueOrDefault("schemas"))) MarkupSchema(schema);
            var body = metadata.GetValueOrDefault("requestBody");
            MarkupDescription(body);
            MarkupContent(GetProperty(body, "content"));
            MarkupSchema(metadata.GetValueOrDefault("schema"));
            MarkupContent(metadata.GetValueOrDefault("content"));
        }

        void MarkupDescription(object node)
        {
            var value = GetProperty(node, "description");
            if (value is JValue { Type: JTokenType.String } token) value = (string)token;
            if (value is not string description) return;
            var html = Markup(host, description, model, filter);
            if (node is JObject obj) obj["description"] = html;
            else if (node is Dictionary<object, object> dictionary) dictionary["description"] = html;
        }

        void MarkupContent(object content)
        {
            foreach (var media in GetChildren(content))
            {
                MarkupSchema(GetProperty(media, "schema"));
                MarkupSchema(GetProperty(media, "itemSchema"));
            }
        }

        void MarkupSchema(object schema)
        {
            MarkupDescription(schema);
            foreach (var property in GetChildren(GetProperty(schema, "properties"))) MarkupSchema(property);
            var items = GetProperty(schema, "items");
            if (items != null) MarkupSchema(items);
            foreach (var branch in GetChildren(GetProperty(schema, "allOf"))) MarkupSchema(branch);
            foreach (var composition in GetChildren(GetProperty(schema, "composition")))
                foreach (var branch in GetChildren(GetProperty(composition, "schemas"))) MarkupSchema(branch);
        }

        // Keep overwrite dictionaries/lists intact: JObjectMerger/JArrayMerger consume those types.
        static object GetProperty(object node, string name) => node switch
        {
            JObject obj => obj[name],
            Dictionary<object, object> dictionary => dictionary.GetValueOrDefault(name),
            _ => null
        };

        static IEnumerable<object> GetChildren(object node) => node switch
        {
            JObject obj => obj.PropertyValues(),
            Dictionary<object, object> dictionary => dictionary.Values,
            IEnumerable<object> array => array,
            _ => []
        };
    }

    private static void MarkupRecursive(JToken jToken, IHostService host, FileModel model, Func<string, bool> filter = null)
    {
        if (jToken is JArray jArray)
        {
            foreach (var item in jArray)
            {
                MarkupRecursive(item, host, model, filter);
            }
        }

        if (jToken is JObject jObject)
        {
            foreach (var pair in jObject)
            {
                if (MarkupKeys.Contains(pair.Key) && pair.Value != null)
                {
                    if (pair.Value is JValue { Type: JTokenType.String } jValue)
                    {
                        jObject[pair.Key] = Markup(host, (string)jValue, model, filter);
                    }
                }
                MarkupRecursive(jObject[pair.Key], host, model, filter);
            }
        }
    }

    public static RestApiTagViewModel BuildTag(IHostService host, RestApiTagViewModel tag, FileModel model, Func<string, bool> filter = null)
    {
        tag.Conceptual = Markup(host, tag.Conceptual, model, filter);
        tag.Description = Markup(host, tag.Description, model, filter);
        return tag;
    }

    private static string Markup(IHostService host, string markdown, FileModel model, Func<string, bool> filter = null)
    {
        if (string.IsNullOrEmpty(markdown))
        {
            return markdown;
        }

        if (filter != null && filter(markdown))
        {
            return markdown;
        }

        var mr = host.Markup(markdown, model.OriginalFileAndType);
        model.LinkToFiles = model.LinkToFiles.Union(mr.LinkToFiles);
        model.LinkToUids = model.LinkToUids.Union(mr.LinkToUids);

        var fls = model.FileLinkSources.ToDictionary(p => p.Key, p => p.Value);
        foreach (var pair in mr.FileLinkSources)
        {
            if (fls.TryGetValue(pair.Key, out ImmutableList<LinkSourceInfo> list))
            {
                fls[pair.Key] = list.AddRange(pair.Value);
            }
            else
            {
                fls[pair.Key] = pair.Value;
            }
        }
        model.FileLinkSources = fls.ToImmutableDictionary();

        var uls = model.UidLinkSources.ToDictionary(p => p.Key, p => p.Value);
        foreach (var pair in mr.UidLinkSources)
        {
            if (uls.TryGetValue(pair.Key, out ImmutableList<LinkSourceInfo> list))
            {
                uls[pair.Key] = list.AddRange(pair.Value);
            }
            else
            {
                uls[pair.Key] = pair.Value;
            }
        }
        model.UidLinkSources = uls.ToImmutableDictionary();

        return mr.Html;
    }
}
