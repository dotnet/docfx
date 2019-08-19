// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Web;
using Microsoft.DocAsCode.MarkdigEngine.Extensions;

namespace Microsoft.Docs.Build
{
    internal class DependencyResolver
    {
        private readonly BuildScope _buildScope;
        private readonly WorkQueue<Document> _buildQueue;
        private readonly BookmarkValidator _bookmarkValidator;
        private readonly DependencyMapBuilder _dependencyMapBuilder;
        private readonly GitCommitProvider _gitCommitProvider;
        private readonly IReadOnlyDictionary<string, string> _resolveAlias;
        private readonly Lazy<XrefMap> _xrefMap;
        private readonly TemplateEngine _templateEngine;

        public DependencyResolver(
            Config config,
            BuildScope buildScope,
            WorkQueue<Document> buildQueue,
            GitCommitProvider gitCommitProvider,
            BookmarkValidator bookmarkValidator,
            DependencyMapBuilder dependencyMapBuilder,
            Lazy<XrefMap> xrefMap,
            TemplateEngine templateEngine)
        {
            _buildScope = buildScope;
            _buildQueue = buildQueue;
            _bookmarkValidator = bookmarkValidator;
            _dependencyMapBuilder = dependencyMapBuilder;
            _gitCommitProvider = gitCommitProvider;
            _xrefMap = xrefMap;
            _resolveAlias = LoadResolveAlias(config);
            _templateEngine = templateEngine;
        }

        public (Error error, string content, Document file) ResolveContent(SourceInfo<string> path, Document declaringFile, DependencyType dependencyType = DependencyType.Inclusion)
        {
            var (error, content, child) = TryResolveContent(declaringFile, path);

            _dependencyMapBuilder.AddDependencyItem(declaringFile, child, dependencyType);

            return (error, content, child);
        }

        public (Error error, string link, Document file) ResolveRelativeLink(Document relativeToFile, SourceInfo<string> path, Document declaringFile)
        {
            var (error, link, file) = ResolveAbsoluteLink(path, declaringFile);

            if (file != null)
            {
                link = UrlUtility.GetRelativeUrl(relativeToFile.SiteUrl, link);
            }

            return (error, link, file);
        }

        public (Error error, string link, Document file) ResolveAbsoluteLink(SourceInfo<string> path, Document declaringFile)
        {
            var (error, link, fragment, linkType, file, isCrossReference) = TryResolveAbsoluteLink(declaringFile, path);

            if (file != null)
            {
                _buildQueue.Enqueue(file);
            }

            // NOTE: bookmark validation result depend on current inclusion stack
            var relativeToFile = (Document)InclusionContext.RootFile ?? declaringFile;
            var isSelfBookmark = linkType == LinkType.SelfBookmark || relativeToFile == file;
            if (!isCrossReference && (isSelfBookmark || file != null))
            {
                _dependencyMapBuilder.AddDependencyItem(declaringFile, file, UrlUtility.FragmentToDependencyType(fragment));
                _bookmarkValidator.AddBookmarkReference(declaringFile, isSelfBookmark ? relativeToFile : file, fragment, isSelfBookmark, path);
            }

            return (error, link, file);
        }

        public (Error error, string href, string display, IXrefSpec spec) ResolveRelativeXref(Document relativeToFile, SourceInfo<string> href, Document declaringFile)
        {
            var (error, link, display, spec) = ResolveAbsoluteXref(href, declaringFile);

            if (spec?.DeclaringFile != null)
            {
                link = UrlUtility.GetRelativeUrl(relativeToFile.SiteUrl, link);
            }

            return (error, link, display, spec);
        }

        public (Error error, string href, string display, IXrefSpec spec) ResolveAbsoluteXref(SourceInfo<string> href, Document declaringFile)
        {
            var (uid, query, fragment) = UrlUtility.SplitUrl(href);
            string moniker = null;
            string text = null;
            var queries = new NameValueCollection();
            if (!string.IsNullOrEmpty(query))
            {
                queries = HttpUtility.ParseQueryString(query);
                moniker = queries["view"];
                queries.Remove("view");
                text = queries["text"];
                queries.Remove("text");
            }
            var displayProperty = queries["displayProperty"];
            queries.Remove("displayProperty");

            // need to url decode uid from input content
            var (error, resolvedHref, xrefSpec) = _xrefMap.Value.Resolve(Uri.UnescapeDataString(uid), href);

            var name = xrefSpec?.GetXrefPropertyValueAsString("name");

            // for internal UID, the display property can not be markdown or inline markdown
            // because the display text should be plain text
            string displayPropertyValue = null;
            if (xrefSpec is InternalXrefSpec internalSpec)
            {
                var contentType = internalSpec.GetXrefPropertyContentType(displayProperty);
                if (contentType != JsonSchemaContentType.Markdown && contentType != JsonSchemaContentType.InlineMarkdown)
                {
                    displayPropertyValue = xrefSpec?.GetXrefPropertyValueAsString(displayProperty);
                }
            }
            else
            {
                displayPropertyValue = xrefSpec?.GetXrefPropertyValueAsString(displayProperty);
            }

            // fallback order:
            // text -> xrefSpec.displayPropertyName -> xrefSpec.name -> uid
            var display = !string.IsNullOrEmpty(text)
                ? text
                : displayPropertyValue ?? name ?? uid;

            if (xrefSpec?.DeclaringFile != null)
            {
                _dependencyMapBuilder.AddDependencyItem(declaringFile, xrefSpec?.DeclaringFile, DependencyType.UidInclusion);
            }

            if (!string.IsNullOrEmpty(resolvedHref))
            {
                if (!string.IsNullOrEmpty(moniker))
                {
                    queries["view"] = moniker;
                }
                resolvedHref = UrlUtility.MergeUrl(
                    resolvedHref,
                    queries.AllKeys.Length == 0 ? "" : "?" + string.Join('&', queries),
                    fragment.Length == 0 ? "" : fragment.Substring(1));
            }

            return (error, resolvedHref, display, xrefSpec);
        }

