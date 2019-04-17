// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;

namespace Microsoft.Docs.Build
{
    /// <summary>
    /// A docset is a collection of documents in the folder identified by `docfx.yml/docfx.json`.
    /// </summary>
    internal class Docset
    {
        /// <summary>
        /// Gets the absolute path to folder containing `docfx.yml/docfx.json`, it is not necessarily the path to git repository.
        /// </summary>
        public string DocsetPath { get; }

        /// <summary>
        /// Gets the config associated with this docset, loaded from `docfx.yml/docfx.json`.
        /// </summary>
        public Config Config { get; }

        /// <summary>
        /// Gets the culture computed from <see cref="Locale"/>/>.
        /// </summary>
        public CultureInfo Culture { get; }

        /// <summary>
        /// Gets the lower-case culture name computed from <see cref="CommandLineOptions.Locale" or <see cref="Config.DefaultLocale"/>/>
        /// </summary>
        public string Locale { get; }

        /// <summary>
        /// Gets a value indicating whether enable legacy output.
        /// </summary>
        public bool Legacy => _options.Legacy;

        /// <summary>
        /// Gets the resolve alias
        /// </summary>
        public IReadOnlyDictionary<string, string> ResolveAlias { get; }

        /// <summary>
        /// Gets the localization docset, it will be set when the current build locale is different with default locale
        /// </summary>
        public Docset LocalizationDocset { get; private set; }

        /// <summary>
        /// Gets the fallback docset, usually is English docset. It will be set when the current docset is localization docset.
        /// </summary>
        public Docset FallbackDocset { get; private set; }

        /// <summary>
        /// Gets the reversed <see cref="Config.Routes"/> for faster lookup.
        /// </summary>
        public IReadOnlyDictionary<string, string> Routes { get; }

        /// <summary>
        /// Gets the root repository of docset
        /// </summary>
        public Repository Repository { get; }

        /// <summary>
        /// Gets the dependency repos/files locked version
        /// </summary>
        public DependencyLockModel DependencyLock { get; }

        /// <summary>
        /// Gets the dependency repos/file mappings
        /// </summary>
        public RestoreMap RestoreMap { get; }

        /// <summary>
        /// Gets the dependent docsets
        /// </summary>
        public IReadOnlyDictionary<string, Docset> DependencyDocsets => _dependencyDocsets.Value;

        /// <summary>
        /// Gets the redirection map.
        /// </summary>
        public RedirectionMap Redirections => _redirections.Value;

        /// <summary>
        /// Gets the initial build scope.
        /// </summary>
        public HashSet<Document> BuildScope => _buildScope.Value;

        /// <summary>
        /// Gets the scan scope used to generate toc map, xref map, xxx map before build
        /// </summary>
        public HashSet<Document> ScanScope => _scanScope.Value;

        private readonly CommandLineOptions _options;
        private readonly Report _report;
        private readonly ConcurrentDictionary<string, Lazy<Repository>> _repositories;
        private readonly Lazy<HashSet<Document>> _buildScope;
        private readonly Lazy<HashSet<Document>> _scanScope;
        private readonly Lazy<RedirectionMap> _redirections;
        private readonly Lazy<IReadOnlyDictionary<string, Docset>> _dependencyDocsets;

        public Docset(
            Report report,
            string docsetPath,
            string locale,
            Config config,
            CommandLineOptions options,
            DependencyLockModel dependencyLock,
            RestoreMap restoreMap,
            Repository repository = null,
            Docset localizedDocset = null,
            Docset fallbackDocset = null,
            bool isDependency = false)
            : this(report, docsetPath, !string.IsNullOrEmpty(locale) ? locale : config.Localization.DefaultLocale, config, options, dependencyLock, restoreMap, repository, fallbackDocset, localizedDocset)
        {
            Debug.Assert(dependencyLock != null);
            Debug.Assert(restoreMap != null);

            if (!isDependency && !string.Equals(Locale, Config.Localization.DefaultLocale, StringComparison.OrdinalIgnoreCase))
            {
                // localization/fallback docset will share the same context, config, build locale and options with source docset
                // source docset configuration will be overwritten by build locale overwrite configuration
                if (LocalizationUtility.TryGetSourceDocsetPath(this, restoreMap, out var sourceDocsetPath, out var sourceBranch, out _))
                {
                    var repo = Repository.Create(sourceDocsetPath, sourceBranch);
                    FallbackDocset = new Docset(_report, sourceDocsetPath, Locale, Config, _options, DependencyLock, RestoreMap, repo, localizedDocset: this, isDependency: true);
                }
                else if (LocalizationUtility.TryGetLocalizedDocsetPath(this, restoreMap, Config, Locale, out var localizationDocsetPath, out var localizationBranch, out var localizationDependencyLock))
                {
                    var repo = Repository.Create(localizationDocsetPath, localizationBranch);
                    LocalizationDocset = new Docset(_report, localizationDocsetPath, Locale, Config, _options, DependencyLock, RestoreMap, repo, fallbackDocset: this, isDependency: true);
                }
            }
        }

