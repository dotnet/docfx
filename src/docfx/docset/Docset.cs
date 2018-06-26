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
        public IReadOnlyDictionary<string, Docset> DependentDocset => _dependentDocsets.Value;

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
        private readonly Lazy<Dictionary<string, Docset>> _dependentDocsets;
        private readonly Lazy<HashSet<Document>> _buildScope;
        private readonly Lazy<RedirectionMap> _redirections;

        public Docset(Context context, string docsetPath, Config config, CommandLineOptions options)
        {
            DocsetPath = Path.GetFullPath(docsetPath);
            Config = config;

            // pass on the command line options to its children
            _options = options;
            _context = context;
            _buildScope = new Lazy<HashSet<Document>>(() => CreateBuildScope(Redirections.Files));
            _dependentDocsets = new Lazy<Dictionary<string, Docset>>(() => LoadDependencies());
            _redirections = new Lazy<RedirectionMap>(() =>
            {
                var (errors, map) = RedirectionMap.Create(this);
                context.Report("docfx.yml", errors);
                return map;
            });
        }

        private Dictionary<string, Docset> LoadDependencies()
        {
            var result = new Dictionary<string, Docset>(Config.Dependencies.Count);
            foreach (var (name, url) in Config.Dependencies)
            {
                var (dir, _, _) = Restore.GetGitRestoreInfo(url);

                // get dependent docset config or default config
                // todo: what parent config should be pass on its children
                Config.LoadIfExists(dir, _options, out var config);
                result.TryAdd(PathUtility.NormalizeFolder(name), new Docset(_context, dir, config, _options));
            }
            return result;
        }

        private HashSet<Document> CreateBuildScope(IEnumerable<Document> redirections)
        {
            using (ConsoleLog.Measure("Globbing files"))
            {
                return FileGlob.GetFiles(DocsetPath, Config.Content.Include, Config.Content.Exclude)
                               .Select(file => Document.TryCreateFromFile(this, Path.GetRelativePath(DocsetPath, file)))
                               .Concat(redirections)
                               .ToHashSet();
            }
        }
    }
}
