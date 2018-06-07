// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Microsoft.Docs.Build
{
    internal static class BuildMarkdown
    {
        public static Task<DependencyMap> Build(Context context, Document file, TableOfContentsMap tocMap, Action<Document> buildChild)
        {
            var dependencyItems = new HashSet<DependencyItem>();
            var inclusionDependency = new Dictionary<Document, HashSet<DependencyItem>>();
            var markdown = file.ReadText();

            var (html, markup) = MarkdownUtility.Markup(markdown, file, context, ResolveHref, ResolveContent);

            var metadata = JsonUtility.Merge(Metadata.GetFromConfig(file), markup.Metadata);

            var content = markup.HasHtml ? HtmlUtility.TransformHtml(html, node => node.StripTags()) : html;

            var model = new PageModel
            {
                Content = content,
                Metadata = new PageMetadata
                {
                    Title = markup.Title,
                    Metadata = metadata,
                },
            };

            context.WriteJson(model, file.OutputPath);
            return Task.FromResult(new DependencyMap(dependencyItems));

            string ResolveHref(Document relativeTo, string href, Document resultRelativeTo)
            {
                var (link, buildItem) = relativeTo.TryResolveHref(href, resultRelativeTo);
                if (buildItem != null)
                {
                    buildChild(buildItem);

                    AddDependenyItem(relativeTo, buildItem, DependencyType.File);
                }
                return link;
            }

            (string str, Document include) ResolveContent(Document relativeTo, string href)
            {
                var (str, buildItem) = relativeTo.TryResolveContent(href);

                AddDependenyItem(relativeTo, buildItem, DependencyType.Inclusion);

                return (str, buildItem);
            }

            void AddDependenyItem(Document relativeTo, Document buildItem, DependencyType type)
            {
                if (buildItem != null)
                {
                    if (relativeTo.Equals(file))
                    {
                        dependencyItems.Add(new DependencyItem(buildItem, type));
                    }
                    else
                    {
                        if (!inclusionDependency.TryGetValue(relativeTo, out var inclusionDependencyItems))
                        {
                            inclusionDependencyItems = inclusionDependency[relativeTo] = new HashSet<DependencyItem>();
                        }

                        inclusionDependencyItems.Add(new DependencyItem(buildItem, type));
                    }
                }

            }
        }
    }
}
