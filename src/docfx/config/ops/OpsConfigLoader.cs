// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using ECMA2Yaml;
using Newtonsoft.Json.Linq;

namespace Microsoft.Docs.Build;

internal static class OpsConfigLoader
{
    public static OpsConfig? LoadOpsConfig(ErrorBuilder errors, Package package, Repository? repository)
    {
        if (repository is null)
        {
            return default;
        }

        var opDirRelativeToPackage = Path.GetRelativePath(package.BasePath, repository.Path);
        var fullPath = new PathString(Path.Combine(package.BasePath, opDirRelativeToPackage, ".openpublishing.publish.config.json"));
        if (!package.Exists(fullPath))
        {
            return default;
        }

        var filePath = new FilePath(Path.GetRelativePath(package.BasePath, fullPath));
        return JsonUtility.Deserialize<OpsConfig>(errors, package.ReadString(fullPath), filePath);
    }

    public static (string? xrefEndpoint, string[]? xrefQueryTags, JObject? config) LoadDocfxConfig(
        ErrorBuilder errors, Repository? repository, Package package)
    {
        if (repository is null)
        {
            return (default, default, default);
        }

        var opsConfig = LoadOpsConfig(errors, package, repository);
        if (opsConfig is null)
        {
            return (default, default, default);
        }

        var buildSourceFolder = new PathString(Path.GetRelativePath(repository.Path, package.BasePath));
        return ToDocfxConfig(repository.Branch, opsConfig, buildSourceFolder);
    }

    private static (string? xrefEndpoint, string[]? xrefQueryTags, JObject config) ToDocfxConfig(
        string? branch, OpsConfig opsConfig, PathString buildSourceFolder)
    {
        var result = new JObject();
        var dependencies = GetDependencies(opsConfig, branch, buildSourceFolder);

        result["urlType"] = "docs";
        result["dependencies"] = new JObject(
            from dep in dependencies
            where !dep.name.Equals("_themes", StringComparison.OrdinalIgnoreCase) &&
                  !dep.name.Equals("_themes.pdf", StringComparison.OrdinalIgnoreCase) &&
                  !dep.name.Equals("_repo.en-us", StringComparison.OrdinalIgnoreCase) &&
                  !dep.name.StartsWith("_dependentPackages", StringComparison.OrdinalIgnoreCase)
            select new JProperty(dep.path, dep.obj));

        var template = dependencies.FirstOrDefault(
            dep => dep.name.Equals("_themes", StringComparison.OrdinalIgnoreCase)).obj;

        // override template branch to main for live branch build
        if (template != null && branch == "live")
        {
            template["branch"] = "main";
        }

        result["template"] = template;

        result["outputPdf"] = opsConfig.NeedGeneratePdfUrlTemplate;

        result["editRepositoryUrl"] = opsConfig.GitRepositoryUrlOpenToPublicContributors;
        result["editRepositoryBranch"] = opsConfig.GitRepositoryBranchOpenToPublicContributors;
        result["fallbackRepository"] = dependencies.FirstOrDefault(
            dep => dep.name.Equals("_repo.en-us", StringComparison.OrdinalIgnoreCase)).obj;
        result["redirectionFiles"] = JToken.FromObject(opsConfig.RedirectionFiles);

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

            var splitTOCSet = new HashSet<PathString>(docsetConfig.SplitTOC);

            foreach (var item in opsConfig.SplitTOC)
            {
                if (item.StartsWithPath(buildSourceFolder, out var splitTOCRelativeToDocset))
                {
                    splitTOCSet.Add(splitTOCRelativeToDocset);
                }
            }

            result["splitTOC"] = JArray.FromObject(splitTOCSet);
        }

        var joinTOCPluginConfig = docsetConfig?.JoinTOCPlugin ?? opsConfig.JoinTOCPlugin ?? Array.Empty<OpsJoinTocConfig>();
        (result["fileMetadata"], result["joinTOC"]) =
            GenerateJoinTocMetadataAndConfig(
                joinTOCPluginConfig,
                docsetConfig?.JoinTOCPlugin != null ? default : new PathString(buildSourceFolder));
        var sourceMaps = new JArray();

        var monodoc = GetMonodocConfig(docsetConfig, opsConfig, buildSourceFolder);
        if (monodoc != null)
        {
            result["monodoc"] = monodoc;
            sourceMaps.AddRange(monodoc.Select((_, index) => $".sourcemap-ecma-{index}.json"));
        }

        var maml2YamlMonikerPath = GetMAML2YamlMonikerPath(docsetConfig, opsConfig);
        if (maml2YamlMonikerPath != null)
        {
            result["mamlMonikerPath"] = maml2YamlMonikerPath;
            sourceMaps.AddRange(maml2YamlMonikerPath.Select((_, index) => $".sourcemap-maml-{index}.json"));
        }

        result["sourceMap"] = sourceMaps;
        result["isLearn"] = IsLearn(docsetConfig);
        result["microsoftGraphTenantId"] = "72f988bf-86f1-41af-91ab-2d7cd011db47";
        result["microsoftGraphClientId"] = OpsAccessor.DocsEnvironment == DocsEnvironment.Prod
            ? "323e04d2-a55f-4ee7-a3fb-a548b728be48"
            : "b799e059-9dd8-4839-a39c-96f7531e55e2";

