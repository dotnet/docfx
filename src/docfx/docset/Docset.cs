// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace Microsoft.Docs.Build
{
    /// <summary>
    /// A docset is a collection of documents in the folder identified by `docfx.yml/docfx.json`.
    /// </summary>
    internal class Docset : IEquatable<Docset>, IComparable<Docset>
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
            Repository repository = null)
        {
            _options = options;
            _errorLog = errorLog;
            Config = config;
            DocsetPath = PathUtility.NormalizeFolder(Path.GetFullPath(docsetPath));
            Locale = !string.IsNullOrEmpty(locale) ? locale.ToLowerInvariant() : config.Localization.DefaultLocale;
            Routes = NormalizeRoutes(config.Routes);
            Culture = CreateCultureInfo(locale);
            (HostName, SiteBasePath) = SplitBaseUrl(config.BaseUrl);

            Repository = repository ?? Repository.Create(DocsetPath, branch: null);

            _dependencyDocsets = new Lazy<IReadOnlyDictionary<string, Docset>>(() =>
            {
                var (errors, dependencies) = LoadDependencies(_errorLog, docsetPath, Config, Locale, restoreGitMap, _options);
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

                result.TryAdd(PathUtility.NormalizeFolder(name), new Docset(errorLog, dir, locale, subConfig, options, subRestoreMap));
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

        public int CompareTo(Docset other)
        {
            return PathUtility.PathComparer.Compare(DocsetPath, other.DocsetPath);
        }

        public override int GetHashCode()
        {
            return PathUtility.PathComparer.GetHashCode(DocsetPath);
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as Docset);
        }

        public bool Equals(Docset other)
        {
            if (other == null)
            {
                return false;
            }

            return PathUtility.PathComparer.Equals(DocsetPath, other.DocsetPath);
        }

        public static bool operator ==(Docset obj1, Docset obj2)
        {
            return Equals(obj1, obj2);
        }

        public static bool operator !=(Docset obj1, Docset obj2)
        {
            return !Equals(obj1, obj2);
        }
    }
}