        private (Error error, string content, Document file) TryResolveContent(Document declaringFile, SourceInfo<string> href)
        {
            var (error, file, _, _, _, pathToDocset) = TryResolveFile(declaringFile, href);

            if (file?.RedirectionUrl != null)
            {
                return default;
            }

            if (file is null)
            {
                var (content, fileFromHistory) = TryResolveContentFromHistory(declaringFile, _gitCommitProvider, pathToDocset, _templateEngine);
                if (fileFromHistory != null)
                {
                    return (null, content, fileFromHistory);
                }
            }

            return file != null ? (error, file.ReadText(), file) : default;
        }

        private (Error error, string href, string fragment, LinkType linkType, Document file, bool isCrossReference) TryResolveAbsoluteLink(Document declaringFile, SourceInfo<string> href)
        {
            Debug.Assert(href != null);

            if (href.Value.StartsWith("xref:"))
            {
                var uid = new SourceInfo<string>(href.Value.Substring("xref:".Length), href);
                var (uidError, uidHref, _, xrefSpec) = ResolveAbsoluteXref(uid, declaringFile);
                var xrefLinkType = xrefSpec?.DeclaringFile != null ? LinkType.RelativePath : LinkType.External;

                return (uidError, uidHref, null, xrefLinkType, xrefSpec?.DeclaringFile, true);
            }

            var decodedHref = new SourceInfo<string>(Uri.UnescapeDataString(href), href);
            var (error, file, query, fragment, linkType, pathToDocset) = TryResolveFile(declaringFile, decodedHref);

            if (linkType == LinkType.WindowsAbsolutePath)
            {
                return (error, "", fragment, linkType, null, false);
            }

            // Cannot resolve the file, leave href as is
            if (file is null)
            {
                file = TryResolveResourceFromHistory(declaringFile, _gitCommitProvider, pathToDocset, _templateEngine);
                if (file is null)
                {
                    return (error, href, fragment, linkType, null, false);
                }

                // set file to resource got from histroy, reset the error
                error = null;
            }

            // Self reference, don't build the file, leave href as is
            if (file == declaringFile)
            {
                if (linkType == LinkType.SelfBookmark)
                {
                    return (error, query + fragment, fragment, linkType, null, false);
                }

                var selfUrl = Document.PathToRelativeUrl(
                    Path.GetFileName(file.SitePath), file.ContentType, file.Mime, file.Docset.Config.Output.Json, file.IsPage);

                return (error, selfUrl + query + fragment, fragment, LinkType.SelfBookmark, null, false);
            }

            // Link to dependent repo, don't build the file, leave href as is
            if (declaringFile.Docset.DependencyDocsets.Values.Any(v => file.Docset == v))
            {
                return (Errors.LinkIsDependency(href, file), href, fragment, linkType, null, false);
            }

            if (file?.RedirectionUrl != null)
            {
                return (error, file.SiteUrl + query + fragment, null, linkType, file, false);
            }

            // Pages outside build scope, don't build the file, leave href as is
            if (error is null
                && (file.ContentType == ContentType.Page || file.ContentType == ContentType.TableOfContents)
                && !_buildScope.Files.Contains(file))
            {
                return (Errors.LinkOutOfScope(href, file), href, fragment, linkType, null, false);
            }

            return (error, file.SiteUrl + query + fragment, fragment, linkType, file, false);
        }

