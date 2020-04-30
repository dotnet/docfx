// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Microsoft.Docs.Build
{
    internal class RedirectionProvider
    {
        private readonly ErrorLog _errorLog;
        private readonly DocumentProvider _documentProvider;
        private readonly MonikerProvider _monikerProvider;
        private readonly BuildScope _buildScope;

        private readonly IReadOnlyDictionary<FilePath, string> _redirectUrls;
        private readonly IReadOnlyDictionary<FilePath, (FilePath, SourceInfo?, bool)> _renameHistory;

        public IEnumerable<FilePath> Files => _redirectUrls.Keys;

        public RedirectionProvider(
            string docsetPath, string hostName, ErrorLog errorLog, BuildScope buildScope, Repository? repository, DocumentProvider documentProvider, MonikerProvider monikerProvider)
        {
            _errorLog = errorLog;
            _buildScope = buildScope;
            _documentProvider = documentProvider;
            _monikerProvider = monikerProvider;

            using (Progress.Start("Loading redirections"))
            {
                var redirections = LoadRedirectionModel(docsetPath, repository);
                _redirectUrls = GetRedirectUrls(redirections, hostName);
                _renameHistory = GetRenameHistory(redirections, _redirectUrls);
            }
        }

        public bool Contains(FilePath file)
        {
            return _redirectUrls.ContainsKey(file);
        }

        public (Error?, string) GetRedirectUrl(FilePath file)
        {
            var redirectionChain =
                _renameHistory.GroupBy(kvp => kvp.Value.Item1).ToDictionary(
                    g => g.Key,
                    g =>
                    {
                        var chosen = g.OrderBy(x => x.Key).Last();
                        return (chosen.Key, chosen.Value.Item2);
                    });
            var (error, _) = GetOriginalFileCore(file, redirectionChain);
            return (error, _redirectUrls[file]);
        }

        public FilePath GetOriginalFile(FilePath file)
        {
            var renameHistory = _renameHistory.Where(kvp => kvp.Value.Item3).ToDictionary(kvp => kvp.Key, kvp => (kvp.Value.Item1, kvp.Value.Item2));
            var (_, originalFile) = GetOriginalFileCore(file, renameHistory);
            return originalFile;
        }

        private (Error?, FilePath) GetOriginalFileCore(FilePath file, Dictionary<FilePath, (FilePath, SourceInfo?)> chain)
        {
            var redirectionChain = new Stack<FilePath>();
            while (chain.TryGetValue(file, out var item))
            {
                var (renamedFrom, source) = item;
                if (redirectionChain.Contains(file))
                {
                    redirectionChain.Push(file);
                    return (Errors.Redirection.CircularRedirection(source, redirectionChain.Reverse()), file);
                }
                redirectionChain.Push(file);
                file = renamedFrom;
            }
            return (null, file);
        }

        private IReadOnlyDictionary<FilePath, string> GetRedirectUrls(RedirectionItem[] redirections, string hostName)
        {
            var redirectUrls = new Dictionary<FilePath, string>();

            foreach (var item in redirections)
            {
                var path = item.SourcePath;
                var redirectUrl = item.RedirectUrl;

                if (string.IsNullOrEmpty(path) || string.IsNullOrEmpty(redirectUrl))
                {
                    _errorLog.Write(Errors.Redirection.RedirectionIsNullOrEmpty(redirectUrl, path));
                    continue;
                }

                if (!_buildScope.Glob(path))
                {
                    continue;
                }

                var type = _buildScope.GetContentType(path);
                if (type != ContentType.Page)
                {
                    _errorLog.Write(Errors.Redirection.RedirectionInvalid(redirectUrl, path));
                    continue;
                }

                var absoluteRedirectUrl = redirectUrl.Value.Trim();
                var filePath = FilePath.Redirection(path);

                if (item.RedirectDocumentId)
                {
                    switch (UrlUtility.GetLinkType(absoluteRedirectUrl))
                    {
                        case LinkType.RelativePath:
                            var siteUrl = _documentProvider.GetDocument(filePath).SiteUrl;
                            absoluteRedirectUrl = PathUtility.Normalize(Path.Combine(Path.GetDirectoryName(siteUrl) ?? "", absoluteRedirectUrl));
                            break;
                        case LinkType.AbsolutePath:
                            break;
                        case LinkType.External:
                            absoluteRedirectUrl = RemoveLeadingHostNameLocale(absoluteRedirectUrl, hostName);
                            break;
                        default:
                            _errorLog.Write(Errors.Redirection.RedirectionUrlNotFound(path, redirectUrl));
                            break;
                    }
                }

                if (!redirectUrls.TryAdd(filePath, absoluteRedirectUrl))
                {
                    _errorLog.Write(Errors.Redirection.RedirectionConflict(redirectUrl, path));
                }
            }
            return redirectUrls;
        }

        private static RedirectionItem[] LoadRedirectionModel(string docsetPath, Repository? repository)
        {
            foreach (var fullPath in ProbeRedirectionFiles(docsetPath, repository))
            {
                if (File.Exists(fullPath))
                {
                    var content = File.ReadAllText(fullPath);
                    var filePath = new FilePath(Path.GetRelativePath(docsetPath, fullPath));
                    var model = fullPath.EndsWith(".yml")
                        ? YamlUtility.Deserialize<RedirectionModel>(content, filePath)
                        : JsonUtility.Deserialize<RedirectionModel>(content, filePath);

                    // Expand redirect items array or object form
                    var redirections = model.Redirections.arrayForm
                        ?? model.Redirections.objectForm?.Select(
                                pair => new RedirectionItem { SourcePath = pair.Key, RedirectUrl = pair.Value })
                        ?? Array.Empty<RedirectionItem>();

                    var renames = model.Renames.Select(
                        pair => new RedirectionItem { SourcePath = pair.Key, RedirectUrl = pair.Value, RedirectDocumentId = true });

                    // Rebase source_path based on redirection definition file path
                    var basedir = Path.GetDirectoryName(fullPath) ?? "";

                    return (
                        from item in redirections.Concat(renames)
                        let sourcePath = Path.GetRelativePath(docsetPath, Path.Combine(basedir, item.SourcePath))
                        where !sourcePath.StartsWith(".")
                        select new RedirectionItem
                        {
                            SourcePath = new PathString(sourcePath),
                            RedirectUrl = item.RedirectUrl,
                            RedirectDocumentId = item.RedirectDocumentId,
                        }).OrderBy(item => item.RedirectUrl.Source).ToArray();
                }
            }

            return Array.Empty<RedirectionItem>();
        }

        private static IEnumerable<string> ProbeRedirectionFiles(string docsetPath, Repository? repository)
        {
            yield return Path.Combine(docsetPath, "redirections.yml");
            yield return Path.Combine(docsetPath, "redirections.json");

            if (repository != null)
            {
                yield return Path.Combine(repository.Path, ".openpublishing.redirection.json");
            }
        }

        private IReadOnlyDictionary<FilePath, (FilePath, SourceInfo?, bool)> GetRenameHistory(
            RedirectionItem[] redirections, IReadOnlyDictionary<FilePath, string> redirectUrls)
        {
            // Convert the redirection target from redirect url to file path according to the version of redirect source
            var renameHistory = new Dictionary<FilePath, (FilePath, SourceInfo?, bool)>();
            var publishUrlMap = GetPublishUrlMap(redirectUrls.Keys);

            foreach (var item in redirections)
            {
                var file = FilePath.Redirection(item.SourcePath);
                if (!redirectUrls.TryGetValue(file, out var redirectUrl))
                {
                    continue;
                }

                redirectUrl = RemoveTrailingIndex(redirectUrl);
                if (!publishUrlMap.TryGetValue(redirectUrl, out var docs))
                {
                    if (item.RedirectDocumentId)
                    {
                        _errorLog.Write(Errors.Redirection.RedirectionUrlNotFound(item.SourcePath, item.RedirectUrl));
                    }
                    continue;
                }

                var (errors, redirectionSourceMonikers) = _monikerProvider.GetFileLevelMonikers(file);
                _errorLog.Write(errors);

                List<FilePath> candidates;
                if (redirectionSourceMonikers.Length == 0)
                {
                    candidates = docs.Where(doc => _monikerProvider.GetFileLevelMonikers(doc).monikers.Length == 0).ToList();
                }
                else
                {
                    candidates = docs.Where(doc => _monikerProvider.GetFileLevelMonikers(doc).monikers.Intersect(redirectionSourceMonikers).Any()).ToList();
                }

                foreach (var candidate in candidates)
                {
                    if (!renameHistory.TryAdd(candidate, (file, item.RedirectUrl.Source, item.RedirectDocumentId)))
                    {
                        if (item.RedirectDocumentId)
                        {
                            _errorLog.Write(Errors.Redirection.RedirectionUrlConflict(item.RedirectUrl));
                        }
                    }
                }
            }
            return renameHistory;
        }

        private Dictionary<string, List<FilePath>> GetPublishUrlMap(IEnumerable<FilePath> redirectUrlSources)
        {
            var fileUrls = new ConcurrentBag<(FilePath file, string url)>();
            var allSources = _buildScope.Files.Concat(redirectUrlSources);
            ParallelUtility.ForEach(_errorLog, allSources, file => fileUrls.Add((file, _documentProvider.GetDocsSiteUrl(file))));

            var publishUrlMap = fileUrls.GroupBy(fileUrl => fileUrl.url)
                                .ToDictionary(group => group.Key, group => group.Select(g => g.file).ToList(), PathUtility.PathComparer);
            return publishUrlMap;
        }

        private static string RemoveTrailingIndex(string redirectionUrl)
        {
            var (url, _, _) = UrlUtility.SplitUrl(redirectionUrl);
            return url.EndsWith("/index", PathUtility.PathComparison) ? url.Substring(0, url.Length - "index".Length) : url;
        }

        private static string RemoveLeadingHostNameLocale(string redirectionUrl, string hostName)
        {
            var uri = new Uri(redirectionUrl);
            var redirectPath = UrlUtility.MergeUrl(uri.PathAndQuery, "", uri.Fragment).TrimStart('/');
            if (!string.Equals(uri.Host, hostName, StringComparison.OrdinalIgnoreCase))
            {
                return redirectionUrl;
            }

            int slashIndex = redirectPath.IndexOf('/');
            if (slashIndex < 0)
            {
                return $"/{redirectPath}";
            }

            var firstSegment = redirectPath.Substring(0, slashIndex);
            return LocalizationUtility.IsValidLocale(firstSegment)
                ? $"{redirectPath.Substring(firstSegment.Length)}"
                : $"/{redirectPath}";
        }
    }
}
