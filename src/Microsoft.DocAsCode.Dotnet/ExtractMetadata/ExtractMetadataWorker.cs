// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;

using Microsoft.Build.Construction;
using Microsoft.Build.Framework;
using Microsoft.Build.Logging;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MSBuild;

using Microsoft.DocAsCode.Common;
using Microsoft.DocAsCode.DataContracts.Common;
using Microsoft.DocAsCode.DataContracts.ManagedReference;

namespace Microsoft.DocAsCode.Dotnet;

internal class ExtractMetadataWorker : IDisposable
{
    private readonly Dictionary<FileType, List<FileInformation>> _files;
    private readonly ExtractMetadataConfig _config;
    private readonly DotnetApiOptions _options;
    private readonly ConsoleLogger _msbuildLogger;

    //Lacks UT for shared workspace
    private readonly MSBuildWorkspace _workspace;

    public ExtractMetadataWorker(ExtractMetadataConfig config, DotnetApiOptions options)
    {
        _config = config;
        _options = options;
        _files = config.Files?.Select(s => new FileInformation(s))
            .GroupBy(f => f.Type)
            .ToDictionary(s => s.Key, s => s.Distinct().ToList());

        var msbuildProperties = config.MSBuildProperties ?? new Dictionary<string, string>();
        if (!msbuildProperties.ContainsKey("Configuration"))
        {
            msbuildProperties["Configuration"] = "Release";
        }

        _msbuildLogger = new(Logger.LogLevelThreshold switch
        {
            LogLevel.Verbose => LoggerVerbosity.Normal,
            LogLevel.Diagnostic => LoggerVerbosity.Diagnostic,
            _ => LoggerVerbosity.Quiet,
        });

        _workspace = MSBuildWorkspace.Create(msbuildProperties);
        _workspace.WorkspaceFailed += (sender, e) => Logger.LogWarning($"{e.Diagnostic}");
    }

    public void Dispose()
    {
        _workspace.Dispose();
    }

