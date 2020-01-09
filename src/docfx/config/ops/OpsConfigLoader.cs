// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace Microsoft.Docs.Build
{
    internal static class OpsConfigLoader
    {
        public static (OpsConfig opsConfig, string opsConfigFileName, string opsConfigFileContent) LoadOpsConfig(string workingDirectory)
        {
            var fileName = ".openpublishing.publish.config.json";
            var fullPath = Path.Combine(workingDirectory, fileName);
            if (!File.Exists(fullPath))
            {
                return (null, null, null);
            }

            var filePath = new FilePath(Path.GetRelativePath(workingDirectory, fullPath));
            var fileContent = File.ReadAllText(fullPath);
            return (JsonUtility.Deserialize<OpsConfig>(fileContent, filePath), fileName, fileContent);
        }

        public static (string xrefEndpoint, string[] xrefQueryTags, JObject config) LoadDocfxConfig(string docsetPath, string branch)
        {
            var directory = docsetPath;

            do
            {
                var opsConfig = LoadOpsConfig(directory).opsConfig;
                if (opsConfig is null)
                {
                    directory = Path.GetDirectoryName(directory);
                    continue;
                }

                var buildSourceFolder = new PathString(Path.GetRelativePath(directory, docsetPath));

                return ToDocfxConfig(branch, opsConfig, buildSourceFolder);
            }
            while (!string.IsNullOrEmpty(directory));

            return default;
        }

        private static (string xrefEndpoint, string[] xrefQueryTags, JObject config) ToDocfxConfig(string branch, OpsConfig opsConfig, PathString buildSourceFolder)
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

            var docsetConfig = opsConfig.DocsetsToPublish.FirstOrDefault(
                config => config.BuildSourceFolder.FolderEquals(buildSourceFolder));

            if (docsetConfig != null)
            {
                if (!string.IsNullOrEmpty(docsetConfig.DocsetName))
                {
                    result["name"] = docsetConfig.DocsetName;
                    result["extend"] = OpsConfigAdapter.BuildConfigApi;
                }

                result["globalMetadata"] = new JObject
                {
                    ["open_to_public_contributors"] = docsetConfig.OpenToPublicContributors,
                };
            }

            return (opsConfig.XrefEndpoint, docsetConfig?.XrefQueryTags, result);
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
