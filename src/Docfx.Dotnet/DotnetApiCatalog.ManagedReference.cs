// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Docfx.Common;
using Docfx.DataContracts.Common;
using Docfx.DataContracts.ManagedReference;
using Microsoft.CodeAnalysis;

#nullable enable

namespace Docfx.Dotnet;

partial class DotnetApiCatalog
{
    private static void CreateManagedReference(List<(IAssemblySymbol symbol, Compilation compilation)> assemblies, ExtractMetadataConfig config, DotnetApiOptions options)
    {
        var projectMetadataList = new List<MetadataItem>();
        var filter = new SymbolFilter(config, options);
        var extensionMethods = assemblies.SelectMany(assembly => assembly.Item1.FindExtensionMethods(filter)).ToArray();
        var allAssemblies = new HashSet<IAssemblySymbol>(assemblies.Select(a => a.Item1), SymbolEqualityComparer.Default);

        foreach (var (assembly, compilation) in assemblies)
        {
            Logger.LogInfo($"Processing {assembly.Name}");
            var projectMetadata = assembly.Accept(new SymbolVisitorAdapter(
                compilation, new(compilation, config.MemberLayout, allAssemblies), config, filter, extensionMethods));

            if (projectMetadata != null)
                projectMetadataList.Add(projectMetadata);
        }

        Logger.LogInfo($"Creating output...");
        var allMembers = new Dictionary<string, MetadataItem>();
        var allReferences = new Dictionary<string, ReferenceItem>();
        MergeMembers(allMembers, projectMetadataList);
        MergeReferences(allReferences, projectMetadataList);

        if (allMembers.Count == 0)
        {
            var value = StringExtension.ToDelimitedString(projectMetadataList.Select(s => s.Name));
            Logger.Log(LogLevel.Warning, $"No .NET API detected for {value}.");
            return;
        }

        ResolveAndExportYamlMetadata(allMembers, allReferences);

        return;


        void ResolveAndExportYamlMetadata(
        Dictionary<string, MetadataItem> allMembers, Dictionary<string, ReferenceItem> allReferences)
        {
            var outputFileNames = new Dictionary<string, int>(FilePathComparer.OSPlatformSensitiveStringComparer);
            var model = YamlMetadataResolver.ResolveMetadata(allMembers, allReferences, config.NamespaceLayout);

            // generate toc.yml
            model.TocYamlViewModel.Type = MemberType.Toc;

            var tocViewModel = new TocItemViewModel
            {
                Metadata = new() { ["memberLayout"] = config.MemberLayout },
                Items = model.TocYamlViewModel.ToTocViewModel(),
            };
            string tocFilePath = Path.Combine(config.OutputFolder, "toc.yml");

            YamlUtility.Serialize(tocFilePath, tocViewModel, YamlMime.TableOfContent);
            outputFileNames.Add(tocFilePath, 1);

            ApiReferenceViewModel indexer = [];

            // generate each item's yaml
            var members = model.Members;
            foreach (var memberModel in members)
            {
                var fileName = memberModel.Name.Replace('`', '-');
                var outputFileName = GetUniqueFileNameWithSuffix(fileName + Constants.YamlExtension, outputFileNames);
                string itemFilePath = Path.Combine(config.OutputFolder, outputFileName);
                var memberViewModel = memberModel.ToPageViewModel(config);
                memberViewModel.ShouldSkipMarkup = config.ShouldSkipMarkup;
                memberViewModel.MemberLayout = config.MemberLayout;
                YamlUtility.Serialize(itemFilePath, memberViewModel, YamlMime.ManagedReference);
                Logger.Log(LogLevel.Diagnostic, $"Metadata file for {memberModel.Name} is saved to {itemFilePath}.");
                AddMemberToIndexer(memberModel, outputFileName, indexer);
            }

            // generate manifest file
            JsonUtility.Serialize(Path.Combine(config.OutputFolder, ".manifest"), indexer, indented: true);
        }
    }

    private static string GetUniqueFileNameWithSuffix(string fileName, Dictionary<string, int> existingFileNames)
    {
        if (existingFileNames.TryGetValue(fileName, out int suffix))
        {
            existingFileNames[fileName] = suffix + 1;
            var newFileName = $"{fileName}_{suffix}";
            var extensionIndex = fileName.LastIndexOf('.');
            if (extensionIndex > -1)
            {
                newFileName = $"{fileName.Substring(0, extensionIndex)}_{suffix}.{fileName.Substring(extensionIndex + 1)}";
            }
            return GetUniqueFileNameWithSuffix(newFileName, existingFileNames);
        }
        else
        {
            existingFileNames[fileName] = 1;
            return fileName;
        }
    }

    private static void AddMemberToIndexer(MetadataItem memberModel, string outputPath, ApiReferenceViewModel indexer)
    {
        if (memberModel.Type == MemberType.Namespace)
        {
            indexer.Add(memberModel.Name, outputPath);
        }
        else
        {
            TreeIterator.Preorder(memberModel, null, s => s!.Items, (member, parent) =>
            {
                if (indexer.TryGetValue(member!.Name, out var path))
                {
                    Logger.LogWarning($"{member.Name} already exists in {path}, the duplicate one {outputPath} will be ignored.");
                }
                else
                {
                    indexer.Add(member.Name, outputPath);
                }
                return true;
            });
        }
    }

    private static void MergeMembers(Dictionary<string, MetadataItem> result, List<MetadataItem> items)
    {
        foreach (var item in items)
        {
            MergeNode(item);
        }

        bool MergeNode(MetadataItem node)
        {
            if (node.Type is MemberType.Assembly)
            {
                foreach (var item in node.Items ?? [])
                {
                    MergeNode(item);
                }
                return false;
            }

            if (!result.TryGetValue(node.Name, out var existingNode))
            {
                result.Add(node.Name, node);
                foreach (var item in node.Items ?? [])
                {
                    MergeNode(item);
                }
                return true;
            }

            if (node.Type is MemberType.Namespace or MemberType.Class)
            {
                foreach (var item in node.Items ?? [])
                {
                    if (MergeNode(item))
                    {
                        existingNode.Items ??= [];
                        existingNode.Items.Add(item);
                    }
                }
                return false;
            }

            Logger.Log(LogLevel.Warning, $"Ignore duplicated member {node.Type}:{node.Name} from {node.Source?.Path} as it already exist in {existingNode.Source?.Path}.");
            return false;
        }
    }

    private static void MergeReferences(Dictionary<string, ReferenceItem> result, List<MetadataItem> items)
    {
        foreach (var project in items)
        {
            if (project.References != null)
            {
                foreach (var pair in project.References)
                {
                    if (result.TryGetValue(pair.Key, out var value))
                    {
                        value.Merge(pair.Value);
                    }
                    else
                    {
                        result[pair.Key] = pair.Value;
                    }
                }
            }
        }
    }
}
