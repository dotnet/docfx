// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Docs.Build;

/// <summary>
/// Generate Service Page.
/// </summary>
internal class ServicePageGenerator
{
    private readonly PathString _docsetPath;
    private readonly Input _input;
    private readonly JoinTOCConfig _joinTOCConfig;
    private readonly BuildScope _buildScope;
    private readonly TocParser _tocParser;
    private readonly ErrorBuilder _errors;

    public ServicePageGenerator(
        PathString docsetPath,
        Input input,
        JoinTOCConfig joinTOCConfig,
        BuildScope buildScope,
        TocParser tocParser,
        ErrorBuilder errors)
    {
        _docsetPath = docsetPath;
        _input = input;
        _joinTOCConfig = joinTOCConfig;
        _buildScope = buildScope;
        _tocParser = tocParser;
        _errors = errors;
    }

    public void GenerateServicePageFromTopLevelTOC(TocNode node, List<FilePath> results, string directoryName = "")
    {
        var name = node.Name.Value;
        if (string.IsNullOrEmpty(name))
        {
            return;
        }

        var filename = name.Replace(" ", "");
        var referenceTOCRelativeDir = Path.GetDirectoryName(_joinTOCConfig.ReferenceToc) ?? ".";
        var referenceTOCFullPath = Path.GetFullPath(Path.Combine(_docsetPath, referenceTOCRelativeDir));

        if (string.IsNullOrEmpty(node.Href.Value))
        {
            TryToGenerateServicePageForItem(node, directoryName, filename, referenceTOCFullPath, results);
        }

        if (!string.IsNullOrEmpty(node.Href.Value))
        {
            node.Uid = node.Uid.With(null);
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

    private void TryToGenerateServicePageForItem(
        TocNode node,
        string directoryName,
        string filename,
        string referenceTOCFullPath,
        List<FilePath> results)
    {
        if (node.LandingPageType.Value != null)
        {
            var topLevelTOCRelativeDir = Path.GetDirectoryName(_joinTOCConfig.TopLevelToc);
            var referenceTOCRelativeDir = Path.GetDirectoryName(_joinTOCConfig.ReferenceToc);
            var baseDir = _joinTOCConfig.OutputFolder.IsDefault ? referenceTOCRelativeDir ?? topLevelTOCRelativeDir : _joinTOCConfig.OutputFolder;
            var pageType = node.LandingPageType.Value;
            var servicePagePath = pageType == LandingPageType.Root
                ? FilePath.Generated(new PathString($"./{baseDir}/{directoryName}/index.yml"))
                : FilePath.Generated(new PathString($"./{baseDir}/{directoryName}/{filename}.yml"));
            if (!_buildScope.Contains(servicePagePath.Path))
            {
                return;
            }

            if (string.IsNullOrEmpty(node.Href.Value))
            {
                var servicePageFullPath = Path.GetFullPath(Path.Combine(_docsetPath, servicePagePath.Path)) ?? _docsetPath;
                var referenceTocFullPath = Path.GetDirectoryName(Path.GetFullPath(Path.Combine(_docsetPath, _joinTOCConfig.ReferenceToc ?? ".")));
                var hrefRelativeToReferenceTOC = Path.GetRelativePath(referenceTocFullPath ?? ".", servicePageFullPath);
                node.Href = node.Href.With(hrefRelativeToReferenceTOC);
            }

            var name = node.Name;
            var fullName = node.Name;
            var children = new List<ServicePageItem>();
            foreach (var item in node.Items)
            {
                ServicePageItem child;
                var childName = item.Value.Name.Value;
                var childHref = item.Value.Href.Value;
                var childUid = item.Value.Uid.Value;
                var childHrefType = TocLoader.GetHrefType(childHref);

                if (item.Value.LandingPageType.Value != null)
                {
                    if (string.IsNullOrEmpty(childHref))
                    {
                        // generate href for it based on service-page path
                        childHref = pageType == LandingPageType.Root
                            ? $"./{childName?.Replace(" ", "")}.yml"
                            : $"{name.Value?.Replace(" ", "")}/{childName?.Replace(" ", "")}.yml";
                    }
                    else
                    {
                        childHref = childHrefType == TocHrefType.RelativeFolder || childHrefType == TocHrefType.TocFile
                            ? null
                            : GetHrefRelativeToServicePage(childHref, referenceTOCFullPath, servicePagePath);
                    }
                    child = new ServicePageItem(childName, childHref, null);
                }
                else
                {
                    if (!string.IsNullOrEmpty(childHref))
                    {
                        if (childHrefType == TocHrefType.RelativeFolder)
                        {
                            child = new ServicePageItem(childName, null, GetUidFromSplitTOC(childHref, referenceTOCFullPath));
                        }
                        else
                        {
                            childHref = GetHrefRelativeToServicePage(childHref, referenceTOCFullPath, servicePagePath);
                            child = new ServicePageItem(childName, childHref, null);
                        }
                    }
                    else
                    {
                        child = new ServicePageItem(childName, null, childUid ?? GetSubTocFirstUid(item));
                    }
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

            var servicePageToken = new ServicePageModel
            {
                Name = name,
                FullName = fullName,
                Children = children,
                Langs = langs,
                PageType = pageType,
            };

            _input.AddGeneratedContent(servicePagePath, JsonUtility.ToJObject(servicePageToken), "ReferenceContainer");
        }
    }

    private string? GetUidFromSplitTOC(string childHref, string referenceTOCFullPath)
    {
        childHref = Path.Combine(childHref, "toc.yml");
        var childHrefFullPath = Path.Combine(referenceTOCFullPath, childHref);
        var childHrefRelativeToDocset = Path.GetRelativePath(_docsetPath, childHrefFullPath);

        var filePath = _input.GetFirstMatchInSplitToc(childHrefRelativeToDocset);
        if (filePath != null)
        {
            var node = _tocParser.Parse(filePath!, _errors);
            return GetSubTocFirstUid(node);
        }

        return null;
    }

    private string? GetHrefRelativeToServicePage(string childHref, string referenceTOCFullPath, FilePath servicePagePath)
    {
        if (!(childHref.StartsWith("~/") || childHref.StartsWith("~\\")))
        {
            var hrefFileFullPath = Path.GetFullPath(Path.Combine(referenceTOCFullPath, childHref));
            var servicePageFullPath = Path.GetDirectoryName(Path.GetFullPath(Path.Combine(_docsetPath, servicePagePath.Path))) ?? _docsetPath;
            var hrefRelativePathToServicePage = Path.GetRelativePath(servicePageFullPath, hrefFileFullPath);
            childHref = hrefRelativePathToServicePage;
        }

        return childHref;
    }

    private string? GetSubTocFirstUid(TocNode node)
    {
        if (!string.IsNullOrEmpty(node.Uid))
        {
            return node.Uid.Value;
        }

        foreach (var item in node.Items)
        {
            if (!string.IsNullOrEmpty(item.Value.Uid))
            {
                return item.Value.Uid;
            }
            else
            {
                return GetSubTocFirstUid(item);
            }
        }

        return null;
    }
}