        private (Error error, Document file, string query, string fragment, LinkType linkType, string pathToDocset) TryResolveFile(Document declaringFile, SourceInfo<string> href)
        {
            href = href.Or("");
            var (path, query, fragment) = UrlUtility.SplitUrl(href);

            switch (UrlUtility.GetLinkType(href))
            {
                case LinkType.SelfBookmark:
                    return (null, declaringFile, query, fragment, LinkType.SelfBookmark, null);

                case LinkType.WindowsAbsolutePath:
                    return (Errors.LocalFilePath(href), null, null, null, LinkType.WindowsAbsolutePath, null);

                case LinkType.RelativePath:
                    if (string.IsNullOrEmpty(path))
                    {
                        // https://tools.ietf.org/html/rfc2396#section-4.2
                        // a hack way to process empty href
                        return (null, declaringFile, query, fragment, LinkType.SelfBookmark, null);
                    }

                    // Resolve path relative to docset
                    var pathToDocset = ResolveToDocsetRelativePath(path, declaringFile);

                    // Use the actual file name case
                    if (_buildScope.GetActualFileName(pathToDocset, out var pathActualCase))
                    {
                        pathToDocset = pathActualCase;
                    }

                    // resolve from redirection files
                    if (_buildScope.Redirections.TryGetRedirection(pathToDocset, out var redirectFile))
                    {
                        return (null, redirectFile, query, fragment, LinkType.RelativePath, pathToDocset);
                    }

                    var file = Document.CreateFromFile(declaringFile.Docset, pathToDocset, _templateEngine, _buildScope);

                    // for LandingPage should not be used, it is a hack to handle some specific logic for landing page based on the user input for now
                    // which needs to be removed once the user input is correct
                    if (_templateEngine != null && TemplateEngine.IsLandingData(declaringFile.Mime))
                    {
                        if (file is null)
                        {
                            // try to resolve with .md for landing page
                            pathToDocset = ResolveToDocsetRelativePath($"{path}.md", declaringFile);
                            file = Document.CreateFromFile(declaringFile.Docset, pathToDocset, _templateEngine, _buildScope);
                        }

                        // Do not report error for landing page
                        return (null, file, query, fragment, LinkType.RelativePath, pathToDocset);
                    }

                    if (file is null)
                    {
                        return (Errors.FileNotFound(new SourceInfo<string>(path, href)), null, query, fragment, LinkType.RelativePath, pathToDocset);
                    }

                    return (null, file, query, fragment, LinkType.RelativePath, pathToDocset);

                default:
                    return default;
            }
        }

        private string ResolveToDocsetRelativePath(string path, Document declaringFile)
        {
            var docsetRelativePath = PathUtility.NormalizeFile(Path.Combine(Path.GetDirectoryName(declaringFile.FilePath.Path), path));
            if (!File.Exists(Path.Combine(declaringFile.Docset.DocsetPath, docsetRelativePath)))
            {
                foreach (var (alias, aliasPath) in _resolveAlias)
                {
                    if (path.StartsWith(alias, PathUtility.PathComparison))
                    {
                        return PathUtility.NormalizeFile(aliasPath + path.Substring(alias.Length));
                    }
                }
            }
            return docsetRelativePath;
        }

        private Document TryResolveResourceFromHistory(Document declaringFile, GitCommitProvider gitCommitProvider, string pathToDocset, TemplateEngine templateEngine)
        {
            if (string.IsNullOrEmpty(pathToDocset))
            {
                return default;
            }

            // try to resolve from source repo's git history
            var fallbackDocset = _buildScope.GetFallbackDocset(declaringFile.Docset);
            if (fallbackDocset != null && Document.GetContentType(pathToDocset) == ContentType.Resource)
            {
                var (repo, _, commits) = gitCommitProvider.GetCommitHistory(fallbackDocset, pathToDocset);
                if (repo != null && commits.Count > 0)
                {
                    return Document.Create(fallbackDocset, pathToDocset, templateEngine, FileOrigin.Fallback, isFromHistory: true);
                }
            }

            return default;
        }

        private (string content, Document file) TryResolveContentFromHistory(Document declaringFile, GitCommitProvider gitCommitProvider, string pathToDocset, TemplateEngine templateEngine)
        {
            if (string.IsNullOrEmpty(pathToDocset))
            {
                return default;
            }

            // try to resolve from source repo's git history
            var fallbackDocset = _buildScope.GetFallbackDocset(declaringFile.Docset);
            if (fallbackDocset != null)
            {
                var (repo, pathToRepo, commits) = gitCommitProvider.GetCommitHistory(fallbackDocset, pathToDocset);
                if (repo != null)
                {
                    var repoPath = PathUtility.NormalizeFolder(repo.Path);
                    if (commits.Count > 1)
                    {
                        // the latest commit would be deleting it from repo
                        if (GitUtility.TryGetContentFromHistory(repoPath, pathToRepo, commits[1].Sha, out var content))
                        {
                            return (content, Document.Create(fallbackDocset, pathToDocset, templateEngine, FileOrigin.Fallback, isFromHistory: true));
                        }
                    }
                }
            }

            return default;
        }

        private static Dictionary<string, string> LoadResolveAlias(Config config)
        {
            var result = new Dictionary<string, string>(PathUtility.PathComparer);

            foreach (var (alias, aliasPath) in config.ResolveAlias)
            {
                result.TryAdd(PathUtility.NormalizeFolder(alias), PathUtility.NormalizeFolder(aliasPath));
            }

            return result.Reverse().ToDictionary(item => item.Key, item => item.Value);
        }
    }
}