    public async Task ExtractMetadataAsync()
    {
        if (_files.TryGetValue(FileType.NotSupported, out List<FileInformation> unsupportedFiles))
        {
            foreach (var file in unsupportedFiles)
            {
                Logger.LogWarning($"Skip unsupported file {file}");
            }
        }

        var hasCompilationError = false;
        var projectCompilations = new HashSet<Compilation>();
        var assemblies = new List<(IAssemblySymbol, Compilation)>();

        if (_files.TryGetValue(FileType.Solution, out var solutionFiles))
        {
            foreach (var solution in solutionFiles.Select(s => s.NormalizedPath))
            {
                Logger.LogInfo($"Loading solution {solution}");
                foreach (var project in SolutionFile.Parse(solution).ProjectsInOrder)
                {
                    if (project.ProjectType is SolutionProjectType.KnownToBeMSBuildFormat &&
                        await LoadCompilationFromProject(project.AbsolutePath) is { } compilation)
                    {
                        projectCompilations.Add(compilation);
                    }
                }
            }
        }

        if (_files.TryGetValue(FileType.Project, out var projectFiles))
        {
            foreach (var projectFile in projectFiles)
            {
                if (await LoadCompilationFromProject(projectFile.NormalizedPath) is { } compilation)
                {
                    projectCompilations.Add(compilation);
                }
            }
        }

        foreach (var compilation in projectCompilations)
        {
            hasCompilationError |= compilation.CheckDiagnostics(_config.AllowCompilationErrors);
            assemblies.Add((compilation.Assembly, compilation));
        }

        if (_files.TryGetValue(FileType.CSSourceCode, out var csFiles))
        {
            var compilation = CompilationHelper.CreateCompilationFromCSharpFiles(csFiles.Select(f => f.NormalizedPath));
            hasCompilationError |= compilation.CheckDiagnostics(_config.AllowCompilationErrors);
            assemblies.Add((compilation.Assembly, compilation));
        }

        if (_files.TryGetValue(FileType.VBSourceCode, out var vbFiles))
        {
            var compilation = CompilationHelper.CreateCompilationFromVBFiles(vbFiles.Select(f => f.NormalizedPath));
            hasCompilationError |= compilation.CheckDiagnostics(_config.AllowCompilationErrors);
            assemblies.Add((compilation.Assembly, compilation));
        }

        if (_files.TryGetValue(FileType.Assembly, out var assemblyFiles))
        {
            foreach (var assemblyFile in assemblyFiles)
            {
                Logger.LogInfo($"Loading assembly {assemblyFile.NormalizedPath}");
                var (compilation, assembly) = CompilationHelper.CreateCompilationFromAssembly(assemblyFile.NormalizedPath, _config.References);
                hasCompilationError |= compilation.CheckDiagnostics(_config.AllowCompilationErrors);
                assemblies.Add((assembly, compilation));
            }
        }

        if (hasCompilationError)
        {
            return;
        }

        if (assemblies.Count <= 0)
        {
            Logger.LogWarning("No .NET API project detected.");
            return;
        }

        var projectMetadataList = new List<MetadataItem>();
        var extensionMethods = assemblies.SelectMany(assembly => assembly.Item1.FindExtensionMethods()).ToArray();
        var filter = new SymbolFilter(_config, _options);
        var allAssemblies = new HashSet<IAssemblySymbol>(assemblies.Select(a => a.Item1), SymbolEqualityComparer.Default);

        foreach (var (assembly, compilation) in assemblies)
        {
            Logger.LogInfo($"Processing {assembly.Name}");
            var projectMetadata = assembly.Accept(new SymbolVisitorAdapter(
                compilation, new(compilation, _config.MemberLayout, allAssemblies), _config, filter, extensionMethods));

            if (projectMetadata != null)
                projectMetadataList.Add(projectMetadata);
        }

        Logger.LogInfo($"Creating output...");
        Dictionary<string, MetadataItem> allMembers;
        Dictionary<string, ReferenceItem> allReferences;
        using (new PerformanceScope("MergeMetadata"))
        {
            allMembers = MergeYamlProjectMetadata(projectMetadataList);
        }

        using (new PerformanceScope("MergeReference"))
        {
            allReferences = MergeYamlProjectReferences(projectMetadataList);
        }

        if (allMembers == null || allMembers.Count == 0)
        {
            var value = StringExtension.ToDelimitedString(projectMetadataList.Select(s => s.Name));
            Logger.Log(LogLevel.Warning, $"No .NET API detected for {value}.");
        }
        else
        {
            using (new PerformanceScope("ResolveAndExport"))
            {
                ResolveAndExportYamlMetadata(allMembers, allReferences);
            }
        }
    }

    private async Task<Compilation> LoadCompilationFromProject(string path)
    {
        var project = _workspace.CurrentSolution.Projects.FirstOrDefault(
            p => FilePathComparer.OSPlatformSensitiveRelativePathComparer.Equals(p.FilePath, path));

        if (project is null)
        {
            Logger.LogInfo($"Loading project {path}");
            if (!_config.NoRestore)
            {
                await Process.Start("dotnet", $"restore \"{path}\"").WaitForExitAsync();
            }
            project = await _workspace.OpenProjectAsync(path, _msbuildLogger);
        }

        if (!project.SupportsCompilation)
        {
            Logger.LogInfo($"Skip unsupported project {project.FilePath}.");
            return null;
        }

        return await project.GetCompilationAsync();
    }

