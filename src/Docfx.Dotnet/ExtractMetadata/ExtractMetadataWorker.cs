// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using Docfx.Common;
using Docfx.DataContracts.Common;
using Docfx.DataContracts.ManagedReference;
using Microsoft.Build.Construction;
using Microsoft.Build.Framework;
using Microsoft.Build.Logging;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MSBuild;

#nullable enable

namespace Docfx.Dotnet;

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
            .ToDictionary(s => s.Key, s => s.Distinct().ToList()) ?? new();

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
        if (_files.TryGetValue(FileType.NotSupported, out var unsupportedFiles))
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

        if (_config.OutputFormat is MetadataOutputFormat.Markdown)
        {
            MarkdownFormatter.Save(assemblies, _config, _options);
            return;
        }

        var projectMetadataList = new List<MetadataItem>();
        var filter = new SymbolFilter(_config, _options);
        var extensionMethods = assemblies.SelectMany(assembly => assembly.Item1.FindExtensionMethods(filter)).ToArray();
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

        using (new PerformanceScope("ResolveAndExport"))
        {
            ResolveAndExportYamlMetadata(allMembers, allReferences);
        }
    }

    private async Task<Compilation?> LoadCompilationFromProject(string path)
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
            var memberViewModel = memberModel.ToPageViewModel(_config);
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
                foreach (var item in node.Items ?? new())
                {
                    MergeNode(item);
                }
                return false;
            }

            if (!result.TryGetValue(node.Name, out var existingNode))
            {
                result.Add(node.Name, node);
                foreach (var item in node.Items ?? new())
                {
                    MergeNode(item);
                }
                return true;
            }

            if (node.Type is MemberType.Namespace or MemberType.Class)
            {
                foreach (var item in node.Items ?? new())
                {
                    if (MergeNode(item))
                    {
                        existingNode.Items ??= new();
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
    }
}