        private Docset(
            Report report,
            string docsetPath,
            string locale,
            Config config,
            CommandLineOptions options,
            DependencyLockModel dependencyLock,
            RestoreMap restoreMap,
            Repository repository = null,
            Docset fallbackDocset = null,
            Docset localizedDocset = null)
        {
            Debug.Assert(fallbackDocset is null || localizedDocset is null);

            _options = options;
            _report = report;
            RestoreMap = restoreMap;
            Config = config;
            DocsetPath = PathUtility.NormalizeFolder(Path.GetFullPath(docsetPath));
            Locale = locale.ToLowerInvariant();
            Routes = NormalizeRoutes(config.Routes);
            Culture = CreateCultureInfo(locale);
            LocalizationDocset = localizedDocset;
            FallbackDocset = fallbackDocset;
            DependencyLock = dependencyLock;

            ResolveAlias = LoadResolveAlias(Config);
            Repository = repository ?? Repository.Create(DocsetPath, branch: null);
            var glob = GlobUtility.CreateGlobMatcher(Config.Files, Config.Exclude.Concat(Config.DefaultExclude).ToArray());

            // pass on the command line options to its children
            _buildScope = new Lazy<HashSet<Document>>(() => CreateBuildScope(Redirections.Files, glob));
            _redirections = new Lazy<RedirectionMap>(() =>
            {
                var (errors, map) = RedirectionMap.Create(this, glob);
                report.Write(Config.ConfigFileName, errors);
                return map;
            });
            _scanScope = new Lazy<HashSet<Document>>(() => GetScanScope(this));

            _dependencyDocsets = new Lazy<IReadOnlyDictionary<string, Docset>>(() =>
            {
                var (errors, dependencies) = LoadDependencies(_report, Config, Locale, DependencyLock, RestoreMap, _options);
                _report.Write(Config.ConfigFileName, errors);
                return dependencies;
            });

            _repositories = new ConcurrentDictionary<string, Lazy<Repository>>();
        }

        public Repository GetRepository(string filePath)
        {
            return GetRepositoryInternal(Path.Combine(DocsetPath, filePath));

            Repository GetRepositoryInternal(string fullPath)
            {
                if (GitUtility.IsRepo(fullPath))
                {
                    if (string.Equals(fullPath, DocsetPath.Substring(0, DocsetPath.Length - 1), PathUtility.PathComparison))
                    {
                        return Repository;
                    }

                    return Repository.Create(fullPath, branch: null);
                }

                var parent = Path.GetDirectoryName(fullPath);
                return !string.IsNullOrEmpty(parent)
                    ? _repositories.GetOrAdd(PathUtility.NormalizeFile(parent), k => new Lazy<Repository>(() => GetRepositoryInternal(k))).Value
                    : null;
            }
        }

        private static IReadOnlyDictionary<string, string> NormalizeRoutes(Dictionary<string, string> routes)
        {
            var result = new Dictionary<string, string>();
            foreach (var (key, value) in routes.Reverse())
            {
                result.Add(
                    key.EndsWith('/') || key.EndsWith('\\') ? PathUtility.NormalizeFolder(key) : PathUtility.NormalizeFile(key),
                    PathUtility.NormalizeFile(value));
            }
            return result;
        }

        private CultureInfo CreateCultureInfo(string locale)
        {
            try
            {
                return new CultureInfo(locale);
            }
            catch (CultureNotFoundException)
            {
                throw Errors.InvalidLocale(locale).ToException();
            }
        }

        private Dictionary<string, string> LoadResolveAlias(Config config)
        {
            var result = new Dictionary<string, string>(PathUtility.PathComparer);

            foreach (var (alias, aliasPath) in config.ResolveAlias)
            {
                result.TryAdd(PathUtility.NormalizeFolder(alias), PathUtility.NormalizeFolder(aliasPath));
            }

            return result.Reverse().ToDictionary(item => item.Key, item => item.Value);
        }

        private HashSet<Document> CreateBuildScope(IEnumerable<Document> redirections, Func<string, bool> glob)
        {
            using (Progress.Start("Globbing files"))
            {
                var files = new ConcurrentBag<Document>();

                ParallelUtility.ForEach(
                    Directory.EnumerateFiles(DocsetPath, "*.*", SearchOption.AllDirectories),
                    file =>
                    {
                        var relativePath = Path.GetRelativePath(DocsetPath, file);
                        if (glob(relativePath))
                        {
                            files.Add(Document.TryCreateFromFile(this, relativePath));
                        }
                    });

                return new HashSet<Document>(files.Concat(redirections));
            }
        }

        private static (List<Error>, Dictionary<string, Docset>) LoadDependencies(Report report, Config config, string locale, DependencyLockModel dependencyLock, RestoreMap restoreMap, CommandLineOptions options)
        {
            var errors = new List<Error>();
            var result = new Dictionary<string, Docset>(config.Dependencies.Count, PathUtility.PathComparer);
            foreach (var (name, url) in config.Dependencies)
            {
                var (dir, subLock) = restoreMap.GetGitRestorePath(url, dependencyLock);

                // get dependent docset config or default config
                // todo: what parent config should be pass on its children
                var (loadErrors, subConfig) = ConfigLoader.TryLoad(dir, options, locale);
                errors.AddRange(loadErrors);

                result.TryAdd(PathUtility.NormalizeFolder(name), new Docset(report, dir, locale, subConfig, options, subLock, restoreMap, isDependency: true));
            }
            return (errors, result);
        }

        private static HashSet<Document> GetScanScope(Docset docset)
        {
            var scanScopeFilePaths = new HashSet<string>(PathUtility.PathComparer);
            var scanScope = new HashSet<Document>();

            foreach (var buildScope in new[] { docset.LocalizationDocset?.BuildScope, docset.BuildScope, docset.FallbackDocset?.BuildScope })
            {
                if (buildScope is null)
                {
                    continue;
                }

                foreach (var document in buildScope)
                {
                    if (scanScopeFilePaths.Add(document.FilePath))
                    {
                        scanScope.Add(document);
                    }
                }
            }

            return scanScope;
        }
    }
}
