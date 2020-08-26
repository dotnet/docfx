// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
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

            if (!string.IsNullOrEmpty(node.LandingPageType.ToString()))
            {
                var baseDir = string.IsNullOrEmpty(_joinTOCConfig.OutputFolder) ? Directory.GetCurrentDirectory() : _joinTOCConfig.OutputFolder.Replace("..", "");
                var servicePagePath = FilePath.Generated(new PathString($"./{baseDir}/{directoryName}/{filename}.yml"));

                var name = node.Name;
                var fullName = node.Name;
                var uid = node.Uid;
                var href = node.Href;

                var children = new List<ServicePageItem>();
                foreach (var item in node.Items)
                {
                    ServicePageItem newItem;
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

                        newItem = new ServicePageItem(newItemName, newItemHref, new SourceInfo<string?>(null));
                    }
                    else
                    {
                        newItem = new ServicePageItem(newItemName, new SourceInfo<string?>(null), newItemUid);
                    }

                    children.Add(newItem);
                }

                var langs = new List<string?>();
                if (_joinTOCConfig.ContainerPageMetadata != null)
                {
                    _joinTOCConfig.ContainerPageMetadata.TryGetValue("langs", out JToken? lang);
                    langs = lang?.ToObject<List<string?>>();
                }

                var pageType = node.LandingPageType.Value;
                results.Add(servicePagePath);
                var servicePageToken = new ServicePageModel(name, fullName, href, uid, children, langs, pageType);
                _input.AddGeneratedContent(servicePagePath, JsonUtility.ToJObject(servicePageToken), "ReferenceContainer");
            }

            foreach (var item in node.Items)
            {
                GenerateServicePageFromTopLevelTOC(item, results, $"{directoryName}/{filename}");
            }
        }
    }
}
