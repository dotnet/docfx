// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.RestApi
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Composition;
    using System.IO;
    using System.Linq;

    using Microsoft.DocAsCode.Build.Common;
    using Microsoft.DocAsCode.Common;
    using Microsoft.DocAsCode.DataContracts.RestApi;
    using Microsoft.DocAsCode.Plugins;

    using Newtonsoft.Json.Linq;

    [Export(nameof(RestApiDocumentProcessor), typeof(IDocumentBuildStep))]
    public class BuildRestApiDocument : BaseDocumentBuildStep
    {
        private static readonly HashSet<string> MarkupKeys = new HashSet<string> { "description" };

        public override string Name => nameof(BuildRestApiDocument);

        public override int BuildOrder => 0;

        public override void Build(FileModel model, IHostService host)
        {
            switch (model.Type)
            {
                case DocumentType.Article:
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
                    break;
                case DocumentType.Overwrite:
                    BuildItem(host, model);
                    break;
                default:
                    throw new NotSupportedException();
            }
        }

        public static RestApiItemViewModelBase BuildItem(IHostService host, RestApiItemViewModelBase item, FileModel model, Func<string, bool> filter = null)
        {
            item.Summary = Markup(host, item.Summary, model, filter);
            item.Description = Markup(host, item.Description, model, filter);
            if (model.Type != DocumentType.Overwrite)
            {
                item.Conceptual = Markup(host, item.Conceptual, model, filter);
                item.Remarks = Markup(host, item.Remarks, model, filter);
            }

            var rootModel = item as RestApiRootItemViewModel;
            if (rootModel != null)
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

                    foreach (var jToken in param.Metadata.Values.OfType<JToken>())
                    {
                        MarkupRecursive(jToken, host, model, filter);
                    }
                }
            }
            if (childModel?.Responses != null)
            {
                foreach (var response in childModel.Responses)
                {
                    response.Description = Markup(host, response.Description, model, filter);

                    foreach (var jToken in response.Metadata.Values.OfType<JToken>())
                    {
                        MarkupRecursive(jToken, host, model, filter);
                    }
                }
            }
            return item;
        }

        private static void MarkupRecursive(JToken jToken, IHostService host, FileModel model, Func<string, bool> filter = null)
        {
            var jArray = jToken as JArray;
            if (jArray != null)
            {
                foreach (var item in jArray)
                {
                    MarkupRecursive(item, host, model, filter);
                }
            }

            var jObject = jToken as JObject;
            if (jObject != null)
            {
                foreach (var pair in jObject)
                {
                    if (MarkupKeys.Contains(pair.Key) && pair.Value != null)
                    {
                        var jValue = pair.Value as JValue;
                        if (jValue != null && jValue.Type == JTokenType.String)
                        {
                            jObject[pair.Key] = Markup(host, (string)jValue, model, filter);
                        }
                    }
                    MarkupRecursive(jObject[pair.Key], host, model, filter);
                }
            }
        }

        private static void BuildItem(IHostService host, FileModel model)
        {
            var overwrites = MarkdownReader.ReadMarkdownAsOverwrite(host, model.FileAndType).ToList();
            model.Content = overwrites;
            model.LinkToFiles = overwrites.SelectMany(o => o.LinkToFiles).ToImmutableHashSet();
            model.LinkToUids = overwrites.SelectMany(o => o.LinkToUids).ToImmutableHashSet();
            model.FileLinkSources = overwrites.SelectMany(o => o.FileLinkSources).ToImmutableDictionary();
            model.UidLinkSources = overwrites.SelectMany(o => o.UidLinkSources).ToImmutableDictionary();
            model.Uids = (from item in overwrites
                          select new UidDefinition(
                              item.Uid,
                              model.LocalPathFromRoot,
                              item.Documentation.StartLine + 1)).ToImmutableArray();
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

            var mr = host.Markup(markdown, model.FileAndType);
            model.LinkToFiles = model.LinkToFiles.Union(mr.LinkToFiles);
            model.LinkToUids = model.LinkToUids.Union(mr.LinkToUids);

            var fls = model.FileLinkSources.ToDictionary(p => p.Key, p => p.Value);
            foreach (var pair in mr.FileLinkSources)
            {
                ImmutableList<LinkSourceInfo> list;
                if (fls.TryGetValue(pair.Key, out list))
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
                ImmutableList<LinkSourceInfo> list;
                if (uls.TryGetValue(pair.Key, out list))
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
}
