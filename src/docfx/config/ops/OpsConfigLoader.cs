// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Linq;
using ECMA2Yaml;
using Newtonsoft.Json.Linq;

namespace Microsoft.Docs.Build
{
    internal static class OpsConfigLoader
    {
        public static OpsConfig? LoadOpsConfig(string workingDirectory)
        {
            var fullPath = Path.Combine(workingDirectory, ".openpublishing.publish.config.json");
            if (!File.Exists(fullPath))
            {
                return null;
            }

            var filePath = new FilePath(Path.GetRelativePath(workingDirectory, fullPath));
            return JsonUtility.Deserialize<OpsConfig>(File.ReadAllText(fullPath), filePath);
        }

        public static (string? xrefEndpoint, string[]? xrefQueryTags, JObject? config) LoadDocfxConfig(string docsetPath, Repository? repository)
        {
            if (repository is null)
            {
                return default;
            }

            var opsConfig = LoadOpsConfig(repository.Path);
            if (opsConfig is null)
            {
                return default;
            }

            var buildSourceFolder = new PathString(Path.GetRelativePath(repository.Path, docsetPath));
            return ToDocfxConfig(repository.Branch ?? "master", opsConfig, buildSourceFolder);
        }

        private static (string? xrefEndpoint, string[]? xrefQueryTags, JObject config) ToDocfxConfig(string branch, OpsConfig opsConfig, PathString buildSourceFolder)
        {
            var result = new JObject();
            var dependencies = GetDependencies(opsConfig, branch, buildSourceFolder);

            result["dependencies"] = new JObject(
                from dep in dependencies
                where !dep.name.Equals("_themes", StringComparison.OrdinalIgnoreCase) &&
                      !dep.name.Equals("_themes.pdf", StringComparison.OrdinalIgnoreCase) &&
                      !dep.name.Equals("_repo.en-us", StringComparison.OrdinalIgnoreCase) &&
                      !dep.name.StartsWith("_dependentPackages", StringComparison.OrdinalIgnoreCase)
                select new JProperty(dep.path, dep.obj));

            result["template"] = dependencies.FirstOrDefault(
                dep => dep.name.Equals("_themes", StringComparison.OrdinalIgnoreCase)).obj;

            result["outputPdf"] = opsConfig.NeedGeneratePdfUrlTemplate;

            result["editRepositoryUrl"] = opsConfig.GitRepositoryUrlOpenToPublicContributors;
            result["editRepositoryBranch"] = opsConfig.GitRepositoryBranchOpenToPublicContributors;

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

            result["fileMetadata"] = GenerateJoinTocMetadata(docsetConfig?.JoinTOCPlugin ?? opsConfig.JoinTOCPlugin ?? Array.Empty<OpsJoinTocConfig>());

            var monodoc = GetMonodocConfig(docsetConfig, opsConfig, buildSourceFolder);
            if (monodoc != null)
            {
                result["monodoc"] = JsonUtility.ToJObject(monodoc);
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

        private static JObject? GetMonodocConfig(OpsDocsetConfig? docsetConfig, OpsConfig opsConfig, string buildSourceFolder)
        {
            var result = default(JObject);
            if (docsetConfig?.ECMA2Yaml != null)
            {
                result = JsonUtility.ToJObject(docsetConfig.ECMA2Yaml);
            }
            else if (opsConfig.ECMA2Yaml != null)
            {
                result = JsonUtility.ToJObject(opsConfig.ECMA2Yaml);
                result[nameof(ECMA2YamlRepoConfig.SourceXmlFolder)] = Path.GetRelativePath(buildSourceFolder, opsConfig.ECMA2Yaml.SourceXmlFolder);
                result[nameof(ECMA2YamlRepoConfig.OutputYamlFolder)] = Path.GetRelativePath(buildSourceFolder, opsConfig.ECMA2Yaml.OutputYamlFolder);
            }
            return result;
        }

        private static JObject GenerateJoinTocMetadata(OpsJoinTocConfig[] configs)
        {
            var conceptualToc = new JObject();
            var refToc = new JObject();

            foreach (var config in configs)
            {
                if (!string.IsNullOrEmpty(config.ConceptualTOC) && !string.IsNullOrEmpty(config.ReferenceTOCUrl))
                {
                    refToc[config.ConceptualTOC] = config.ReferenceTOCUrl;
                }
                if (!string.IsNullOrEmpty(config.ReferenceTOC) && !string.IsNullOrEmpty(config.ConceptualTOCUrl))
                {
                    conceptualToc[config.ReferenceTOC] = config.ConceptualTOCUrl;
                }
            }

            return new JObject
            {
                ["universal_conceptual_toc"] = conceptualToc,
                ["universal_ref_toc"] = refToc,
            };
        }
    }
}
