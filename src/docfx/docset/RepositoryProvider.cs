// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace Microsoft.Docs.Build
{
    internal class RepositoryProvider
    {
        private readonly ConcurrentDictionary<string, Lazy<Repository>> _repositories = new ConcurrentDictionary<string, Lazy<Repository>>();
        private readonly Repository _docsetRepository;
        private readonly Repository _fallbackRepository;
        private readonly RestoreGitMap _restoreGitMap;
        private readonly Config _config;

        public RepositoryProvider(string docsetPath)
            : this(Repository.Create(docsetPath), null, null, null)
        {
        }

        public RepositoryProvider WithConfig(Config config)
        {
            return new RepositoryProvider(_docsetRepository, _fallbackRepository, _restoreGitMap, config);
        }

        public RepositoryProvider WithRestoreMap(RestoreGitMap restoreGitMap)
        {
            return new RepositoryProvider(_docsetRepository, GetFallbackRepository(_docsetRepository, restoreGitMap), restoreGitMap, _config);
        }

        private RepositoryProvider(Repository docsetRepository, Repository fallbackRepository, RestoreGitMap restoreGitMap, Config config)
        {
            _docsetRepository = docsetRepository;
            _fallbackRepository = fallbackRepository;
            _restoreGitMap = restoreGitMap;
            _config = config;
        }

        public Repository GetRepository(FileOrigin origin, string dependencyName = null)
        {
            switch (origin)
            {
                case FileOrigin.Redirection:
                case FileOrigin.Default:
                    return _docsetRepository;

                case FileOrigin.Fallback:
                    return _fallbackRepository;

                case FileOrigin.Template when _config != null && _restoreGitMap != null:
                    return _repositories.GetOrAdd(origin.ToString(), _ =>
                    new Lazy<Repository>(() =>
                    {
                        if (_config.Template.Type != PackageType.Git)
                            return null;

                        var (templatePath, templateCommit) = _restoreGitMap.GetRestoreGitPath(_config.Template, false);
                        return Repository.Create(templatePath, _config.Template.Branch, _config.Template.Url, templateCommit);
                    })).Value;

                case FileOrigin.Dependency when _config != null && _restoreGitMap != null && dependencyName != null:
                    return _repositories.GetOrAdd(origin.ToString(), _ =>
                    new Lazy<Repository>(() =>
                    {
                        if (_config.Dependencies[dependencyName].Type != PackageType.Git)
                            return null;

                        var (dependencyPath, dependencyCommit) = _restoreGitMap.GetRestoreGitPath(_config.Dependencies[dependencyName], false);
                        return Repository.Create(dependencyPath, _config.Template.Branch, _config.Template.Url, dependencyCommit);
                    })).Value;
            }

            return null;
        }

        private static Repository GetFallbackRepository(
            Repository repository,
            RestoreGitMap restoreGitMap)
        {
            Debug.Assert(restoreGitMap != null);

            if (LocalizationUtility.TryGetFallbackRepository(repository, out var fallbackRemote, out string fallbackBranch, out _))
            {
                foreach (var branch in new[] { fallbackBranch, "master" })
                {
                    if (restoreGitMap.IsBranchRestored(fallbackRemote, branch))
                    {
                        var (fallbackRepoPath, fallbackRepoCommit) = restoreGitMap.GetRestoreGitPath(new PackageUrl(fallbackRemote, branch), bare: false);
                        return Repository.Create(fallbackRepoPath, branch, fallbackRemote, fallbackRepoCommit);
                    }
                }
            }

            return default;
        }
    }
}
