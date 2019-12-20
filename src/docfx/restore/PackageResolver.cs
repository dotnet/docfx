// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http.Headers;

namespace Microsoft.Docs.Build
{
    internal class PackageResolver
    {
        internal static Func<string, string> GitRemoteProxy;

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
                    var dir = Path.Combine(_docsetPath, path.Path);
                    if (!Directory.Exists(dir))
                    {
                        throw Errors.DirectoryNotFound(new SourceInfo<string>(path.Path)).ToException();
                    }
                    return dir;
            }
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

        private void DownloadGitRepository(string url, string committish, bool depthOne)
        {
            var cwd = GetGitRepositoryPath(url, committish);

            Directory.CreateDirectory(cwd);
            GitUtility.ExecuteNonQuery(cwd, "init");

            // Allow test to proxy remotes to local folder
            if (GitRemoteProxy != null)
            {
                url = GitRemoteProxy(url);
            }

            var (httpOption, secrets) = GetGitCommandLineConfig(url);
            var depthOneOption = depthOne ? "--depth 1" : "--depth 9999999";
            var options = $"--progress --update-head-ok --prune {depthOneOption}";

            try
            {
                GitUtility.ExecuteNonQuery(cwd, $"{httpOption} fetch {options} \"{url}\" +{committish}:{committish}", secrets);
            }
            catch (InvalidOperationException)
            {
                // Fallback to fetch all branches if the input committish is not supported by fetch
                GitUtility.ExecuteNonQuery(cwd, $"{httpOption} fetch {options} \"{url}\" +refs/heads/*:refs/heads/*", secrets);
            }

            GitUtility.ExecuteNonQuery(cwd, $"-c core.longpaths=true checkout --force --progress {committish}");
        }

        private static string GetGitRepositoryPath(string url, string branch)
        {
            return new PathString(Path.Combine(AppData.GitRoot, PathUtility.UrlToShortName(url), branch));
        }

        private (string cmd, string[] secrets) GetGitCommandLineConfig(string url)
        {
            var gitConfigs = (
                from http in _config.Http
                where url.StartsWith(http.Key)
                from header in http.Value.Headers
                select (cmd: $"-c http.{http.Key}.extraheader=\"{header.Key}: {header.Value}\"", secret: GetSecretFromHeader(header))).ToArray();

            return (string.Join(' ', gitConfigs.Select(item => item.cmd)), gitConfigs.Select(item => item.secret).ToArray());

            string GetSecretFromHeader(KeyValuePair<string, string> header)
            {
                if (header.Key.Equals("Authorization", StringComparison.OrdinalIgnoreCase) &&
                    AuthenticationHeaderValue.TryParse(header.Value, out var value))
                {
                    return value.Parameter;
                }
                return header.Value;
            }
        }
    }
}
