// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.TableOfContents
{
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Linq;

    using Microsoft.DocAsCode.Common;
    using Microsoft.DocAsCode.DataContracts.Common;
    using Microsoft.DocAsCode.Plugins;
    using Microsoft.DocAsCode.Exceptions;

    public class TocResolverUtility
    {
        public static IEnumerable<FileModel> Resolve(ImmutableList<FileModel> models, IHostService host)
        {
            var tocCache = new Dictionary<string, TocItemInfo>(FilePathComparer.OSPlatformSensitiveStringComparer);
            foreach (var model in models)
            {
                if (!tocCache.ContainsKey(model.OriginalFileAndType.FullPath))
                {
                    tocCache[model.OriginalFileAndType.FullPath] = new TocItemInfo(model.OriginalFileAndType, (TocItemViewModel)model.Content);
                }
            }
            var tocResolver = new TocResolver(host, tocCache);
            foreach (var key in tocCache.Keys.ToList())
            {
                tocCache[key] = tocResolver.Resolve(key);
            }

            foreach (var model in models)
            {
                if (!tocCache.TryGetValue(model.OriginalFileAndType.FullPath, out TocItemInfo tocItemInfo))
                {
                    throw new DocfxException($"File {model.OriginalFileAndType.FullPath} cannot be found from toc cache.");
                }

                // If the TOC file is referenced by other TOC, remove it from the collection
                if (!tocItemInfo.IsReferenceToc)
                {
                    model.Content = tocItemInfo.Content;
                    yield return model;
                }
            }
        }
    }
}
