// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;

namespace Microsoft.Docs.Build
{
    /// <summary>
    /// Generate Service Page.
    /// </summary>
    internal class ServicePageGenerator
    {
        private readonly Input _input;
        private readonly JoinTOCConfig _joinTOCConfig;
        private readonly RepositoryProvider _repositoryProvider;
        private readonly Output _output;

        public ServicePageGenerator(
            Input input,
            JoinTOCConfig joinTOCConfig,
            RepositoryProvider repositoryProvider,
            Output output)
        {
            _input = input;
            _joinTOCConfig = joinTOCConfig;
            _repositoryProvider = repositoryProvider;
            _output = output;
        }

        public void GenerateServicePageFromTopLevelTOC(
            TableOfContentsNode node,
            ConcurrentBag<FilePath> results,
            string directoryName = "")
        {
            if (node == null || string.IsNullOrEmpty(node.Name))
            {
                return;
            }

            var filename = Regex.Replace(node.Name, @"\s+", "");

            if (!string.IsNullOrEmpty(node.LandingPageType))
            {
                var baseDir = string.IsNullOrEmpty(_joinTOCConfig.OutputPath) ? Directory.GetCurrentDirectory() : _joinTOCConfig.OutputPath.Replace("..", "");
                var servicePagePath = FilePath.Generated(new PathString($"./{baseDir}/{directoryName}/{filename}.yml"));

                var name = node.Name;
                var fullName = node.Name;
                var uid = node.Uid;
                var href = node.Href;

                var children = new List<SourceInfo<ServicePageModel>>();
                foreach (var item in node.Items)
                {
                    ServicePageModel newItem;
                    var newItemName = item.Value.Name;
                    var newItemHref = item.Value.Href;
                    var newItemUid = item.Value.Uid;

                    if (!string.IsNullOrEmpty(newItemHref.Value))
                    {
                        if (_repositoryProvider.Repository != null && item.Source != null)
                        {
                            var itemFilePath = Path.GetDirectoryName(Path.Combine(_repositoryProvider.Repository.Path, item.Source.File.Path));
                            var itemHrefPath = Path.Combine(itemFilePath == null ? "" : itemFilePath, newItemHref.Value);
                            var outDir = Path.Combine(_output.OutputPath, servicePagePath.Path);
                            var newhref = Path.GetRelativePath(outDir == null ? "" : outDir, itemHrefPath);
                            newItemHref = new SourceInfo<string?>(newhref);
                        }

                        newItem = new ServicePageModel(newItemName, newItemHref, new SourceInfo<string?>(null));
                    }
                    else
                    {
                        newItem = new ServicePageModel(newItemName, new SourceInfo<string?>(null), newItemUid);
                    }

                    children.Add(new SourceInfo<ServicePageModel>(newItem));
                }

                var langs = new List<string?>();
                if (_joinTOCConfig.ContainerPageMetadata != null)
                {
                    foreach (var lang in _joinTOCConfig.ContainerPageMetadata)
                    {
                        if (lang.Key.Equals("langs") && lang.Value != null)
                        {
                            langs = lang.Value.ToObject<List<string?>>();
                        }
                    }
                }
                var landingPageType = node.LandingPageType.Value?.ToLowerInvariant();
                var pageType = new SourceInfo<string?>(landingPageType);

                var yamlMime = new SourceInfo<string?>("ReferenceContainer");

                results.Add(servicePagePath);

                var servicePageToken = new ServicePageModel(name, fullName, href, uid, children, langs, pageType, yamlMime);

                _input.AddGeneratedContent(servicePagePath, JsonUtility.ToJObject(servicePageToken));
            }

            // DFS
            foreach (var item in node.Items)
            {
                GenerateServicePageFromTopLevelTOC(item, results, $"{directoryName}/{filename}");
            }
        }
    }
}
