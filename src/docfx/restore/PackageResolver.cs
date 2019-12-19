// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Polly;
using Polly.Extensions.Http;

namespace Microsoft.Docs.Build
{
    internal class PackageResolver
    {
        private readonly string _docsetPath;
        private readonly Config _config;
        private readonly bool _noFetch;

        public PackageResolver(string docsetPath, Config config, bool noFetch = false)
        {
            _docsetPath = docsetPath;
            _config = config;
            _noFetch = noFetch;
        }

        public string ResolvePackage(PackagePath path)
        {
            if (!_noFetch)
            {
                DownloadPackage(path);
            }

            switch (path.Type)
            {
                case PackageType.Git:
                    var gitdir = GetGitRepositoryPath(path.Url, path.Branch);
                    if (!Directory.Exists(gitdir))
                    {
                        throw Errors.NeedRestore($"{path.Url}#{path.Branch}").ToException();
                    }
                    return gitdir;

                default:
                    if (!Directory.Exists(path.Path))
                    {
                        throw Errors.DirectoryNotFound(new SourceInfo<string>(path.Path)).ToException();
                    }
                    return path.Path;
            }
        }

        private static string GetGitRepositoryPath(string url, string branch)
        {
            return new PathString(Path.Combine(AppData.GitRoot, PathUtility.UrlToShortName(url), branch));
        }

        public void DownloadPackage(PackagePath path)
        {
            switch (path.Type)
            {
                case PackageType.Git:
                    DownloadGitRepository(path.Url, path.Branch);
                    break;
            }
        }

        private void DownloadGitRepository(string url, string branch)
        {
            throw new NotImplementedException();
        }
    }
}
