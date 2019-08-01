// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;

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
        /// Gets the dependency git repository mappings
        /// </summary>
        public RestoreGitMap RestoreGitMap { get; }

        /// <summary>
        /// Gets the dependency file mappings
        /// </summary>
        public RestoreFileMap RestoreFileMap { get; }

        /// <summary>
        /// Gets the site base path
        /// </summary>
        public string SiteBasePath { get; }

        /// <summary>
        /// Gets the {Schema}://{HostName}
        /// </summary>
        public string HostName { get; }

        /// <summary>
        /// Gets the dependent docsets
        /// </summary>
        public IReadOnlyDictionary<string, Docset> DependencyDocsets => _dependencyDocsets.Value;

        private readonly CommandLineOptions _options;
        private readonly ErrorLog _errorLog;
        private readonly ConcurrentDictionary<string, Lazy<Repository>> _repositories;
        private readonly Lazy<IReadOnlyDictionary<string, Docset>> _dependencyDocsets;

        public Docset(
            ErrorLog errorLog,
            string docsetPath,
            string locale,
            Config config,
            CommandLineOptions options,
            RestoreGitMap restoreGitMap,
            Repository repository = null,
            Repository fallbackRepo = default)
            : this(
                  errorLog,
                  docsetPath,
                  !string.IsNullOrEmpty(locale) ? locale : config.Localization.DefaultLocale,
                  config,
                  options,
                  restoreGitMap,
                  repository,
                  fallbackDocset: null,
                  localizedDocset: null)
        {
            Debug.Assert(restoreGitMap != null);

            if (!string.Equals(Locale, Config.Localization.DefaultLocale, StringComparison.OrdinalIgnoreCase))
            {
                // localization/fallback docset will share the same context, config, build locale and options with source docset
                // source docset configuration will be overwritten by build locale overwrite configuration
                if (fallbackRepo != default)
                {
                    FallbackDocset = new Docset(_errorLog, fallbackRepo.Path, Locale, Config, _options, RestoreGitMap, fallbackRepo, localizedDocset: this);
                }
                else if (LocalizationUtility.TryGetLocalizedDocsetPath(this, restoreGitMap, Config, Locale, out var localizationDocsetPath, out var localizationBranch))
                {
                    var repo = Repository.Create(localizationDocsetPath, localizationBranch);
                    LocalizationDocset = new Docset(_errorLog, localizationDocsetPath, Locale, Config, _options, RestoreGitMap, repo, fallbackDocset: this);
                }
            }
        }

        private Docset(
            ErrorLog errorLog,
            string docsetPath,
            string locale,
            Config config,
            CommandLineOptions options,
            RestoreGitMap restoreGitMap,
            Repository repository = null,
            Docset fallbackDocset = null,
            Docset localizedDocset = null)
        {
            Debug.Assert(fallbackDocset is null || localizedDocset is null);

            _options = options;
            _errorLog = errorLog;
            Config = config;
            DocsetPath = PathUtility.NormalizeFolder(Path.GetFullPath(docsetPath));
            Locale = locale.ToLowerInvariant();
            Routes = NormalizeRoutes(config.Routes);
            Culture = CreateCultureInfo(locale);
            LocalizationDocset = localizedDocset;
            FallbackDocset = fallbackDocset;
            (HostName, SiteBasePath) = SplitBaseUrl(config.BaseUrl);

            Repository = repository ?? Repository.Create(DocsetPath, branch: null);

            RestoreGitMap = restoreGitMap;
            RestoreFileMap = new RestoreFileMap(this);

            _dependencyDocsets = new Lazy<IReadOnlyDictionary<string, Docset>>(() =>
            {
                var (errors, dependencies) = LoadDependencies(_errorLog, docsetPath, Config, Locale, RestoreGitMap, _options);
                _errorLog.Write(errors);
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
                throw Errors.LocaleInvalid(locale).ToException();
            }
        }

        private static (List<Error>, Dictionary<string, Docset>) LoadDependencies(
            ErrorLog errorLog, string docsetPath, Config config, string locale, RestoreGitMap restoreMap, CommandLineOptions options)
        {
            var errors = new List<Error>();
            var result = new Dictionary<string, Docset>(config.Dependencies.Count, PathUtility.PathComparer);
            foreach (var (name, url) in config.Dependencies)
            {
                var (remote, branch, _) = UrlUtility.SplitGitUrl(url);
                var (dir, subRestoreMap) = restoreMap.GetGitRestorePath(remote, branch, docsetPath);

                // get dependent docset config or default config
                // todo: what parent config should be pass on its children
                var (loadErrors, subConfig) = ConfigLoader.TryLoad(dir, options, locale);
                errors.AddRange(loadErrors);

                result.TryAdd(PathUtility.NormalizeFolder(name), new Docset(errorLog, dir, locale, subConfig, options, subRestoreMap, fallbackDocset: null));
            }
            return (errors, result);
        }

        private static (string hostName, string siteBasePath) SplitBaseUrl(string baseUrl)
        {
            string hostName = string.Empty;
            string siteBasePath = ".";
            if (!string.IsNullOrEmpty(baseUrl)
                && Uri.TryCreate(baseUrl, UriKind.Absolute, out var uriResult))
            {
                if (uriResult.AbsolutePath != "/")
                {
                    siteBasePath = uriResult.AbsolutePath.Substring(1);
                }
                hostName = $"{uriResult.Scheme}://{uriResult.Host}";
            }
            return (hostName, siteBasePath);
        }
    }
}
