// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.RestApi
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Composition;
    using System.Linq;

    using Microsoft.DocAsCode.Build.Common;
    using Microsoft.DocAsCode.DataContracts.RestApi;
    using Microsoft.DocAsCode.Plugins;

    using Newtonsoft.Json.Linq;

    [Export(nameof(RestApiDocumentProcessor), typeof(IDocumentBuildStep))]
    public class BuildRestApiDocument : BuildReferenceDocumentBase
    {
        private static readonly HashSet<string> MarkupKeys = new HashSet<string> { "description" };

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
                        if (pair.Value is JValue jValue && jValue.Type == JTokenType.String)
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
}
