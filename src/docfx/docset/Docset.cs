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
        /// Gets the dependent docsets
        /// </summary>
        public IReadOnlyDictionary<string, Docset> DependencyDocsets { get; }

        /// <summary>
        /// Gets the resolve alias
        /// </summary>
        public IReadOnlyDictionary<string, string> ResolveAlias { get; }

        /// <summary>
        /// Gets the localization docset, it will be set when the current build locale is different with default locale
        /// </summary>
        public Docset LocalizationDocset { get; }

        /// <summary>
        /// Gets the fallback docset, usually is English docset. It will be set when the current docset is localization docset.
        /// </summary>
        public Docset FallbackDocset { get; }

        /// <summary>
        /// Gets the reversed <see cref="Config.Routes"/> for faster lookup.
        /// </summary>
        public IReadOnlyDictionary<string, string> Routes { get; }

        /// <summary>
        /// Gets the root repository of docset
        /// </summary>
        public Repository Repository { get; }

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

        public LegacyTemplate LegacyTemplate => _legacyTemplate.Value;

        private readonly CommandLineOptions _options;
        private readonly Report _report;
        private readonly ConcurrentDictionary<string, Lazy<Repository>> _repositories;
        private readonly Lazy<HashSet<Document>> _buildScope;
        private readonly Lazy<HashSet<Document>> _scanScope;
        private readonly Lazy<RedirectionMap> _redirections;
        private readonly Lazy<LegacyTemplate> _legacyTemplate;

        public Docset(Report report, string docsetPath, string locale, Config config, CommandLineOptions options, Repository repository = null, bool isDependency = false)
            : this(report, docsetPath, !string.IsNullOrEmpty(locale) ? locale : config.Localization.DefaultLocale, config, options, repository, null, null)
        {
            if (!isDependency && !string.Equals(Locale, config.Localization.DefaultLocale, StringComparison.OrdinalIgnoreCase))
            {
                // localization/fallback docset will share the same context, config, build locale and options with source docset
                // source docset configuration will be overwritten by build locale overwrite configuration
                if (LocalizationUtility.TryGetSourceDocsetPath(this, out var sourceDocsetPath, out var sourceBranch))
                {
                    var repo = Repository.Create(sourceDocsetPath, sourceBranch);
                    FallbackDocset = new Docset(report, sourceDocsetPath, Locale, config, options, repo, localizedDocset: this);
                }
                else if (LocalizationUtility.TryGetLocalizedDocsetPath(this, Config, Locale, out var localizationDocsetPath, out var localizationBranch))
                {
                    var repo = Repository.Create(localizationDocsetPath, localizationBranch);
                    LocalizationDocset = new Docset(report, localizationDocsetPath, Locale, config, options, repo, fallbackDocset: this);
                }
            }
        }

        private Docset(Report report, string docsetPath, string locale, Config config, CommandLineOptions options, Repository repository = null, Docset fallbackDocset = null, Docset localizedDocset = null)
        {
            Debug.Assert(fallbackDocset == null || localizedDocset == null);

            _options = options;
            _report = report;
            Config = config;
            DocsetPath = PathUtility.NormalizeFolder(Path.GetFullPath(docsetPath));
            Locale = locale.ToLowerInvariant();
            Routes = NormalizeRoutes(config.Routes);
            Culture = CreateCultureInfo(locale);
            FallbackDocset = fallbackDocset;
            LocalizationDocset = localizedDocset;

            var configErrors = new List<Error>();
            (configErrors, DependencyDocsets) = LoadDependencies(Config);
            ResolveAlias = LoadResolveAlias(Config);
            Repository = repository ?? Repository.Create(DocsetPath, branch: null);

            // pass on the command line options to its children
            _buildScope = new Lazy<HashSet<Document>>(() => CreateBuildScope(Redirections.Files));
            _redirections = new Lazy<RedirectionMap>(() =>
            {
                var (errors, map) = RedirectionMap.Create(this);
                errors.AddRange(configErrors);
                report.Write(Config.ConfigFileName, errors);
                return map;
            });
            _scanScope = new Lazy<HashSet<Document>>(() => this.GetScanScope());

            _legacyTemplate = new Lazy<LegacyTemplate>(() =>
            {
                Debug.Assert(!string.IsNullOrEmpty(Config.Theme));
                var (themeRemote, themeBranch) = LocalizationUtility.GetLocalizedTheme(Config.Theme, Locale, Config.Localization.DefaultLocale);
                return new LegacyTemplate(RestoreMap.GetGitRestorePath($"{themeRemote}#{themeBranch}"), Locale);
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

        private (List<Error>, Dictionary<string, Docset>) LoadDependencies(Config config)
        {
            var errors = new List<Error>();
            var result = new Dictionary<string, Docset>(config.Dependencies.Count, PathUtility.PathComparer);
            foreach (var (name, url) in config.Dependencies)
            {
                var dir = RestoreMap.GetGitRestorePath(url);

                // get dependent docset config or default config
                // todo: what parent config should be pass on its children
                var (loadErrors, subConfig) = ConfigLoader.TryLoad(dir, _options, Locale);
                errors.AddRange(loadErrors);
                result.TryAdd(PathUtility.NormalizeFolder(name), new Docset(_report, dir, Locale, subConfig, _options, isDependency: true));
            }
            return (errors, result);
        }

        private HashSet<Document> CreateBuildScope(IEnumerable<Document> redirections)
        {
            using (Progress.Start("Globbing files"))
            {
                var glob = GlobUtility.CreateGlobMatcher(Config.Files, Config.Exclude.Concat(Config.DefaultExclude).ToArray());
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

                var result = new HashSet<Document>(files);

                foreach (var redirection in redirections)
                {
                    if (glob(redirection.FilePath))
                    {
                        result.Add(redirection);
                    }
                    else
                    {
                        _report.Write(Errors.RedirectionOutOfScope(redirection, Config.ConfigFileName));
                    }
                }

                return result;
            }
        }
    }
}
