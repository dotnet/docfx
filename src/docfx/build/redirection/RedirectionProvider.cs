// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;

namespace Microsoft.Docs.Build
{
    internal class RedirectionProvider
    {
        private readonly Config _config;
        private readonly ErrorBuilder _errors;
        private readonly DocumentProvider _documentProvider;
        private readonly MonikerProvider _monikerProvider;
        private readonly BuildScope _buildScope;
        private readonly BuildOptions _buildOptions;
        private readonly Func<PublishUrlMap> _publishUrlMap;

        private readonly Watch<(Dictionary<FilePath, string> urls, HashSet<PathString> paths, RedirectionItem[] items)> _redirects;
        private readonly Watch<(Dictionary<FilePath, FilePath> rename, Dictionary<FilePath, (FilePath, SourceInfo?)> redirect)> _history;

        public IEnumerable<FilePath> Files => _redirects.Value.urls.Keys;

        public RedirectionProvider(
            Config config,
            BuildOptions buildOptions,
            ErrorBuilder errors,
            BuildScope buildScope,
            DocumentProvider documentProvider,
            MonikerProvider monikerProvider,
            Func<PublishUrlMap> publishUrlMap)
        {
            _errors = errors;
            _config = config;
            _buildScope = buildScope;
            _buildOptions = buildOptions;
            _documentProvider = documentProvider;
            _monikerProvider = monikerProvider;
            _publishUrlMap = publishUrlMap;

            _redirects = Watcher.Create(LoadRedirections);
            _history = Watcher.Create(LoadHistory);
        }

        public bool TryGetValue(PathString file, [NotNullWhen(true)] out FilePath? actualPath)
        {
            if (_redirects.Value.paths.TryGetValue(file, out var value))
            {
                actualPath = FilePath.Redirection(value, default);
                return true;
            }

            actualPath = default;
            return false;
        }

        public string GetRedirectUrl(ErrorBuilder errors, FilePath file)
        {
            var redirectionChain = new Stack<FilePath>();
            var redirectionFile = file;
            var redirectUrls = _redirects.Value.urls;
            while (_history.Value.redirect.TryGetValue(redirectionFile, out var item))
            {
                var (renamedFrom, source) = item;
                if (redirectionChain.Contains(redirectionFile))
                {
                    redirectionChain.Push(redirectionFile);
                    errors.Add(Errors.Redirection.CircularRedirection(source, redirectionChain.Reverse()));
                    return redirectUrls[file];
                }
                redirectionChain.Push(redirectionFile);
                redirectionFile = renamedFrom;
            }

            return redirectUrls[file];
        }

        public FilePath GetOriginalFile(FilePath file)
        {
            var renameChain = new HashSet<FilePath>();
            while (_history.Value.rename.TryGetValue(file, out var renamedFrom))
            {
                if (!renameChain.Add(file))
                {
                    return file;
                }
                file = renamedFrom;
            }
            return file;
        }

        private (Dictionary<FilePath, string>, HashSet<PathString>, RedirectionItem[]) LoadRedirections()
        {
            using (Progress.Start("Loading redirections"))
            {
                var redirections = LoadRedirectionModel();
                var redirectUrls = GetRedirectUrls(redirections, _config.HostName);
                var redirectPaths = redirectUrls.Keys.Select(x => x.Path).ToHashSet();

                return (redirectUrls, redirectPaths, redirections);
            }
        }

