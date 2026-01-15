// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Reflection.PortableExecutable;
using Docfx.Common;
using Microsoft.Build.Construction;
using Microsoft.Build.Framework;
using Microsoft.Build.Locator;
using Microsoft.Build.Logging;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.MSBuild;

#nullable enable

namespace Docfx.Dotnet;

partial class DotnetApiCatalog
{
    private static async Task<List<(IAssemblySymbol symbol, Compilation compilation)>> Compile(ExtractMetadataConfig config)
    {
        var latestVersion = MSBuildLocator.QueryVisualStudioInstances().MaxBy(instance => instance.Version);
        if (latestVersion == null)
        {
            throw new InvalidOperationException("Failed to find a version of Visual Studio or .NET SDK installed");
        }

        MSBuildLocator.RegisterInstance(latestVersion);

        var files = config.Files?.Select(s => new FileInformation(s))
            .GroupBy(f => f.Type)
            .ToDictionary(s => s.Key, s => s.Distinct().ToList()) ?? [];

        var msbuildProperties = config.MSBuildProperties ?? [];
        msbuildProperties.TryAdd("Configuration", "Release");

        // TODO: Add BinaryLogger support and print log warnings/errors to console. (It requires Roslyn v5.0.0)
        // NOTE:
        // logger parameter is not works when using Roslyn 4.9.0 or later.
        // It'll be fixed in later releases.
        // - https://github.com/dotnet/roslyn/discussions/71950
        // - https://github.com/dotnet/roslyn/issues/72202
        var msbuildLogger = new ConsoleLogger(Logger.LogLevelThreshold switch
        {
            LogLevel.Verbose => LoggerVerbosity.Normal,
            LogLevel.Diagnostic => LoggerVerbosity.Diagnostic,
            _ => LoggerVerbosity.Quiet,
        });

        using var workspace = MSBuildWorkspace.Create(msbuildProperties);
        workspace.RegisterWorkspaceFailedHandler(e =>
        {
            Logger.LogWarning($"{e.Diagnostic}");
        });

        if (files.TryGetValue(FileType.NotSupported, out var unsupportedFiles))
        {
            foreach (var file in unsupportedFiles)
            {
                Logger.LogWarning($"Skip unsupported file {file.NormalizedPath}");
            }
        }

        var hasCompilationError = false;
        var projectCompilations = new HashSet<Compilation>();
        var assemblies = new List<(IAssemblySymbol, Compilation)>();

        if (files.TryGetValue(FileType.Solution, out var solutionFiles))
        {
            foreach (var solution in solutionFiles.Select(s => s.NormalizedPath))
            {
                Logger.LogInfo($"Loading solution {solution}");
                var sf = SolutionFile.Parse(solution);

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

        if (files.TryGetValue(FileType.Project, out var projectFiles))
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
            hasCompilationError |= compilation.CheckDiagnostics(config.AllowCompilationErrors);
            assemblies.Add((compilation.Assembly, compilation));
        }

        var references = config.References ?? [];
        var metadataReferences = references.Select(CompilationHelper.CreateMetadataReference).ToArray();

        // LoadCompilationFrom C# source files
        if (files.TryGetValue(FileType.CSSourceCode, out var csFiles))
        {
            var compilation = CompilationHelper.CreateCompilationFromCSharpFiles(csFiles.Select(f => f.NormalizedPath), msbuildProperties, metadataReferences);
            hasCompilationError |= compilation.CheckDiagnostics(config.AllowCompilationErrors);
            assemblies.Add((compilation.Assembly, compilation));
        }

        // LoadCompilationFrom VB source files
        if (files.TryGetValue(FileType.VBSourceCode, out var vbFiles))
        {
            var compilation = CompilationHelper.CreateCompilationFromVBFiles(vbFiles.Select(f => f.NormalizedPath), msbuildProperties, metadataReferences);
            hasCompilationError |= compilation.CheckDiagnostics(config.AllowCompilationErrors);
            assemblies.Add((compilation.Assembly, compilation));
        }

        // Load Compilation from assembly files
        if (files.TryGetValue(FileType.Assembly, out var assemblyFiles))
        {
            foreach (var assemblyFile in assemblyFiles)
            {
                var normalizedAssemblyPath = assemblyFile.NormalizedPath;

                using var peReader = new PEReader(new FileStream(normalizedAssemblyPath, FileMode.Open, FileAccess.Read));
                if (!peReader.HasMetadata)
                {
                    Logger.LogInfo($"Skip non-managed assembly {normalizedAssemblyPath}");
                    continue;
                }

                Logger.LogInfo($"Loading assembly {normalizedAssemblyPath}");
                var (compilation, assembly) = CompilationHelper.CreateCompilationFromAssembly(normalizedAssemblyPath, peReader, config.IncludePrivateMembers, metadataReferences);
                hasCompilationError |= compilation.CheckDiagnostics(config.AllowCompilationErrors);
                assemblies.Add((assembly, compilation));
            }
        }

        if (hasCompilationError)
        {
            return [];
        }

        if (assemblies.Count <= 0)
        {
            Logger.LogWarning("No .NET API project detected.");
        }

        return assemblies;

        async Task<Compilation?> LoadCompilationFromProject(string path)
        {
            var project = workspace.CurrentSolution.Projects.FirstOrDefault(
                p => FilePathComparer.OSPlatformSensitiveRelativePathComparer.Equals(p.FilePath, path));

            if (project is null)
            {
                Logger.LogInfo($"Loading project {path}");
                if (!config.NoRestore)
                {
                    using var process = Process.Start("dotnet", $"restore \"{path}\"");
                    await process.WaitForExitAsync();
                }

                // Build using MSBuild APIs
                var buildParams = new Microsoft.Build.Execution.BuildParameters
                {
                    Loggers = new[] { msbuildLogger }
                };

                var buildRequest = new Microsoft.Build.Execution.BuildRequestData(
                    path,
                    msbuildProperties,
                    null,
                    new[] { "Build" },
                    null);

                var buildResult = Microsoft.Build.Execution.BuildManager.DefaultBuildManager.Build(buildParams, buildRequest);

                if (buildResult.OverallResult != Microsoft.Build.Execution.BuildResultCode.Success)
                {
                    Logger.LogWarning($"Build failed for project {path}");
                }

                project = await workspace.OpenProjectAsync(path, msbuildLogger);
            }

            var compilation = await project.GetCompilationAsync();
            if (compilation == null)
            {
                Logger.LogInfo($"Skip unsupported project {project.FilePath}.");
                return null;
            }

            foreach (var unresolvedAnalyzer in project.AnalyzerReferences.OfType<UnresolvedAnalyzerReference>())
            {
                Logger.LogWarning($"There is .NET Analyzer that can't be resolved. "
                                  + $"If this analyzer is .NET Source Generator project. "
                                  + $"Try build with `dotnet build -c Release` command before running docfx. Path: {unresolvedAnalyzer.FullPath}",
                    code: WarningCodes.Metadata.FailedToResolveAnalyzer);
            }

            foreach (var analyzer in project.AnalyzerReferences.OfType<AnalyzerFileReference>())
            {
                analyzer.AnalyzerLoadFailed += (sender, e) =>
                {
                    var analyzerName = analyzer.Display;
                    var errorCode = e.ErrorCode;
                    var referencedCompilerVersion = e.ReferencedCompilerVersion;

                    Logger.LogWarning($"Failed to load .NET Analyzer. AnalyzerName: {analyzerName}, ErrorCode: {errorCode}, ReferencedCompilerVersion: {referencedCompilerVersion}",
                        code: WarningCodes.Metadata.FailedToLoadAnalyzer);
                };
            }

            return compilation;
        }
    }
}