        return (opsConfig.XrefEndpoint, docsetConfig?.XrefQueryTags, result);
    }

    private static (JObject obj, string path, string name)[] GetDependencies(OpsConfig config, string? branch, string buildSourceFolder)
    {
        return
            (from dep in config.DependentRepositories
             let path = new PathString(buildSourceFolder).GetRelativePath(dep.PathToRoot)
             let depBranch = branch != null && dep.BranchMapping.TryGetValue(branch, out var mappedBranch) ? mappedBranch : dep.Branch
             let obj = new JObject
             {
                 ["url"] = dep.Url,
                 ["includeInBuild"] = dep.IncludeInBuild,
                 ["branch"] = depBranch,
             }
             select (obj, path, dep.PathToRoot.Value)).ToArray();
    }

    private static JArray? GetMonodocConfig(OpsDocsetConfig? docsetConfig, OpsConfig opsConfig, string buildSourceFolder)
    {
        var result = new JArray();
        var ecma2YamlConfig = docsetConfig?.ECMA2Yaml ?? opsConfig.ECMA2Yaml;
        if (ecma2YamlConfig != null)
        {
            foreach (var ecma2Yaml in ecma2YamlConfig)
            {
                var ecma2YamlJObject = JsonUtility.ToJObject(ecma2Yaml);
                ecma2YamlJObject[nameof(ECMA2YamlRepoConfig.SourceXmlFolder)] = Path.GetRelativePath(buildSourceFolder, ecma2Yaml.SourceXmlFolder);
                ecma2YamlJObject[nameof(ECMA2YamlRepoConfig.OutputYamlFolder)] = Path.GetRelativePath(buildSourceFolder, ecma2Yaml.OutputYamlFolder);
                result.Add(ecma2YamlJObject);
            }
        }
        return result.Count == 0 ? null : result;
    }

    private static JArray? GetMAML2YamlMonikerPath(OpsDocsetConfig? docsetConfig, OpsConfig opsConfig)
    {
        var maml2YamlMonikerPath = docsetConfig?.MonikerPath ?? opsConfig.MonikerPath;
        return maml2YamlMonikerPath == null || maml2YamlMonikerPath.Length == 0
            ? null
            : new JArray(maml2YamlMonikerPath);
    }

    private static bool IsLearn(OpsDocsetConfig? docsetConfig)
        => docsetConfig?.CustomizedTasks != null
        && docsetConfig.CustomizedTasks.TryGetValue("docset_postbuild", out var plugins)
        && plugins.Any(plugin => plugin.EndsWith("TripleCrownValidation.ps1", StringComparison.OrdinalIgnoreCase));

    private static (JObject joinTocMetadata, JArray joinTocConfig) GenerateJoinTocMetadataAndConfig(
        OpsJoinTocConfig[] configs,
        PathString baseDir)
    {
        var conceptualToc = new JObject();
        var refToc = new JObject();
        var joinTocConfig = new JArray();

        foreach (var config in configs)
        {
            if (!string.IsNullOrEmpty(config.ConceptualTOC) && !string.IsNullOrEmpty(config.ReferenceTOCUrl))
            {
                refToc[baseDir.GetRelativePath(new PathString(config.ConceptualTOC))] = config.ReferenceTOCUrl;
                refToc[Path.GetRelativePath(baseDir, config.ConceptualTOC)] = config.ReferenceTOCUrl;
                var conceptualTOCDir = Path.GetDirectoryName(config.ConceptualTOC);
                var conceptualTOCRelativeDir = Path.GetRelativePath(baseDir, string.IsNullOrEmpty(conceptualTOCDir) ? "." : conceptualTOCDir);
                refToc[Path.Combine(conceptualTOCRelativeDir, "_splitted/**")] = config.ReferenceTOCUrl;
            }

            if (!string.IsNullOrEmpty(config.ReferenceTOC) && !string.IsNullOrEmpty(config.ConceptualTOCUrl))
            {
                conceptualToc[Path.GetRelativePath(baseDir, config.ReferenceTOC)] = config.ConceptualTOCUrl;
                var refTOCDir = Path.GetDirectoryName(config.ReferenceTOC);
                var refTOCRelativeDir = Path.GetRelativePath(baseDir, string.IsNullOrEmpty(refTOCDir) ? "." : refTOCDir);
                conceptualToc[Path.Combine(refTOCRelativeDir, "_splitted/**")] = config.ConceptualTOCUrl;
                conceptualToc[baseDir.GetRelativePath(new PathString(config.ReferenceTOC))] = config.ConceptualTOCUrl;
            }

            if (!string.IsNullOrEmpty(config.ReferenceTOC) && !string.IsNullOrEmpty(config.ReferenceTOCUrl))
            {
                var refTOCDir = Path.GetDirectoryName(config.ReferenceTOC);
                var refTOCRelativeDir = Path.GetRelativePath(baseDir, string.IsNullOrEmpty(refTOCDir) ? "." : refTOCDir);
                refToc[Path.Combine(refTOCRelativeDir, "_splitted/**")] = config.ReferenceTOCUrl;
            }

            var item = new JObject();
            if (!string.IsNullOrEmpty(config.OutputFolder))
            {
                item["outputFolder"] = baseDir.GetRelativePath(new PathString(config.OutputFolder));
            }
            if (config.ContainerPageMetadata != null)
            {
                item["containerPageMetadata"] = config.ContainerPageMetadata;
            }
            if (!string.IsNullOrEmpty(config.ReferenceTOC))
            {
                item["referenceToc"] = baseDir.GetRelativePath(new PathString(config.ReferenceTOC));
            }
            if (!string.IsNullOrEmpty(config.TopLevelTOC))
            {
                item["topLevelToc"] = baseDir.GetRelativePath(new PathString(config.TopLevelTOC));
            }
            if (!string.IsNullOrEmpty(config.OriginalReferenceTOC))
            {
                item["originalReferenceToc"] = baseDir.GetRelativePath(new PathString(config.OriginalReferenceTOC));
            }

            joinTocConfig.Add(item);
        }

        return (new JObject
        {
            ["universal_conceptual_toc"] = conceptualToc,
            ["universal_ref_toc"] = refToc,
        }, joinTocConfig);
    }
}
