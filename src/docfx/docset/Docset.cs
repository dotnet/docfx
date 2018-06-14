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
        /// Gets the redirection mappings
        /// </summary>
        public IReadOnlyDictionary<Document, string> Redirections => _redirections.Value;

        /// <summary>
        /// Gets the dependent docsets
        /// </summary>
        public IReadOnlyDictionary<string, Docset> DependentDocset => _dependentDocsets.Value;

        private readonly CommandLineOptions _options;
        private Lazy<Dictionary<string, Docset>> _dependentDocsets;
        private Lazy<Dictionary<Document, string>> _redirections;

        public Docset(string docsetPath, CommandLineOptions options)
            : this(docsetPath, Config.Load(docsetPath, options), options)
        {
            _dependentDocsets = new Lazy<Dictionary<string, Docset>>(() => LoadDependencies());
            _redirections = new Lazy<Dictionary<Document, string>>(() => LoadRedirectionMappings());
        }

        public Docset(string docsetPath, Config config, CommandLineOptions options)
        {
            DocsetPath = Path.GetFullPath(docsetPath);
            Config = config;

            // pass on the command line options to its children
            _options = options;
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
                result.Add(name, new Docset(dir, config, _options));
            }
            return result;
        }

        private Dictionary<Document, string> LoadRedirectionMappings()
        {
            var mappings = new Dictionary<Document, string>();
            foreach (var (pathToDocset, href) in Config.Redirections)
            {
                var document = Document.TryCreateFromRedirection(this, pathToDocset);
                if (!document.IsMasterContent)
                {
                    // just throw to abort the whole process
                    throw Errors.InvalidRedirection(document);
                }

                mappings.Add(document, href);
            }

            return mappings;
        }
    }
}
