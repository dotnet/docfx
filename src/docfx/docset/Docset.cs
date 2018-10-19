// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
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
        public IReadOnlyDictionary<string, Docset> DependentDocset { get; }

        /// <summary>
        /// Gets the localization docset, it will be set when the current build locale is different with default locale
        /// </summary>
        public Docset LocalizationDocset { get; }

        /// <summary>
        /// Gets the fallback docset, usually is English docset. It will be set when the current docset is localization docset.
        /// </summary>
        public Docset FallbackDocset { get; }

        /// <summary>
        /// Gets the restore path mappings
        /// </summary>
        public RestoreMap RestoreMap { get; }

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
        private readonly Context _context;
        private readonly Lazy<HashSet<Document>> _buildScope;
        private readonly Lazy<HashSet<Document>> _scanScope;
        private readonly Lazy<RedirectionMap> _redirections;
        private readonly Lazy<LegacyTemplate> _legacyTemplate;

        public Docset(Context context, string docsetPath, Config config, CommandLineOptions options)
            : this(context, docsetPath, config, !string.IsNullOrEmpty(options.Locale) ? options.Locale.ToLowerInvariant() : config.DefaultLocale, options, null)
        {
            if (!string.Equals(Locale, config.DefaultLocale, StringComparison.OrdinalIgnoreCase))
            {
                var localizationDocsetPath = LocalizationConvention.GetLocalizationDocsetPath(DocsetPath, Config, Locale, RestoreMap);

                // localization docset will share the same context, config, build locale and options with source docset
                // source docset configuration will be overwrote by build locale overwrite configuration
                LocalizationDocset = string.IsNullOrEmpty(localizationDocsetPath) ? null : new Docset(context, localizationDocsetPath, config, Locale, options, this);
            }
        }

        private Docset(Context context, string docsetPath, Config config, string locale, CommandLineOptions options, Docset fallbackDocset)
        {
            _options = options;
            _context = context;
            Config = config;
            DocsetPath = PathUtility.NormalizeFolder(Path.GetFullPath(docsetPath));
            Locale = locale;
            Culture = CreateCultureInfo(locale);
            RestoreMap = new RestoreMap(DocsetPath);
            FallbackDocset = fallbackDocset;

            var configErrors = new List<Error>();
            (configErrors, DependentDocset) = LoadDependencies(Config, RestoreMap);

            // pass on the command line options to its children
            _buildScope = new Lazy<HashSet<Document>>(() => CreateBuildScope(Redirections.Files));
            _redirections = new Lazy<RedirectionMap>(() =>
            {
                var (errors, map) = RedirectionMap.Create(this);
                errors.AddRange(configErrors);
                context.Report(Config.ConfigFileName, errors);
                return map;
            });
            _scanScope = new Lazy<HashSet<Document>>(() => CreateScanScope());

            _legacyTemplate = new Lazy<LegacyTemplate>(() => new LegacyTemplate(RestoreMap.GetGitRestorePath(Config.Dependencies["_themes"])));
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

        private (List<Error>, Dictionary<string, Docset>) LoadDependencies(Config config, RestoreMap restoreMap)
        {
            var errors = new List<Error>();
            var result = new Dictionary<string, Docset>(config.Dependencies.Count, PathUtility.PathComparer);
            foreach (var (name, url) in config.Dependencies)
            {
                var dir = restoreMap.GetGitRestorePath(url);

                // get dependent docset config or default config
                // todo: what parent config should be pass on its children
                Config.LoadIfExists(dir, _options, out var loadErrors, out var subConfig);
                errors.AddRange(loadErrors);
                result.TryAdd(PathUtility.NormalizeFolder(name), new Docset(_context, dir, subConfig, _options));
            }
            return (errors, result);
        }

        private HashSet<Document> CreateBuildScope(IEnumerable<Document> redirections)
        {
            using (Progress.Start("Globbing files"))
            {
                var fileGlob = new FileGlob(Config.Content.Include, Config.Content.Exclude);
                var files = fileGlob.GetFiles(DocsetPath).Select(file => Document.TryCreateFromFile(this, Path.GetRelativePath(DocsetPath, file))).ToHashSet();

                foreach (var redirection in redirections)
                {
                    if (fileGlob.IsMatch(redirection.FilePath))
                    {
                        files.Add(redirection);
                    }
                    else
                    {
                        _context.Report(Errors.RedirectionOutOfScope(redirection, Config.ConfigFileName));
                    }
                }

                return files;
            }
        }

        private HashSet<Document> CreateScanScope()
        {
            var scanScopeFilePaths = new HashSet<string>(PathUtility.PathComparer);
            var scanScope = new HashSet<Document>();

            foreach (var buildScope in new[] { LocalizationDocset?.BuildScope, BuildScope, FallbackDocset?.BuildScope })
            {
                if (buildScope == null)
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
