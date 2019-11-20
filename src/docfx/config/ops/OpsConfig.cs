// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

namespace Microsoft.Docs.Build
{
    [JsonObject(NamingStrategyType = typeof(SnakeCaseNamingStrategy))]
    internal class OpsConfig
    {
        public readonly OpsDocsetConfig[] DocsetsToPublish = Array.Empty<OpsDocsetConfig>();

        public readonly OpsDependencyConfig[] DependentRepositories = Array.Empty<OpsDependencyConfig>();

        public readonly string GitRepositoryBranchOpenToPublicContributors;

        public readonly string GitRepositoryUrlOpenToPublicContributors;

        public readonly bool NeedGeneratePdfUrlTemplate;

        public static bool TryLoad(string docsetPath, string branch, out JObject result)
        {
            var directory = docsetPath;

            do
            {
                var fullPath = Path.Combine(directory, ".openpublishing.publish.config.json");
                if (!File.Exists(fullPath))
                {
                    directory = Path.GetDirectoryName(directory);
                    continue;
                }

                var filePath = new FilePath(Path.GetRelativePath(docsetPath, fullPath));
                var opsConfig = JsonUtility.Deserialize<OpsConfig>(File.ReadAllText(fullPath), filePath);
                var buildSourceFolder = PathUtility.NormalizeFolder(Path.GetRelativePath(directory, docsetPath));

                result = ToDocfxConfig(branch, opsConfig, buildSourceFolder);
                return true;
            }
            while (!string.IsNullOrEmpty(directory));

            result = null;
            return false;
        }

        private static JObject ToDocfxConfig(string branch, OpsConfig opsConfig, string buildSourceFolder)
        {
            var result = new JObject();
            var dependencies = GetDependencies(opsConfig, branch, buildSourceFolder);

            result["dependencies"] = new JObject(
                from dep in dependencies
                where !dep.name.Equals("_themes", StringComparison.OrdinalIgnoreCase) &&
                      !dep.name.Equals("_themes.pdf", StringComparison.OrdinalIgnoreCase) &&
                      !dep.name.Equals("_repo.en-us", StringComparison.OrdinalIgnoreCase)
                select new JProperty(dep.path, dep.obj));

            result["template"] = dependencies.FirstOrDefault(
                dep => dep.name.Equals("_themes", StringComparison.OrdinalIgnoreCase)).obj;

            result["output"] = new JObject { ["pdf"] = opsConfig.NeedGeneratePdfUrlTemplate };

            result["contribution"] = new JObject
            {
                ["repositoryUrl"] = opsConfig.GitRepositoryUrlOpenToPublicContributors,
                ["repositoryBranch"] = opsConfig.GitRepositoryBranchOpenToPublicContributors,
            };

            var docsetConfig = opsConfig.DocsetsToPublish.FirstOrDefault(config =>
                PathUtility.PathComparer.Equals(
                    PathUtility.NormalizeFolder(config.BuildSourceFolder), buildSourceFolder));

            if (docsetConfig != null)
            {
                result["name"] = docsetConfig.DocsetName;
                result["globalMetadata"] = new JObject
                {
                    ["open_to_public_contributors"] = docsetConfig.OpenToPublicContributors,
                };
            }

            return result;
        }

        private static (JObject obj, string path, string name)[] GetDependencies(OpsConfig config, string branch, string buildSourceFolder)
        {
            return (
                from dep in config.DependentRepositories
                let path = Path.GetRelativePath(buildSourceFolder, dep.PathToRoot)
                let depBranch = dep.BranchMapping.TryGetValue(branch, out var mappedBranch) ? mappedBranch : dep.Branch
                let obj = new JObject
                {
                    ["url"] = dep.Url,
                    ["includeInBuild"] = dep.IncludeInBuild,
                    ["branch"] = depBranch,
                }
                select (obj, path, dep.PathToRoot)).ToArray();
        }
    }
}
