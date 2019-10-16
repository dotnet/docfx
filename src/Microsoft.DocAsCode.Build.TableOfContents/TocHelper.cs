// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.TableOfContents
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.IO;
    using System.Linq;

    using Microsoft.DocAsCode.Common;
    using Microsoft.DocAsCode.DataContracts.Common;
    using Microsoft.DocAsCode.Plugins;

    public static class TocHelper
    {
        private static readonly YamlDeserializerWithFallback _deserializer =
            YamlDeserializerWithFallback.Create<TocViewModel>()
            .WithFallback<TocRootViewModel>();

        public static (List<FileModel> tocModels, HashSet<string> includedTocs) ResolveToc(ImmutableList<FileModel> models, IHostService host)
        {
            var tocCache = new Dictionary<string, TocItemInfo>(FilePathComparer.OSPlatformSensitiveStringComparer);
            var nonReferencedTocModels = new List<FileModel>();
            var referencedToc = new HashSet<string>(FilePathComparer.OSPlatformSensitiveStringComparer);

            foreach (var model in models)
            {
                tocCache[model.OriginalFileAndType.FullPath] = new TocItemInfo(model.OriginalFileAndType, (TocItemViewModel)model.Content);
            }
            var tocResolver = new TocResolver(host, tocCache);
            foreach (var key in tocCache.Keys.ToList())
            {
                tocCache[key] = tocResolver.Resolve(key);
            }

            foreach (var model in models)
            {
                // If the TOC file is referenced by other TOC, remove it from the collection
                var tocItemInfo = tocCache[model.OriginalFileAndType.FullPath];
                if (!tocItemInfo.IsReferenceToc)
                {
                    model.Content = tocItemInfo.Content;
                    nonReferencedTocModels.Add(model);
                }
                else
                {
                    referencedToc.Add(model.Key);
                }
            }

            return (nonReferencedTocModels, referencedToc);
        }

        [Obsolete("Use ResolveToc")]
        public static IEnumerable<FileModel> Resolve(ImmutableList<FileModel> models, IHostService host)
        {
            var (result, _) = ResolveToc(models, host);
            return result;
        }

        public static TocItemViewModel LoadSingleToc(string file)
        {
            if (string.IsNullOrEmpty(file))
            {
                throw new ArgumentNullException(nameof(file));
            }

            if (!EnvironmentContext.FileAbstractLayer.Exists(file))
            {
                throw new FileNotFoundException($"File {file} does not exist.", file);
            }

            var fileType = Utility.GetTocFileType(file);
            try
            {
                if (fileType == TocFileType.Markdown)
                {
                    return new TocItemViewModel
                    {
                        Items = MarkdownTocReader.LoadToc(EnvironmentContext.FileAbstractLayer.ReadAllText(file), file)
                    };
                }
                else if (fileType == TocFileType.Yaml)
                {
                    return LoadYamlToc(file);
                }
            }
            catch (Exception e)
            {
                var message = $"{file} is not a valid TOC File: {e.Message}";
                Logger.LogError(message);
                throw new DocumentException(message, e);
            }

            throw new NotSupportedException($"{file} is not a valid TOC file, supported TOC files should be either \"{Constants.TableOfContents.MarkdownTocFileName}\" or \"{Constants.TableOfContents.YamlTocFileName}\".");
        }

        public static TocItemViewModel LoadYamlToc(string file)
        {
            if (string.IsNullOrEmpty(file))
            {
                throw new ArgumentNullException(nameof(file));
            }

            object obj;
            try
            {
                obj = _deserializer.Deserialize(file);
            }
            catch (Exception ex)
            {
                throw new NotSupportedException($"{file} is not a valid TOC file, detail: {ex.Message}.", ex);
            }
            if (obj is TocViewModel vm)
            {
                return new TocItemViewModel
                {
                    Items = vm,
                };
            }
            if (obj is TocRootViewModel root)
            {
                return new TocItemViewModel
                {
                    Items = root.Items,
                    Metadata = root.Metadata,
                };
            }
            throw new NotSupportedException($"{file} is not a valid TOC file.");
        }
    }
}
