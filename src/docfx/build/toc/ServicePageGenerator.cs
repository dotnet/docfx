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
        private readonly PathString _docsetPath;
        private readonly Input _input;
        private readonly JoinTOCConfig _joinTOCConfig;

        public ServicePageGenerator(
            PathString docsetPath,
            Input input,
            JoinTOCConfig joinTOCConfig)
        {
            _docsetPath = docsetPath;
            _input = input;
            _joinTOCConfig = joinTOCConfig;
        }

        public void GenerateServicePageFromTopLevelTOC(
            TableOfContentsNode node,
            List<FilePath> results,
            string directoryName = "")
        {
            if (node == null || string.IsNullOrEmpty(node.Name))
            {
                return;
            }

            var filename = Regex.Replace(node.Name, @"\s+", "");

            if (!string.IsNullOrEmpty(node.LandingPageType.ToString()))
            {
                var topLevelTOCRelativeDir = Path.GetDirectoryName(_joinTOCConfig.TopLevelToc);
                var baseDir = string.IsNullOrEmpty(_joinTOCConfig.OutputFolder) ? topLevelTOCRelativeDir : _joinTOCConfig.OutputFolder;
                var servicePagePath = FilePath.Generated(new PathString($"./{baseDir}/{directoryName}/{filename}.yml"));

                var name = node.Name;
                var fullName = node.Name;
                var uid = node.Uid;
                var href = node.Href;

                var children = new List<ServicePageItem>();
                foreach (var item in node.Items)
                {
                    ServicePageItem child;
                    var childName = item.Value.Name.Value;
                    var childHref = item.Value.Href.Value;
                    var childUid = item.Value.Uid.Value;

                    if (!string.IsNullOrEmpty(childHref))
                    {
                        if (topLevelTOCRelativeDir != null)
                        {
                            var topLevelTOCYmlDir = Path.GetFullPath(Path.Combine(_docsetPath, topLevelTOCRelativeDir));
                            var hrefFileFullPath = Path.GetFullPath(Path.Combine(topLevelTOCYmlDir == null ? "" : topLevelTOCYmlDir, childHref));
                            var servicePageFullPath = Path.GetDirectoryName(Path.GetFullPath(Path.Combine(_docsetPath, servicePagePath.Path))) ?? _docsetPath;
                            var hrefRelativePath = Path.GetRelativePath(servicePageFullPath, hrefFileFullPath);
                            childHref = hrefRelativePath;
                        }

                        child = new ServicePageItem(childName, childHref, null);
                    }
                    else
                    {
                        child = new ServicePageItem(childName, null, childUid);
                    }

                    children.Add(child);
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