        private Dictionary<FilePath, string> GetRedirectUrls(RedirectionItem[] redirections, string hostName)
        {
            var redirectUrls = new Dictionary<FilePath, string>();

            foreach (var item in redirections)
            {
                var path = item.SourcePath;
                var redirectUrl = item.RedirectUrl;

                if (!_buildScope.Contains(path))
                {
                    continue;
                }

                var type = _buildScope.GetContentType(path);
                if (type != ContentType.Page)
                {
                    _errors.Add(Errors.Redirection.RedirectionInvalid(redirectUrl, path));
                    continue;
                }

                var absoluteRedirectUrl = redirectUrl.Value.Trim();
                var monikers = item.Monikers is null ? default : _monikerProvider.Validate(_errors, item.Monikers);
                var filePath = FilePath.Redirection(path, monikers);

                if (item.RedirectDocumentId)
                {
                    switch (UrlUtility.GetLinkType(absoluteRedirectUrl))
                    {
                        case LinkType.RelativePath:
                            var siteUrl = _documentProvider.GetSiteUrl(filePath);
                            absoluteRedirectUrl = PathUtility.Normalize(Path.Combine(Path.GetDirectoryName(siteUrl) ?? "", absoluteRedirectUrl));
                            break;
                        case LinkType.AbsolutePath:
                            break;
                        case LinkType.External:
                            absoluteRedirectUrl = UrlUtility.RemoveLeadingHostName(absoluteRedirectUrl, hostName, removeLocale: true);
                            break;
                        default:
                            _errors.Add(Errors.Redirection.RedirectUrlInvalid(path, redirectUrl));
                            break;
                    }
                }

                if (!redirectUrls.TryAdd(filePath, absoluteRedirectUrl))
                {
                    _errors.Add(Errors.Redirection.RedirectionConflict(redirectUrl, path));
                }
            }
            return redirectUrls;
        }

        private RedirectionItem[] LoadRedirectionModel()
        {
            var results = new List<RedirectionItem>();

            foreach (var fullPath in ProbeRedirectionFiles())
            {
                if (File.Exists(fullPath))
                {
                    GenerateRedirectionRules(fullPath, results);
                    break;
                }
            }

            foreach (var fullPath in ProbeSubRedirectionFiles())
            {
                if (File.Exists(fullPath))
                {
                    GenerateRedirectionRules(fullPath, results);
                }
            }

            return results.OrderBy(item => item.RedirectUrl.Source).ToArray();
        }

        private void GenerateRedirectionRules(string fullPath, List<RedirectionItem> results)
        {
            var content = File.ReadAllText(fullPath);
            var filePath = new FilePath(Path.GetRelativePath(_buildOptions.DocsetPath, fullPath));
            var model = fullPath.EndsWith(".yml")
                ? YamlUtility.Deserialize<RedirectionModel>(_errors, content, filePath)
                : JsonUtility.Deserialize<RedirectionModel>(_errors, content, filePath);

            // Expand redirect items array or object form
            var redirections = model.Redirections.arrayForm
                ?? model.Redirections.objectForm?.Select(
                        pair => new RedirectionItem { SourcePath = pair.Key, RedirectUrl = pair.Value })
                ?? Array.Empty<RedirectionItem>();

            var renames = model.Renames.Select(
                pair => new RedirectionItem
                {
                    SourcePath = pair.Key,
                    RedirectUrl = pair.Value,
                    RedirectDocumentId = true,
                });

            // Rebase source_path based on redirection definition file path
            var basedir = Path.GetDirectoryName(fullPath) ?? "";

            foreach (var item in redirections.Concat(renames))
            {
                if ((item.SourcePath.IsDefault && item.SourcePathFromRoot.IsDefault) || string.IsNullOrEmpty(item.RedirectUrl))
                {
                    _errors.Add(Errors.JsonSchema.MissingAttribute(item.RedirectUrl, "(source_path or source_path_from_root) or redirect_url"));
                    continue;
                }
                else if (!item.SourcePath.IsDefault && !item.SourcePathFromRoot.IsDefault)
                {
                    // TODO: "source_path" and "source_path_from_root" exist at the same time.
                    continue;
                }

                string sourcePath;

                if (!item.SourcePath.IsDefault)
                {
                    // TODO: syntax check
                    sourcePath = Path.GetRelativePath(_buildOptions.DocsetPath, Path.Combine(basedir, item.SourcePath));
                }
                else
                {
                    // TODO: syntax check
                    var sourcePathRelativeToRepoRoot = item.SourcePathFromRoot.Value.Substring(1);
                    if (_buildOptions.Repository != null)
                    {
                        sourcePath = Path.GetRelativePath(_buildOptions.DocsetPath, Path.Combine(_buildOptions.Repository.Path, sourcePathRelativeToRepoRoot));
                    }
                    else
                    {
                        sourcePath = Path.GetRelativePath(_buildOptions.DocsetPath, Path.Combine(_buildOptions.DocsetPath, sourcePathRelativeToRepoRoot));
                    }
                }

                if (!sourcePath.StartsWith("."))
                {
                    results.Add(new RedirectionItem
                    {
                        SourcePath = new PathString(sourcePath),
                        Monikers = item.Monikers,
                        RedirectUrl = item.RedirectUrl,
                        RedirectDocumentId = item.RedirectDocumentId,
                    });
                }
            }
        }

