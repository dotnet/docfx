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
        private readonly ErrorBuilder _errors;
        private readonly DocumentProvider _documentProvider;
        private readonly MonikerProvider _monikerProvider;
        private readonly BuildScope _buildScope;
        private readonly Lazy<PublishUrlMap> _publishUrlMap;

        private readonly IReadOnlyDictionary<FilePath, string> _redirectUrls;
        private readonly Lazy<(IReadOnlyDictionary<FilePath, FilePath> renameHistory,
            IReadOnlyDictionary<FilePath, (FilePath, SourceInfo?)> redirectionHistory)> _history;

        public IEnumerable<FilePath> Files => _redirectUrls.Keys;

        public RedirectionProvider(
            string docsetPath,
            string hostName,
            ErrorBuilder errors,
            BuildScope buildScope,
            Repository? repository,
            DocumentProvider documentProvider,
            MonikerProvider monikerProvider,
            Lazy<PublishUrlMap> publishUrlMap)
        {
            _errors = errors;
            _buildScope = buildScope;
            _documentProvider = documentProvider;
            _monikerProvider = monikerProvider;

            using (Progress.Start("Loading redirections"))
            {
                var (errors, redirections) = LoadRedirectionModel(docsetPath, repository);
                _errors.Write(errors);
                _redirectUrls = GetRedirectUrls(redirections, hostName);
                _history =
                   new Lazy<(IReadOnlyDictionary<FilePath, FilePath> renameHistory, IReadOnlyDictionary<FilePath, (FilePath, SourceInfo?)> redirectionHistory)>(
                        () => GetRenameAndRedirectionHistory(redirections, _redirectUrls));
                _publishUrlMap = publishUrlMap;
            }
        }

        public bool Contains(FilePath file)
        {
            return _redirectUrls.ContainsKey(file);
        }

        public (Error?, string) GetRedirectUrl(FilePath file)
        {
            var redirectionChain = new Stack<FilePath>();
            var redirectionFile = file;
            while (_history.Value.redirectionHistory.TryGetValue(redirectionFile, out var item))
            {
                var (renamedFrom, source) = item;
                if (redirectionChain.Contains(redirectionFile))
                {
                    redirectionChain.Push(redirectionFile);
                    return (Errors.Redirection.CircularRedirection(source, redirectionChain.Reverse()), _redirectUrls[file]);
                }
                redirectionChain.Push(redirectionFile);
                redirectionFile = renamedFrom;
            }

            return (null, _redirectUrls[file]);
        }

        public FilePath GetOriginalFile(FilePath file)
        {
            var renameChain = new HashSet<FilePath>();
            while (_history.Value.renameHistory.TryGetValue(file, out var renamedFrom))
            {
                if (!renameChain.Add(file))
                {
                    return file;
                }
                file = renamedFrom;
            }
            return file;
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
                    _errors.Write(Errors.Redirection.RedirectionIsNullOrEmpty(redirectUrl, path));
                    continue;
                }

                if (!_buildScope.Glob(path))
                {
                    continue;
                }

                var type = _buildScope.GetContentType(path);
                if (type != ContentType.Page)
                {
                    _errors.Write(Errors.Redirection.RedirectionInvalid(redirectUrl, path));
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
                            absoluteRedirectUrl = UrlUtility.RemoveLeadingHostName(absoluteRedirectUrl, hostName, removeLocale: true);
                            break;
                        default:
                            _errors.Write(Errors.Redirection.RedirectionUrlNotFound(path, redirectUrl));
                            break;
                    }
                }

                if (!redirectUrls.TryAdd(filePath, absoluteRedirectUrl))
                {
                    _errors.Write(Errors.Redirection.RedirectionConflict(redirectUrl, path));
                }
            }
            return redirectUrls;
        }

        private static (List<Error>, RedirectionItem[]) LoadRedirectionModel(string docsetPath, Repository? repository)
        {
            foreach (var fullPath in ProbeRedirectionFiles(docsetPath, repository))
            {
                if (File.Exists(fullPath))
                {
                    var content = File.ReadAllText(fullPath);
                    var filePath = new FilePath(Path.GetRelativePath(docsetPath, fullPath));
                    var (errors, model) = fullPath.EndsWith(".yml")
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

                    return (errors, (
                        from item in redirections.Concat(renames)
                        let sourcePath = Path.GetRelativePath(docsetPath, Path.Combine(basedir, item.SourcePath))
                        where !sourcePath.StartsWith(".")
                        select new RedirectionItem
                        {
                            SourcePath = new PathString(sourcePath),
                            RedirectUrl = item.RedirectUrl,
                            RedirectDocumentId = item.RedirectDocumentId,
                        }).OrderBy(item => item.RedirectUrl.Source).ToArray());
                }
            }

            return (new List<Error>(), Array.Empty<RedirectionItem>());
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

        private (IReadOnlyDictionary<FilePath, FilePath>, IReadOnlyDictionary<FilePath, (FilePath, SourceInfo?)>) GetRenameAndRedirectionHistory(
            RedirectionItem[] redirections, IReadOnlyDictionary<FilePath, string> redirectUrls)
        {
            // Convert the redirection target from redirect url to file path according to the version of redirect source
            var renameHistory = new Dictionary<FilePath, FilePath>();
            var redirectionHistory = new Dictionary<FilePath, (FilePath, SourceInfo?)>();

            foreach (var item in redirections)
            {
                var file = FilePath.Redirection(item.SourcePath);
                if (!redirectUrls.TryGetValue(file, out var redirectUrl))
                {
                    continue;
                }

                var (trimmedRedirectUrl, redirectQuery) = RemoveTrailingIndex(redirectUrl);
                var docs = _publishUrlMap.Value.GetFilesByUrl(trimmedRedirectUrl.ToLowerInvariant());
                if (!docs.Any())
                {
                    if (item.RedirectDocumentId)
                    {
                        _errors.Write(Errors.Redirection.RedirectionUrlNotFound(item.SourcePath, item.RedirectUrl));
                    }
                    continue;
                }

                var (errors, redirectionSourceMonikers) = _monikerProvider.GetFileLevelMonikers(file);
                _errors.Write(errors);

                var candidates = redirectionSourceMonikers.Count == 0
                                    ? docs.Where(doc => _monikerProvider.GetFileLevelMonikers(doc).monikers.Count == 0).ToList()
                                    : docs.Where(
                                        doc => _monikerProvider.GetFileLevelMonikers(doc).monikers.Intersect(redirectionSourceMonikers).Any()).ToList();

                // skip circular redirection validation for url containing query string
                if (candidates.Count > 0 && string.IsNullOrEmpty(redirectQuery))
                {
                    redirectionHistory.TryAdd(file, (candidates.OrderBy(x => x).Last(), item.RedirectUrl.Source));
                }

                foreach (var candidate in candidates)
                {
                    if (item.RedirectDocumentId && !renameHistory.TryAdd(candidate, file))
                    {
                        _errors.Write(Errors.Redirection.RedirectionUrlConflict(item.RedirectUrl));
                    }
                }
            }
            return (renameHistory, redirectionHistory);
        }

        private static (string path, string query) RemoveTrailingIndex(string redirectionUrl)
        {
            var (path, query, _) = UrlUtility.SplitUrl(redirectionUrl);
            return (path.EndsWith("/index", PathUtility.PathComparison) ? path.Substring(0, path.Length - "index".Length) : path, query);
        }
    }
}
