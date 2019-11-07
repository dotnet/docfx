// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
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
        private readonly IReadOnlyDictionary<FilePath, FilePath> _renameHistory;

        public IEnumerable<Document> Files => _redirectUrls.Keys.Select(_documentProvider.GetDocument);

        public RedirectionProvider(
            string docsetPath, ErrorLog errorLog, BuildScope buildScope, DocumentProvider documentProvider, MonikerProvider monikerProvider)
        {
            _errorLog = errorLog;
            _buildScope = buildScope;
            _documentProvider = documentProvider;
            _monikerProvider = monikerProvider;

            (_redirectUrls, _renameHistory) = LoadRedirections(docsetPath);
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

        private (Dictionary<FilePath, string> _redirectUrls, Dictionary<FilePath, FilePath> _renameHistory)
            LoadRedirections(string docsetPath)
        {
            var redirectUrls = new Dictionary<FilePath, string>();
            var renameHistory = new Dictionary<FilePath, FilePath>();

            foreach (var item in LoadRedirectionModel(docsetPath))
            {
                var path = item.SourcePath;
                var redirectUrl = item.RedirectUrl;

                if (string.IsNullOrEmpty(path) || string.IsNullOrEmpty(redirectUrl))
                {
                    _errorLog.Write(Errors.RedirectionIsNullOrEmpty(redirectUrl, path));
                    continue;
                }

                if (!_buildScope.Glob(path))
                {
                    continue;
                }

                var type = Document.GetContentType(path);
                if (type != ContentType.Page)
                {
                    _errorLog.Write(Errors.RedirectionInvalid(redirectUrl, path));
                    continue;
                }

                var mutableRedirectUrl = redirectUrl.Value.Trim();
                if (item.RedirectDocumentId)
                {
                    switch (UrlUtility.GetLinkType(redirectUrl))
                    {
                        case LinkType.RelativePath:
                        case LinkType.AbsolutePath:
                            break;
                        default:
                            _errorLog.Write(Errors.RedirectionUrlNotFound(path, redirectUrl));
                            break;
                    }
                }

                var filePath = new FilePath(path, FileOrigin.Redirection);

                if (!redirectUrls.TryAdd(filePath, redirectUrl))
                {
                    _errorLog.Write(Errors.RedirectionConflict(redirectUrl, path));
                }
                else if (item.RedirectDocumentId)
                {
                    renameHistory.Add(redirectUrl, redirect);
                }
            }

            return (redirectUrls, renameHistory);
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
                            SourcePath = PathUtility.NormalizeFile(sourcePath),
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

        private static string NormalizeRedirectUrl(string redirectionUrl)
        {
            var (url, _, _) = UrlUtility.SplitUrl(redirectionUrl);
            return url.EndsWith("/index", PathUtility.PathComparison) ? url.Substring(0, url.Length - "index".Length) : url;
        }

        private IReadOnlyDictionary<FilePath, Document> GetRenameHistory()
        {
            // Convert the redirection target from redirect url to file path according to the version of redirect source
            var renameHistory = new Dictionary<FilePath, Document>();

            var publishUrlMap = _buildScope.Files.Concat(redirections)
                .GroupBy(file => file.SiteUrl)
                .ToDictionary(group => group.Key, group => group.ToList(), PathUtility.PathComparer);

            foreach (var (originalRedirectUrl, redirect) in renames)
            {
                var (error, redirectionSourceMonikers) = _monikerProvider.GetFileLevelMonikers(redirect);
                if (error != null)
                {
                    _errorLog.Write(error);
                }
                var normalizedRedirectUrl = NormalizeRedirectUrl(redirect.RedirectionUrl);
                if (!publishUrlMap.TryGetValue(normalizedRedirectUrl, out var docs))
                {
                    _errorLog.Write(Errors.RedirectionUrlNotFound(redirect.FilePath.Path, originalRedirectUrl));
                }
                else
                {
                    List<Document> candidates;
                    if (redirectionSourceMonikers.Count == 0)
                    {
                        candidates = docs.Where(doc => _monikerProvider.GetFileLevelMonikers(doc).monikers.Count == 0).ToList();
                    }
                    else
                    {
                        candidates = docs.Where(doc => _monikerProvider.GetFileLevelMonikers(doc).monikers.Intersect(redirectionSourceMonikers).Any()).ToList();
                    }
                    foreach (var item in candidates)
                    {
                        if (!renameHistory.TryAdd(item.FilePath, redirect))
                        {
                            _errorLog.Write(Errors.RedirectionUrlConflict(originalRedirectUrl));
                        }
                    }
                }
            }
            return renameHistory;
        }
    }
}