        private IEnumerable<string> ProbeRedirectionFiles()
        {
            yield return Path.Combine(_buildOptions.DocsetPath, "redirections.yml");
            yield return Path.Combine(_buildOptions.DocsetPath, "redirections.json");

            if (_buildOptions.Repository != null)
            {
                yield return Path.Combine(_buildOptions.Repository.Path, ".openpublishing.redirection.json");
            }
        }

        private IEnumerable<string> ProbeSubRedirectionFiles()
        {
            if (_buildOptions.Repository != null)
            {
                foreach (var item in _config.RedirectionFiles)
                {
                    if (item.Equals(".openpublishing.redirection.json", PathUtility.PathComparison))
                    {
                        continue;
                    }
                    yield return Path.Combine(_buildOptions.Repository.Path, item);
                }
            }
        }

        private (Dictionary<FilePath, FilePath>, Dictionary<FilePath, (FilePath, SourceInfo?)>) LoadHistory()
        {
            using (Progress.Start("Loading redirection history"))
            {
                // Convert the redirection target from redirect url to file path according to the version of redirect source
                var renameHistory = new Dictionary<FilePath, FilePath>();
                var redirectionHistory = new Dictionary<FilePath, (FilePath, SourceInfo?)>();
                var redirectUrls = _redirects.Value.urls;

                foreach (var item in _redirects.Value.items)
                {
                    var monikers = item.Monikers is null ? default : _monikerProvider.Validate(_errors, item.Monikers);
                    var file = FilePath.Redirection(item.SourcePath, monikers);
                    if (!redirectUrls.TryGetValue(file, out var value))
                    {
                        continue;
                    }

                    var (trimmedRedirectUrl, redirectQuery) = RemoveTrailingIndex(value);
                    var docs = _publishUrlMap().GetFilesByUrl(trimmedRedirectUrl.ToLowerInvariant());
                    if (!docs.Any())
                    {
                        if (item.RedirectDocumentId)
                        {
                            _errors.Add(Errors.Redirection.RedirectUrlInvalid(item.SourcePath, item.RedirectUrl));
                        }
                        continue;
                    }

                    var redirectionSourceMonikers = _monikerProvider.GetFileLevelMonikers(_errors, file);
                    var candidates = redirectionSourceMonikers.Count == 0
                                        ? docs.Where(doc => _monikerProvider.GetFileLevelMonikers(_errors, doc).Count == 0).ToList()
                                        : docs.Where(
                                            doc => _monikerProvider.GetFileLevelMonikers(_errors, doc).Intersect(redirectionSourceMonikers).Any()).ToList();

                    // skip circular redirection validation for url containing query string
                    if (candidates.Count > 0 && string.IsNullOrEmpty(redirectQuery))
                    {
                        redirectionHistory.TryAdd(file, (candidates.OrderBy(x => x).Last(), item.RedirectUrl.Source));
                    }

                    foreach (var candidate in candidates)
                    {
                        if (item.RedirectDocumentId && !renameHistory.TryAdd(candidate, file))
                        {
                            _errors.Add(Errors.Redirection.RedirectionUrlConflict(item.RedirectUrl));
                        }
                    }
                }
                return (renameHistory, redirectionHistory);
            }
        }

        private static (string path, string query) RemoveTrailingIndex(string redirectionUrl)
        {
            var (path, query, _) = UrlUtility.SplitUrl(redirectionUrl);
            return (path.EndsWith("/index", PathUtility.PathComparison) ? path.Substring(0, path.Length - "index".Length) : path, query);
        }
    }
}
