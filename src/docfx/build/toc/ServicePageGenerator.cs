// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

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
            if (string.IsNullOrEmpty(node.Name))
            {
                return;
            }

            var filename = Regex.Replace(node.Name, @"\s+", "");

            if (node.LandingPageType.Value != null)
            {
                var topLevelTOCRelativeDir = Path.GetDirectoryName(_joinTOCConfig.TopLevelToc);
                var baseDir = _joinTOCConfig.OutputFolder.IsDefault ? topLevelTOCRelativeDir : _joinTOCConfig.OutputFolder;

                var referenceTOCRelativeDir = Path.GetDirectoryName(_joinTOCConfig.ReferenceToc) ?? ".";
                var referenceTOCFullPath = Path.GetFullPath(Path.Combine(_docsetPath, referenceTOCRelativeDir));

                var pageType = node.LandingPageType.Value;
                FilePath servicePagePath;
                if (pageType == LandingPageType.Root)
                {
                    servicePagePath = FilePath.Generated(new PathString($"./{baseDir}/{directoryName}/index.yml"));
                }
                else
                {
                    servicePagePath = FilePath.Generated(new PathString($"./{baseDir}/{directoryName}/{filename}.yml"));
                }

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

                    var childHrefType = TableOfContentsLoader.GetHrefType(childHref);

                    if (!string.IsNullOrEmpty(childHref) && (childHrefType == TocHrefType.RelativeFolder || childHrefType == TocHrefType.TocFile))
                    {
                        childHref = null;
                    }

                    if (!string.IsNullOrEmpty(childHref))
                    {
                        if (!(childHref.StartsWith("~/") || childHref.StartsWith("~\\")))
                        {
                            var hrefFileFullPath = Path.GetFullPath(Path.Combine(referenceTOCFullPath, childHref));
                            var servicePageFullPath = Path.GetDirectoryName(Path.GetFullPath(Path.Combine(_docsetPath, servicePagePath.Path))) ?? _docsetPath;
                            var hrefRelativePathToReferenceTOC = Path.GetRelativePath(servicePageFullPath, hrefFileFullPath);
                            childHref = hrefRelativePathToReferenceTOC;
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
                    _joinTOCConfig.ContainerPageMetadata.TryGetValue("langs", out var lang);
                    langs = lang?.ToObject<List<string?>>();
                }

                results.Add(servicePagePath);
                var servicePageToken = new ServicePageModel(name, fullName, href, uid, children, langs, pageType);
                _input.AddGeneratedContent(servicePagePath, JsonUtility.ToJObject(servicePageToken), "ReferenceContainer");

                // Add Overview page
                if (node.Items.Count > 0)
                {
                    // add toc item of the overview page
                    var overviewTocItem = new TableOfContentsNode(node);
                    overviewTocItem.Name = overviewTocItem.Name.With("Overview");
                    node.Items.Insert(0, new SourceInfo<TableOfContentsNode>(overviewTocItem));

                    // need to calculate the href to the overview page

                    // add overview service page
                }
            }

            foreach (var item in node.Items)
            {
                if (node.LandingPageType == LandingPageType.Root)
                {
                    GenerateServicePageFromTopLevelTOC(item, results, $"{directoryName}");
                }
                else
                {
                    GenerateServicePageFromTopLevelTOC(item, results, $"{directoryName}/{filename}");
                }
            }
        }
    }
}
