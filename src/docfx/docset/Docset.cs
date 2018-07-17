// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
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
        /// Gets the dependent docsets
        /// </summary>
        public IReadOnlyDictionary<string, Docset> DependentDocset { get; }

        /// <summary>
        /// Gets the restore path mappings
        /// </summary>
        public RestoreMap RestoreMap { get; }

        /// <summary>
        /// Gets the git repository this docset belongs to. Null if there is no git repo.
        /// Multiple repositories inside a single docset is not supported.
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

        private readonly CommandLineOptions _options;
        private readonly Context _context;
        private readonly Lazy<HashSet<Document>> _buildScope;
        private readonly Lazy<RedirectionMap> _redirections;

        public Docset(Context context, string docsetPath, Config config, CommandLineOptions options)
        {
            DocsetPath = Path.GetFullPath(docsetPath);
            Repository = Repository.Create(DocsetPath, options);
            Config = config;
            RestoreMap = new RestoreMap(DocsetPath);
            DependentDocset = LoadDependencies(Config, RestoreMap);

            // pass on the command line options to its children
            _options = options;
            _context = context;
            _buildScope = new Lazy<HashSet<Document>>(() => CreateBuildScope(Redirections.Files));
            _redirections = new Lazy<RedirectionMap>(() =>
            {
                var (errors, map) = RedirectionMap.Create(this);
                context.Report("docfx.yml", errors);
                return map;
            });
        }

        private Dictionary<string, Docset> LoadDependencies(Config config, RestoreMap restoreMap)
        {
            var result = new Dictionary<string, Docset>(config.Dependencies.Count, PathUtility.PathComparer);
            foreach (var (name, url) in config.Dependencies)
            {
                if (!restoreMap.TryGetGitRestorePath(url, out var dir))
                {
                    throw Errors.DependenyRepoNotFound(url).ToException();
                }

                // get dependent docset config or default config
                // todo: what parent config should be pass on its children
                Config.LoadIfExists(dir, _options, out var subConfig);
                result.TryAdd(PathUtility.NormalizeFolder(name), new Docset(_context, dir, subConfig, _options));
            }
            return result;
        }

        private HashSet<Document> CreateBuildScope(IEnumerable<Document> redirections)
        {
            using (Progress.Start("Globbing files"))
            {
                return FileGlob.GetFiles(DocsetPath, Config.Content.Include, Config.Content.Exclude)
                               .Select(file => Document.TryCreateFromFile(this, Path.GetRelativePath(DocsetPath, file)))
                               .Concat(redirections)
                               .ToHashSet();
            }
        }
    }
}
