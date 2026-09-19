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
        var preserveLiteralData = item.Metadata.GetValueOrDefault("_preserveLiteralData") is true;
        item.Summary = Markup(host, item.Summary, model, filter);
        item.Description = Markup(host, item.Description, model, filter);
        if (model.Type != DocumentType.Overwrite)
        {
            item.Conceptual = Markup(host, item.Conceptual, model, filter);
            item.Remarks = Markup(host, item.Remarks, model, filter);
        }

        if (item is RestApiRootItemViewModel rootModel)
        {
            // Mark up recursively for swagger root except for children and tags
            foreach (var jToken in GetMarkupTokens(rootModel.Metadata, preserveLiteralData))
            {
                MarkupRecursive(jToken, host, model, filter, preserveLiteralData);
            }
        }

        var childModel = item as RestApiChildItemViewModel;
        if (childModel != null && preserveLiteralData)
        {
            foreach (var key in new[] { "requestBody", "servers" })
            {
                if (childModel.Metadata.GetValueOrDefault(key) is JToken value)
                {
                    MarkupRecursive(value, host, model, filter, preserveLiteralData);
                }
            }
        }
        if (childModel?.Parameters != null)
        {
            foreach (var param in childModel.Parameters)
            {
                param.Description = Markup(host, param.Description, model, filter);

                foreach (var jToken in GetMarkupTokens(param.Metadata, preserveLiteralData))
                {
                    MarkupRecursive(jToken, host, model, filter, preserveLiteralData);
                }
            }
        }
        if (childModel?.Responses != null)
        {
            foreach (var response in childModel.Responses)
            {
                response.Description = Markup(host, response.Description, model, filter);

                foreach (var jToken in GetMarkupTokens(response.Metadata, preserveLiteralData))
                {
                    MarkupRecursive(jToken, host, model, filter, preserveLiteralData);
                }
            }
        }
        return item;
    }

    private static IEnumerable<JToken> GetMarkupTokens(Dictionary<string, object> metadata, bool preserveLiteralData) =>
        metadata.Where(pair => !preserveLiteralData || !pair.Key.StartsWith("x-", StringComparison.Ordinal))
            .Select(pair => pair.Value).OfType<JToken>();

    private static void MarkupRecursive(JToken jToken, IHostService host, FileModel model, Func<string, bool> filter = null, bool preserveLiteralData = false)
    {
        if (jToken is JArray jArray)
        {
            foreach (var item in jArray)
            {
                MarkupRecursive(item, host, model, filter, preserveLiteralData);
            }
        }

        if (jToken is JObject jObject)
        {
            foreach (var pair in jObject)
            {
                if (preserveLiteralData && (pair.Key.StartsWith("x-", StringComparison.Ordinal) ||
                    (jObject.ContainsKey("type") && pair.Key is "example" or "examples" or "enum" or "default" or "const")))
                {
                    continue;
                }
                if (MarkupKeys.Contains(pair.Key) && pair.Value != null)
                {
                    if (pair.Value is JValue { Type: JTokenType.String } jValue)
                    {
                        jObject[pair.Key] = Markup(host, (string)jValue, model, filter);
                    }
                }
                MarkupRecursive(jObject[pair.Key], host, model, filter, preserveLiteralData);
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
