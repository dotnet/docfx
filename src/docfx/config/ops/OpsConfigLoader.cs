// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ECMA2Yaml;
using Newtonsoft.Json.Linq;

namespace Microsoft.Docs.Build
{
    internal static class OpsConfigLoader
    {
        public static (List<Error>, OpsConfig?) LoadOpsConfig(string workingDirectory)
        {
            var fullPath = Path.Combine(workingDirectory, ".openpublishing.publish.config.json");
            if (!File.Exists(fullPath))
            {
                return (new List<Error>(), null);
            }

            var filePath = new FilePath(Path.GetRelativePath(workingDirectory, fullPath));
            return JsonUtility.Deserialize<OpsConfig>(File.ReadAllText(fullPath), filePath);
        }

        public static (List<Error> errors, string? xrefEndpoint, string[]? xrefQueryTags, JObject? config) LoadDocfxConfig(
            string docsetPath, Repository? repository)
        {
            if (repository is null)
            {
                return (new List<Error>(), default, default, default);
            }

            var (errors, opsConfig) = LoadOpsConfig(repository.Path);
            if (opsConfig is null)
            {
                return (new List<Error>(), default, default, default);
            }

            var buildSourceFolder = new PathString(Path.GetRelativePath(repository.Path, docsetPath));
            var (configErrors, xrefEndpoint, xrefQueryTags, config) = ToDocfxConfig(repository.Branch ?? "master", opsConfig, buildSourceFolder);
            errors.AddRange(configErrors);
            return (errors, xrefEndpoint, xrefQueryTags, config);
        }

        private static (List<Error>, string? xrefEndpoint, string[]? xrefQueryTags, JObject config) ToDocfxConfig(
            string branch, OpsConfig opsConfig, PathString buildSourceFolder)
        {
            var result = new JObject();
            var (errors, dependencies) = GetDependencies(opsConfig, branch, buildSourceFolder);

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

            result["fileMetadata"] =
                GenerateJoinTocMetadata(docsetConfig?.JoinTOCPlugin ?? opsConfig.JoinTOCPlugin ?? Array.Empty<OpsJoinTocConfig>(), buildSourceFolder);

            var monodoc = GetMonodocConfig(docsetConfig, opsConfig, buildSourceFolder);
            if (monodoc != null)
            {
                result["monodoc"] = monodoc;
                result["sourceMap"] = new JArray(monodoc.Select((_, index) => $".sourcemap-{index}.json"));
            }

            return (errors, opsConfig.XrefEndpoint, docsetConfig?.XrefQueryTags, result);
        }

        private static (List<Error>, (JObject obj, string path, string name)[]) GetDependencies(OpsConfig config, string branch, string buildSourceFolder)
        {
            return
                (config.DependentRepositories
                    .Where(x => string.IsNullOrEmpty(x.PathToRoot))
                    .Select(x => Errors.Config.EmptyPathToRoot(x.Url, x.PathToRoot.Source))
                    .ToList(),
                (from dep in config.DependentRepositories.Where(x => !string.IsNullOrEmpty(x.PathToRoot))
                let path = Path.GetRelativePath(buildSourceFolder, dep.PathToRoot)
                let depBranch = dep.BranchMapping.TryGetValue(branch, out var mappedBranch) ? mappedBranch : dep.Branch
                let obj = new JObject
                {
                    ["url"] = dep.Url,
                    ["includeInBuild"] = dep.IncludeInBuild,
                    ["branch"] = depBranch,
                }
                select (obj, path, dep.PathToRoot.Value)).ToArray());
        }

        private static JArray? GetMonodocConfig(OpsDocsetConfig? docsetConfig, OpsConfig opsConfig, string buildSourceFolder)
        {
            var result = new JArray();
            if (docsetConfig?.ECMA2Yaml != null)
            {
                foreach (var ecma2Yaml in docsetConfig.ECMA2Yaml)
                {
                    result.Add(JsonUtility.ToJObject(ecma2Yaml));
                }
            }
            else if (opsConfig.ECMA2Yaml != null)
            {
                foreach (var ecma2Yaml in opsConfig.ECMA2Yaml)
                {
                    var ecma2YamlJObject = JsonUtility.ToJObject(ecma2Yaml);
                    ecma2YamlJObject[nameof(ECMA2YamlRepoConfig.SourceXmlFolder)] = Path.GetRelativePath(buildSourceFolder, ecma2Yaml.SourceXmlFolder);
                    ecma2YamlJObject[nameof(ECMA2YamlRepoConfig.OutputYamlFolder)] = Path.GetRelativePath(buildSourceFolder, ecma2Yaml.OutputYamlFolder);
                    result.Add(ecma2YamlJObject);
                }
            }
            return result.Count == 0 ? null : result;
        }

        private static JObject GenerateJoinTocMetadata(OpsJoinTocConfig[] configs, string buildSourceFolder)
        {
            var conceptualToc = new JObject();
            var refToc = new JObject();

            foreach (var config in configs)
            {
                if (!string.IsNullOrEmpty(config.ConceptualTOC) && !string.IsNullOrEmpty(config.ReferenceTOCUrl))
                {
                    refToc[Path.GetRelativePath(buildSourceFolder, config.ConceptualTOC)] = config.ReferenceTOCUrl;
                }
                if (!string.IsNullOrEmpty(config.ReferenceTOC) && !string.IsNullOrEmpty(config.ConceptualTOCUrl))
                {
                    conceptualToc[Path.GetRelativePath(buildSourceFolder, config.ReferenceTOC)] = config.ConceptualTOCUrl;
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