    private void ResolveAndExportYamlMetadata(
        Dictionary<string, MetadataItem> allMembers, Dictionary<string, ReferenceItem> allReferences)
    {
        var outputFileNames = new Dictionary<string, int>(FilePathComparer.OSPlatformSensitiveStringComparer);
        var model = YamlMetadataResolver.ResolveMetadata(allMembers, allReferences, _config.NamespaceLayout);

        var tocFileName = Constants.TocYamlFileName;

        // generate toc.yml
        model.TocYamlViewModel.Type = MemberType.Toc;

        var tocViewModel = new TocRootViewModel
        {
            Metadata = new() { ["memberLayout"] = _config.MemberLayout },
            Items = model.TocYamlViewModel.ToTocViewModel(),
        };
        string tocFilePath = Path.Combine(_config.OutputFolder, tocFileName);

        YamlUtility.Serialize(tocFilePath, tocViewModel, YamlMime.TableOfContent);
        outputFileNames.Add(tocFilePath, 1);

        ApiReferenceViewModel indexer = new();

        // generate each item's yaml
        var members = model.Members;
        foreach (var memberModel in members)
        {
            var fileName = memberModel.Name.Replace('`', '-');
            var outputFileName = GetUniqueFileNameWithSuffix(fileName + Constants.YamlExtension, outputFileNames);
            string itemFilePath = Path.Combine(_config.OutputFolder, outputFileName);
            var memberViewModel = memberModel.ToPageViewModel();
            memberViewModel.ShouldSkipMarkup = _config.ShouldSkipMarkup;
            memberViewModel.MemberLayout = _config.MemberLayout;
            YamlUtility.Serialize(itemFilePath, memberViewModel, YamlMime.ManagedReference);
            Logger.Log(LogLevel.Diagnostic, $"Metadata file for {memberModel.Name} is saved to {itemFilePath}.");
            AddMemberToIndexer(memberModel, outputFileName, indexer);
        }

        // generate manifest file
        JsonUtility.Serialize(Path.Combine(_config.OutputFolder, ".manifest"), indexer, Newtonsoft.Json.Formatting.Indented);
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
            TreeIterator.Preorder(memberModel, null, s => s.Items, (member, parent) =>
            {
                if (indexer.TryGetValue(member.Name, out string path))
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

    private static Dictionary<string, MetadataItem> MergeYamlProjectMetadata(List<MetadataItem> projectMetadataList)
    {
        if (projectMetadataList == null || projectMetadataList.Count == 0)
        {
            return null;
        }

        Dictionary<string, MetadataItem> namespaceMapping = new();
        Dictionary<string, MetadataItem> allMembers = new();

        foreach (var project in projectMetadataList)
        {
            if (project.Items != null)
            {
                foreach (var ns in project.Items)
                {
                    if (ns.Type == MemberType.Namespace)
                    {
                        if (namespaceMapping.TryGetValue(ns.Name, out MetadataItem nsOther))
                        {
                            if (ns.Items != null)
                            {
                                nsOther.Items ??= new List<MetadataItem>();

                                foreach (var i in ns.Items)
                                {
                                    if (!nsOther.Items.Any(s => s.Name == i.Name))
                                    {
                                        nsOther.Items.Add(i);
                                    }
                                    else
                                    {
                                        Logger.Log(LogLevel.Info, $"{i.Name} already exists in {nsOther.Name}, ignore current one");
                                    }
                                }
                            }
                        }
                        else
                        {
                            namespaceMapping.Add(ns.Name, ns);
                        }
                    }

                    if (!allMembers.ContainsKey(ns.Name))
                    {
                        allMembers.Add(ns.Name, ns);
                    }

                    ns.Items?.ForEach(s =>
                    {
                        if (allMembers.TryGetValue(s.Name, out MetadataItem existingMetadata))
                        {
                            Logger.Log(LogLevel.Warning, $"Duplicate member {s.Name} is found from {existingMetadata.Source?.Path} and {s.Source?.Path}, use the one in {existingMetadata.Source?.Path} and ignore the one from {s.Source?.Path}");
                        }
                        else
                        {
                            allMembers.Add(s.Name, s);
                        }

                        s.Items?.ForEach(s1 =>
                        {
                            if (allMembers.TryGetValue(s1.Name, out MetadataItem existingMetadata1))
                            {
                                Logger.Log(LogLevel.Warning, $"Duplicate member {s1.Name} is found from {existingMetadata1.Source?.Path} and {s1.Source?.Path}, use the one in {existingMetadata1.Source?.Path} and ignore the one from {s1.Source?.Path}");
                            }
                            else
                            {
                                allMembers.Add(s1.Name, s1);
                            }
                        });
                    });
                }
            }
        }

        return allMembers;
    }

    private static Dictionary<string, ReferenceItem> MergeYamlProjectReferences(List<MetadataItem> projectMetadataList)
    {
        if (projectMetadataList == null || projectMetadataList.Count == 0)
        {
            return null;
        }

        var result = new Dictionary<string, ReferenceItem>();

        foreach (var project in projectMetadataList)
        {
            if (project.References != null)
            {
                foreach (var pair in project.References)
                {
                    if (!result.ContainsKey(pair.Key))
                    {
                        result[pair.Key] = pair.Value;
                    }
                    else
                    {
                        result[pair.Key].Merge(pair.Value);
                    }
                }
            }
        }

        return result;
    }
}
