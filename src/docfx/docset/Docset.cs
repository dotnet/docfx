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
    /// A docset is a collection of documents in the folder identified by `docfx.yml`.
    /// </summary>
    internal class Docset
    {
        /// <summary>
        /// Gets the absolute path to folder containing `docfx.yml`, it is not necessarily the path to git repository.
        /// </summary>
        public string DocsetPath { get; }

        /// <summary>
        /// Gets the config associated with this docset, loaded from `docfx.yml`.
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

        public LegacyTemplate LegacyTemplate => _legacyTemplate.Value;

        private readonly CommandLineOptions _options;
        private readonly Context _context;
        private readonly Lazy<HashSet<Document>> _buildScope;
        private readonly Lazy<RedirectionMap> _redirections;
        private readonly Lazy<LegacyTemplate> _legacyTemplate;

        public Docset(Context context, string docsetPath, Config config, CommandLineOptions options)
        {
            _options = options;
            _context = context;
            Config = config;

            DocsetPath = PathUtility.NormalizeFolder(Path.GetFullPath(docsetPath));

            Locale = !string.IsNullOrEmpty(options.Locale) ? options.Locale.ToLowerInvariant() : Config.DefaultLocale;
            Culture = CreateCultureInfo(Locale);
            RestoreMap = new RestoreMap(DocsetPath);
            var configErrors = new List<Error>();
            (configErrors, DependentDocset) = LoadDependencies(Config, RestoreMap);

            // pass on the command line options to its children
            _buildScope = new Lazy<HashSet<Document>>(() => CreateBuildScope(Redirections.Files));
            _redirections = new Lazy<RedirectionMap>(() =>
            {
                var (errors, map) = RedirectionMap.Create(this);
                errors.AddRange(configErrors);
                context.Report("docfx.yml", errors);
                return map;
            });

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
                        _context.Report(Errors.RedirectionOutOfScope(redirection));
                    }
                }

                return files;
            }
        }
    }
}
