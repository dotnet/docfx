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
        private readonly Func<PublishUrlMap> _publishUrlMap;

        private readonly IReadOnlyDictionary<FilePath, string> _redirectUrls;
        private readonly HashSet<PathString> _redirectPaths;
        private readonly Lazy<(IReadOnlyDictionary<FilePath, FilePath> renameHistory,
            IReadOnlyDictionary<FilePath, (FilePath, SourceInfo?)> redirectionHistory)> _history;

        public IEnumerable<FilePath> Files => _redirectUrls.Keys;

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
            _buildScope = buildScope;
            _documentProvider = documentProvider;
            _monikerProvider = monikerProvider;
            _config = config;

            using (Progress.Start("Loading redirections"))
            {
                var redirections = LoadRedirectionModel(errors, buildOptions.DocsetPath, buildOptions.Repository, config);
                _redirectUrls = GetRedirectUrls(redirections, _config.HostName);
                _redirectPaths = _redirectUrls.Keys.Select(x => x.Path).ToHashSet();
                _publishUrlMap = publishUrlMap;
                _history =
                   new Lazy<(IReadOnlyDictionary<FilePath, FilePath> renameHistory, IReadOnlyDictionary<FilePath, (FilePath, SourceInfo?)> redirectionHistory)>(
                        () => GetRenameAndRedirectionHistory(redirections, _redirectUrls));
            }
        }

        public bool TryGetValue(PathString file, [NotNullWhen(true)] out FilePath? actualPath)
        {
            if (_redirectPaths.TryGetValue(file, out var value))
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
            while (_history.Value.redirectionHistory.TryGetValue(redirectionFile, out var item))
            {
                var (renamedFrom, source) = item;
                if (redirectionChain.Contains(redirectionFile))
                {
                    redirectionChain.Push(redirectionFile);
                    errors.Add(Errors.Redirection.CircularRedirection(source, redirectionChain.Reverse()));
                    return _redirectUrls[file];
                }
                redirectionChain.Push(redirectionFile);
                redirectionFile = renamedFrom;
            }

            return _redirectUrls[file];
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

        private static RedirectionItem[] LoadRedirectionModel(ErrorBuilder errors, string docsetPath, Repository? repository, Config config)
        {
            var results = new List<RedirectionItem>();

            foreach (var fullPath in ProbeRedirectionFiles(docsetPath, repository))
            {
                if (File.Exists(fullPath))
                {
                    GenerateRedirectionRules(errors, docsetPath, fullPath, results);
                    break;
                }
            }

            foreach (var fullPath in ProbeSubRedirectionFiles(repository, config.RedirectionFiles))
            {
                if (File.Exists(fullPath))
                {
                    GenerateRedirectionRules(errors, docsetPath, fullPath, results);
                }
            }

            return results.OrderBy(item => item.RedirectUrl.Source).ToArray();
        }

        private static void GenerateRedirectionRules(ErrorBuilder errors, string docsetPath, string fullPath, List<RedirectionItem> results)
        {
            var content = File.ReadAllText(fullPath);
            var filePath = new FilePath(Path.GetRelativePath(docsetPath, fullPath));
            var model = fullPath.EndsWith(".yml")
                ? YamlUtility.Deserialize<RedirectionModel>(errors, content, filePath)
                : JsonUtility.Deserialize<RedirectionModel>(errors, content, filePath);

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
                if (item.SourcePath.IsDefault || string.IsNullOrEmpty(item.RedirectUrl))
                {
                    // Give a missing-attribute warning when source_path or redirect_url not specified
                    errors.Add(Errors.JsonSchema.MissingAttribute(item.RedirectUrl, "source_path or redirect_url"));
                    continue;
                }

                var sourcePath = Path.GetRelativePath(docsetPath, Path.Combine(basedir, item.SourcePath));

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

        private static IEnumerable<string> ProbeRedirectionFiles(string docsetPath, Repository? repository)
        {
            yield return Path.Combine(docsetPath, "redirections.yml");
            yield return Path.Combine(docsetPath, "redirections.json");

            if (repository != null)
            {
                yield return Path.Combine(repository.Path, ".openpublishing.redirection.json");
            }
        }

        private static IEnumerable<string> ProbeSubRedirectionFiles(Repository? repository, HashSet<string> redirectionFiles)
        {
            if (repository != null)
            {
                foreach (var item in redirectionFiles)
                {
                    if (item.Equals(".openpublishing.redirection.json", PathUtility.PathComparison))
                    {
                        continue;
                    }
                    yield return Path.Combine(repository.Path, item);
                }
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

        private static (string path, string query) RemoveTrailingIndex(string redirectionUrl)
        {
            var (path, query, _) = UrlUtility.SplitUrl(redirectionUrl);
            return (path.EndsWith("/index", PathUtility.PathComparison) ? path.Substring(0, path.Length - "index".Length) : path, query);
        }
    }
}
