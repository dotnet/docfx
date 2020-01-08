// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.Docs.Build
{
    internal class RedirectionProvider
    {
        private readonly ErrorLog _errorLog;
        private readonly DocumentProvider _documentProvider;
        private readonly MonikerProvider _monikerProvider;
        private readonly BuildScope _buildScope;

        private readonly IReadOnlyDictionary<FilePath, string> _redirectUrls;
        private readonly IReadOnlyDictionary<FilePath, FilePath> _renameHistory;

        public IEnumerable<FilePath> Files => _redirectUrls.Keys;

        public RedirectionProvider(
            string docsetPath, string hostName, ErrorLog errorLog, BuildScope buildScope, DocumentProvider documentProvider, MonikerProvider monikerProvider)
        {
            _errorLog = errorLog;
            _buildScope = buildScope;
            _documentProvider = documentProvider;
            _monikerProvider = monikerProvider;

            var redirections = LoadRedirectionModel(docsetPath);
            _redirectUrls = GetRedirectUrls(redirections, hostName);
            _renameHistory = GetRenameHistory(redirections, _redirectUrls);
        }

        public bool Contains(FilePath file)
        {
            return _redirectUrls.ContainsKey(file);
        }

        public string GetRedirectUrl(FilePath file)
        {
            return _redirectUrls[file];
        }

        public FilePath GetOriginalFile(FilePath file)
        {
            while (_renameHistory.TryGetValue(file, out var renamedFrom))
            {
                file = renamedFrom;
            }
            return file;
        }

        private IReadOnlyDictionary<FilePath, string> GetRedirectUrls(RedirectionItem[] redirections, string hostName)
        {
            var redirectUrls = new ConcurrentDictionary<FilePath, string>();

            Parallel.ForEach(redirections, item =>
            {
                var path = item.SourcePath;
                var redirectUrl = item.RedirectUrl;

                if (string.IsNullOrEmpty(path) || string.IsNullOrEmpty(redirectUrl))
                {
                    _errorLog.Write(Errors.RedirectionIsNullOrEmpty(redirectUrl, path));
                    return;
                }

                if (!_buildScope.Glob(path))
                {
                    return;
                }

                var type = _documentProvider.GetContentType(path);
                if (type != ContentType.Page)
                {
                    _errorLog.Write(Errors.RedirectionInvalid(redirectUrl, path));
                    return;
                }

                var absoluteRedirectUrl = redirectUrl.Value.Trim();
                var filePath = new FilePath(path, FileOrigin.Redirection);

                if (item.RedirectDocumentId)
                {
                    switch (UrlUtility.GetLinkType(absoluteRedirectUrl))
                    {
                        case LinkType.RelativePath:
                            var siteUrl = _documentProvider.GetDocument(filePath).SiteUrl;
                            absoluteRedirectUrl = PathUtility.Normalize(Path.Combine(Path.GetDirectoryName(siteUrl), absoluteRedirectUrl));
                            break;
                        case LinkType.AbsolutePath:
                            break;
                        case LinkType.External:
                            absoluteRedirectUrl = RemoveLeadingHostNameLocale(absoluteRedirectUrl, hostName);
                            break;
                        default:
                            _errorLog.Write(Errors.RedirectionUrlNotFound(path, redirectUrl));
                            break;
                    }
                }

                if (!redirectUrls.TryAdd(filePath, absoluteRedirectUrl))
                {
                    _errorLog.Write(Errors.RedirectionConflict(redirectUrl, path));
                }
            });

            return redirectUrls;
        }

        private static RedirectionItem[] LoadRedirectionModel(string docsetPath)
        {
            foreach (var fullPath in ProbeRedirectionFiles(docsetPath))
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
                    var basedir = Path.GetDirectoryName(fullPath);

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

        private static IEnumerable<string> ProbeRedirectionFiles(string docsetPath)
        {
            yield return Path.Combine(docsetPath, "redirections.yml");
            yield return Path.Combine(docsetPath, "redirections.json");

            var directory = docsetPath;
            do
            {
                yield return Path.Combine(directory, ".openpublishing.redirection.json");
                directory = Path.GetDirectoryName(directory);
            }
            while (!string.IsNullOrEmpty(directory));
        }

        private IReadOnlyDictionary<FilePath, FilePath> GetRenameHistory(
            RedirectionItem[] redirections, IReadOnlyDictionary<FilePath, string> redirectUrls)
        {
            var publishUrlMap = new Lazy<Dictionary<string, List<(FilePath path, string siteUrl, IReadOnlyList<string> monikers)>>>(
                () => CreatePublishUrlMap(redirectUrls));

            var renameHistory = new ConcurrentDictionary<FilePath, FilePath>();

            // Convert the redirection target from redirect url to file path according to the version of redirect source
            Parallel.ForEach(redirections, item =>
            {
                if (!item.RedirectDocumentId)
                {
                    return;
                }

                var file = new FilePath(item.SourcePath, FileOrigin.Redirection);
                if (!redirectUrls.TryGetValue(file, out var redirectUrl))
                {
                    return;
                }

                redirectUrl = RemoveTrailingIndex(redirectUrl);
                if (!publishUrlMap.Value.TryGetValue(redirectUrl, out var docs))
                {
                    _errorLog.Write(Errors.RedirectionUrlNotFound(item.SourcePath, item.RedirectUrl));
                    return;
                }

                var (error, redirectionSourceMonikers) = _monikerProvider.GetFileLevelMonikers(file);
                if (error != null)
                {
                    _errorLog.Write(error);
                }

                var candidates = redirectionSourceMonikers.Count == 0
                    ? docs.Where(doc => doc.monikers.Count == 0)
                    : docs.Where(doc => doc.monikers.Intersect(redirectionSourceMonikers).Any());

                foreach (var (path, _, _) in candidates)
                {
                    if (!renameHistory.TryAdd(path, file))
                    {
                        _errorLog.Write(Errors.RedirectionUrlConflict(item.RedirectUrl));
                    }
                }
            });

            return renameHistory;
        }

        private Dictionary<string, List<(FilePath path, string siteUrl, IReadOnlyList<string> monikers)>> CreatePublishUrlMap(IReadOnlyDictionary<FilePath, string> redirectUrls)
        {
            var files = new ConcurrentBag<(FilePath path, string siteUrl, IReadOnlyList<string> monikers)>();

            Parallel.ForEach(_buildScope.Files.Concat(redirectUrls.Keys), file =>
            {
                var (error, monikers) = _monikerProvider.GetFileLevelMonikers(file);
                if (error != null)
                {
                    _errorLog.Write(error);
                }

                files.Add((file, _documentProvider.GetDocument(file).SiteUrl, monikers));
            });

            return files
                .GroupBy(file => file.siteUrl)
                .ToDictionary(group => group.Key, group => group.ToList(), PathUtility.PathComparer);
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
