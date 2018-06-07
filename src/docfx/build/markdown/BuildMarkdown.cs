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
        public static Task<IEnumerable<DependencyItem>> Build(Context context, Document file, TableOfContentsMap tocMap, Action<Document> buildChild)
        {
            var dependencyItems = new ConcurrentBag<DependencyItem>();
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
            return Task.FromResult<IEnumerable<DependencyItem>>(dependencyItems);

            string ResolveHref(Document relativeTo, string href, Document resultRelativeTo)
            {
                var (link, buildItem) = relativeTo.TryResolveHref(href, resultRelativeTo);
                if (buildItem != null)
                {
                    // inclusion's dependencies belong to their parent
                    dependencyItems.Add(new DependencyItem(buildItem, DependencyType.File));
                    buildChild(buildItem);
                }
                return link;
            }

            (string str, Document include) ResolveContent(Document relativeTo, string href)
            {
                var (str, include) = relativeTo.TryResolveContent(href);

                if (include != null)
                {
                    // inclusion's dependencies belong to their parent
                    dependencyItems.Add(new DependencyItem(include, DependencyType.Inclusion));
                }

                return (str, include);
            }
        }
    }
}
