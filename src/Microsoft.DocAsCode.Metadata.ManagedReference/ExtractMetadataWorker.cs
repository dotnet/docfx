// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Metadata.ManagedReference
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;

    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.MSBuild;
    using Microsoft.DotNet.ProjectModel.Workspaces;

    using Microsoft.DocAsCode.Common;
    using Microsoft.DocAsCode.DataContracts.Common;
    using Microsoft.DocAsCode.DataContracts.ManagedReference;
    using Microsoft.DocAsCode.Exceptions;
    using Microsoft.DocAsCode.Plugins;

    public sealed class ExtractMetadataWorker : IDisposable
    {
        private static readonly string[] SupportedSolutionExtensions = { ".sln" };
        private static readonly string[] SupportedProjectName = { "project.json" };
        private static readonly string[] SupportedProjectExtensions = { ".csproj", ".vbproj" };
        private static readonly string[] SupportedSourceFileExtensions = { ".cs", ".vb" };
        private static readonly string[] SupportedVBSourceFileExtensions = { ".vb" };
        private static readonly string[] SupportedCSSourceFileExtensions = { ".cs" };
        private static readonly string[] SupportedAssemblyExtensions = { ".dll", ".exe" };
        private static readonly string SupportedCommentFileExtension = ".xml";
        private static readonly List<string> SupportedExtensions = new List<string>();
        private readonly ExtractMetadataInputModel _validInput;
        private readonly ExtractMetadataInputModel _rawInput;
        private readonly bool _rebuild;
        private readonly bool _shouldSkipMarkup;
        private readonly bool _preserveRawInlineComments;
        private readonly string _filterConfigFile;
        private readonly bool _useCompatibilityFileName;

        // TODO: refactor incremental logic
        private readonly Dictionary<string, string> _msbuildProperties;
        private readonly Lazy<MSBuildWorkspace> _workspace;
        static ExtractMetadataWorker()
        {
            SupportedExtensions.AddRange(SupportedSolutionExtensions);
            SupportedExtensions.AddRange(SupportedProjectExtensions);
            SupportedExtensions.AddRange(SupportedSourceFileExtensions);
            SupportedExtensions.AddRange(SupportedAssemblyExtensions);
        }

        public ExtractMetadataWorker(ExtractMetadataInputModel input, bool rebuild, bool useCompatibilityFileName)
        {
            _rawInput = input;
            _validInput = ValidateInput(input);
            _rebuild = rebuild;
            _shouldSkipMarkup = input.ShouldSkipMarkup;
            _preserveRawInlineComments = input.PreserveRawInlineComments;
            if (input.FilterConfigFile != null)
            {
                _filterConfigFile = Path.GetFullPath(Path.Combine(EnvironmentContext.BaseDirectory, input.FilterConfigFile)).Normalize();
            }
            _useCompatibilityFileName = useCompatibilityFileName;

            _msbuildProperties = input.MSBuildProperties ?? new Dictionary<string, string>();
            if (!_msbuildProperties.ContainsKey("Configuration"))
            {
                _msbuildProperties["Configuration"] = "Release";
            }

            _workspace = new Lazy<MSBuildWorkspace>(() => MSBuildWorkspace.Create(_msbuildProperties));
        }

        public async Task ExtractMetadataAsync()
        {
            var validInput = _validInput;
            if (validInput == null)
            {
                return;
            }

            try
            {
                foreach (var pair in validInput.Items)
                {
                    var inputs = pair.Value;
                    var outputFolder = pair.Key;
                    await SaveAllMembersFromCacheAsync(inputs, outputFolder, _rebuild);
                }
            }
            catch (Exception e)
            {
                throw new ExtractMetadataException($"Error extracting metadata for {_rawInput}: {e.Message}", e);
            }
        }

        #region Internal For UT
        internal static MetadataItem GenerateYamlMetadata(Compilation compilation, IAssemblySymbol assembly = null, bool preserveRawInlineComments = false, string filterConfigFile = null, IReadOnlyDictionary<Compilation, IEnumerable<IMethodSymbol>> extensionMethods = null)
        {
            if (compilation == null)
            {
                return null;
            }

            object visitorContext = new object();
            SymbolVisitorAdapter visitor;
            if (compilation.Language == "Visual Basic")
            {
                visitor = new SymbolVisitorAdapter(new CSYamlModelGenerator() + new VBYamlModelGenerator(), SyntaxLanguage.VB, compilation, preserveRawInlineComments, filterConfigFile, extensionMethods);
            }
            else if (compilation.Language == "C#")
            {
                visitor = new SymbolVisitorAdapter(new CSYamlModelGenerator() + new VBYamlModelGenerator(), SyntaxLanguage.CSharp, compilation, preserveRawInlineComments, filterConfigFile, extensionMethods);
            }
            else
            {
                Debug.Assert(false, "Language not supported: " + compilation.Language);
                Logger.Log(LogLevel.Error, "Language not supported: " + compilation.Language);
                return null;
            }

            assembly = assembly ?? compilation.Assembly;
            MetadataItem item = assembly.Accept(visitor);
            return item;
        }

        internal static IReadOnlyDictionary<Compilation, IEnumerable<IMethodSymbol>> GetAllExtensionMethodsFromCompilation(IEnumerable<Compilation> compilations)
        {
            var methods = new Dictionary<Compilation, IEnumerable<IMethodSymbol>>();
            foreach (var compilation in compilations)
            {
                if (compilation.Assembly.MightContainExtensionMethods)
                {
                    var extensions = (from n in GetAllNamespaceMembers(compilation.Assembly).Distinct()
                                      from m in GetExtensionMethodPerNamespace(n)
                                      select m).ToList();
                    if (extensions.Count > 0)
                    {
                        methods[compilation] = extensions;
                    }
                }
            }
            return methods;
        }

        internal static IReadOnlyDictionary<Compilation, IEnumerable<IMethodSymbol>> GetAllExtensionMethodsFromAssembly(Compilation compilation, IEnumerable<IAssemblySymbol> assemblies)
        {
            var methods = new Dictionary<Compilation, IEnumerable<IMethodSymbol>>();
            foreach (var assembly in assemblies)
            {
                if (assembly.MightContainExtensionMethods)
                {
                    var extensions = (from n in GetAllNamespaceMembers(assembly).Distinct()
                                      from m in GetExtensionMethodPerNamespace(n)
                                      select m).ToList();
                    if (extensions.Count > 0)
                    {
                        IEnumerable<IMethodSymbol> ext;
                        if (methods.TryGetValue(compilation, out ext))
                        {
                            methods[compilation] = ext.Union(extensions);
                        }
                        else
                        {
                            methods[compilation] = extensions;
                        }
                    }
                }
            }
            return methods;
        }

        #endregion

        #region Private
        #region Check Supportability
        private static bool IsSupported(string filePath)
        {
            return IsSupported(filePath, SupportedExtensions, SupportedProjectName);
        }

        private static bool IsSupportedSolution(string filePath)
        {
            return IsSupported(filePath, SupportedSolutionExtensions);
        }

        private static bool IsSupportedProject(string filePath)
        {
            return IsSupported(filePath, SupportedProjectExtensions, SupportedProjectName);
        }

        private static bool IsSupportedSourceFile(string filePath)
        {
            return IsSupported(filePath, SupportedSourceFileExtensions);
        }

        private static bool IsSupportedAssemblyFile(string filePath)
        {
            return IsSupported(filePath, SupportedAssemblyExtensions);
        }

        private static bool IsSupportedVBSourceFile(string filePath)
        {
            return IsSupported(filePath, SupportedVBSourceFileExtensions);
        }

        private static bool IsSupportedCSSourceFile(string filePath)
        {
            return IsSupported(filePath, SupportedCSSourceFileExtensions);
        }

        private static bool IsSupported(string filePath, IEnumerable<string> supportedExtension, params string[] supportedFileName)
        {
            var fileExtension = Path.GetExtension(filePath);
            var fileName = Path.GetFileName(filePath);
            return supportedExtension.Contains(fileExtension, StringComparer.OrdinalIgnoreCase) || supportedFileName.Contains(fileName, StringComparer.OrdinalIgnoreCase);
        }
        #endregion

        private static ExtractMetadataInputModel ValidateInput(ExtractMetadataInputModel input)
        {
            if (input == null) return null;

            if (input.Items == null || input.Items.Count == 0)
            {
                Logger.Log(LogLevel.Warning, "No source project or file to process, exiting...");
                return null;
            }

            var items = new Dictionary<string, List<string>>();

            // 1. Input file should exists
            foreach (var pair in input.Items)
            {
                if (string.IsNullOrWhiteSpace(pair.Key))
                {
                    var value = string.Join(", ", pair.Value);
                    Logger.Log(LogLevel.Warning, $"Empty folder name is found: '{pair.Key}': '{value}'. It is not supported, skipping.");
                    continue;
                }

                // HashSet to guarantee the input file path is unique
                HashSet<string> validFilePath = new HashSet<string>();
                foreach (var inputFilePath in pair.Value)
                {
                    if (!string.IsNullOrEmpty(inputFilePath))
                    {
                        if (File.Exists(inputFilePath))
                        {
                            if (IsSupported(inputFilePath))
                            {
                                var path = StringExtension.ToNormalizedFullPath(inputFilePath);
                                validFilePath.Add(path);
                            }
                            else
                            {
                                var value = string.Join(",", SupportedExtensions);
                                Logger.Log(LogLevel.Warning, $"File {inputFilePath} is not supported, supported file extension are: {value}. The file will be ignored.");
                            }
                        }
                        else
                        {
                            Logger.Log(LogLevel.Warning, $"File {inputFilePath} does not exist, will be ignored.");
                        }
                    }
                }

                if (validFilePath.Count > 0) items.Add(pair.Key, validFilePath.ToList());
            }

            if (items.Count > 0)
            {
                var clone = input.Clone();
                clone.Items = items;
                return clone;
            }
            else return null;
        }

        private async Task SaveAllMembersFromCacheAsync(IEnumerable<string> inputs, string outputFolder, bool forceRebuild)
        {
            var projectCache = new ConcurrentDictionary<string, Project>();
            // Project<=>Documents
            var documentCache = new ProjectDocumentCache();
            var projectDependencyGraph = new ConcurrentDictionary<string, List<string>>();
            DateTime triggeredTime = DateTime.UtcNow;
            var solutions = inputs.Where(s => IsSupportedSolution(s));
            var projects = inputs.Where(s => IsSupportedProject(s));

            var sourceFiles = inputs.Where(s => IsSupportedSourceFile(s));
            var assemblyFiles = inputs.Where(s => IsSupportedAssemblyFile(s));

            // Exclude not supported files from inputs
            inputs = solutions.Concat(projects).Concat(sourceFiles).Concat(assemblyFiles);

            // Add filter config file into inputs and cache
            if (!string.IsNullOrEmpty(_filterConfigFile))
            {
                inputs = inputs.Concat(new string[] { _filterConfigFile });
                documentCache.AddDocument(_filterConfigFile, _filterConfigFile);
            }

            // No matter is incremental or not, we have to load solutions into memory
            await solutions.ForEachInParallelAsync(async path =>
            {
                documentCache.AddDocument(path, path);
                var solution = await GetSolutionAsync(path);
                if (solution != null)
                {
                    foreach (var project in solution.Projects)
                    {
                        var filePath = project.FilePath;

                        // If the project is csproj/vbproj, add to project dictionary, otherwise, ignore
                        if (IsSupportedProject(filePath))
                        {
                            projectCache.GetOrAdd(StringExtension.ToNormalizedFullPath(project.FilePath), s => project);
                        }
                        else
                        {
                            var value = string.Join(",", SupportedExtensions);
                            Logger.Log(LogLevel.Warning, $"Project {filePath} inside solution {path} is not supported, supported file extension are: {value}. The project will be ignored.");
                        }
                    }
                }
            }, 60);

            // Load additional projects out if it is not contained in expanded solution
            projects = projects.Except(projectCache.Keys).Distinct();

            await projects.ForEachInParallelAsync(async path =>
            {
                var project = await GetProjectAsync(path);
                if (project != null)
                {
                    projectCache.GetOrAdd(path, s => project);
                }
            }, 60);

            foreach (var item in projectCache)
            {
                var path = item.Key;
                var project = item.Value;
                documentCache.AddDocument(path, path);
                documentCache.AddDocuments(path, project.Documents.Select(s => s.FilePath));
                documentCache.AddDocuments(path, project.MetadataReferences
                    .Where(s => s is PortableExecutableReference)
                    .Select(s => ((PortableExecutableReference)s).FilePath));
                FillProjectDependencyGraph(projectCache, projectDependencyGraph, project);
                // duplicate project references will fail Project.GetCompilationAsync
                var groups = project.ProjectReferences.GroupBy(r => r);
                if (groups.Any(g => g.Count() > 1))
                {
                    projectCache[path] = project.WithProjectReferences(groups.Select(g => g.Key));
                }
            }

            documentCache.AddDocuments(sourceFiles);

            // Incremental check for inputs as a whole:
            var applicationCache = ApplicationLevelCache.Get(inputs);
            if (!forceRebuild)
            {
                var buildInfo = applicationCache.GetValidConfig(inputs);
                if (buildInfo != null && buildInfo.ShouldSkipMarkup == _shouldSkipMarkup)
                {
                    IncrementalCheck check = new IncrementalCheck(buildInfo);
                    // 1. Check if sln files/ project files and its contained documents/ source files are modified
                    var projectModified = check.AreFilesModified(documentCache.Documents) || check.MSBuildPropertiesUpdated(_msbuildProperties);

                    if (!projectModified)
                    {
                        // 2. Check if documents/ assembly references are changed in a project
                        // e.g. <Compile Include="*.cs* /> and file added/deleted
                        foreach (var project in projectCache.Values)
                        {
                            var key = StringExtension.ToNormalizedFullPath(project.FilePath);
                            IEnumerable<string> currentContainedFiles = documentCache.GetDocuments(project.FilePath);
                            var previousDocumentCache = new ProjectDocumentCache(buildInfo.ContainedFiles);

                            IEnumerable<string> previousContainedFiles = previousDocumentCache.GetDocuments(project.FilePath);
                            if (previousContainedFiles != null && currentContainedFiles != null)
                            {
                                projectModified = !previousContainedFiles.SequenceEqual(currentContainedFiles);
                            }
                            else
                            {
                                // When one of them is not null, project is modified
                                if (!object.Equals(previousContainedFiles, currentContainedFiles))
                                {
                                    projectModified = true;
                                }
                            }
                            if (projectModified) break;
                        }
                    }

                    if (!projectModified)
                    {
                        // Nothing modified, use the result in cache
                        try
                        {
                            CopyFromCachedResult(buildInfo, inputs, outputFolder);
                            return;
                        }
                        catch (Exception e)
                        {
                            Logger.Log(LogLevel.Warning, $"Unable to copy results from cache: {e.Message}. Rebuild starts.");
                        }
                    }
                }
            }

            // Build all the projects to get the output and save to cache
            List<MetadataItem> projectMetadataList = new List<MetadataItem>();
            ConcurrentDictionary<string, bool> projectRebuildInfo = new ConcurrentDictionary<string, bool>();
            ConcurrentDictionary<string, Compilation> compilationCache = await GetProjectCompilationAsync(projectCache);
            var extensionMethods = GetAllExtensionMethodsFromCompilation(compilationCache.Values);

            foreach (var key in GetTopologicalSortedItems(projectDependencyGraph))
            {
                var dependencyRebuilt = projectDependencyGraph[key].Any(r => projectRebuildInfo[r]);
                var projectMetadataResult = await GetProjectMetadataFromCacheAsync(projectCache[key], compilationCache[key], outputFolder, documentCache, forceRebuild, _shouldSkipMarkup, _preserveRawInlineComments, _filterConfigFile, extensionMethods, dependencyRebuilt);
                var projectMetadata = projectMetadataResult.Item1;
                if (projectMetadata != null) projectMetadataList.Add(projectMetadata);
                projectRebuildInfo[key] = projectMetadataResult.Item2;
            }

            var csFiles = sourceFiles.Where(s => IsSupportedCSSourceFile(s));
            if (csFiles.Any())
            {
                var csContent = string.Join(Environment.NewLine, csFiles.Select(s => File.ReadAllText(s)));
                var csCompilation = CompilationUtility.CreateCompilationFromCsharpCode(csContent);
                if (csCompilation != null)
                {
                    var csMetadata = await GetFileMetadataFromCacheAsync(csFiles, csCompilation, outputFolder, forceRebuild, _shouldSkipMarkup, _preserveRawInlineComments, _filterConfigFile, extensionMethods);
                    if (csMetadata != null) projectMetadataList.Add(csMetadata.Item1);
                }
            }

            var vbFiles = sourceFiles.Where(s => IsSupportedVBSourceFile(s));
            if (vbFiles.Any())
            {
                var vbContent = string.Join(Environment.NewLine, vbFiles.Select(s => File.ReadAllText(s)));
                var vbCompilation = CompilationUtility.CreateCompilationFromVBCode(vbContent);
                if (vbCompilation != null)
                {
                    var vbMetadata = await GetFileMetadataFromCacheAsync(vbFiles, vbCompilation, outputFolder, forceRebuild, _preserveRawInlineComments, _shouldSkipMarkup, _filterConfigFile, extensionMethods);
                    if (vbMetadata != null) projectMetadataList.Add(vbMetadata.Item1);
                }
            }

            if (assemblyFiles.Any())
            {
                var assemblyCompilation = CompilationUtility.CreateCompilationFromAssembly(assemblyFiles);
                if (assemblyCompilation != null)
                {
                    var referencedAssemblyList = CompilationUtility.GetAssemblyFromAssemblyComplation(assemblyCompilation);
                    var assemblyExtension = GetAllExtensionMethodsFromAssembly(assemblyCompilation, referencedAssemblyList);
                    var assemblyMetadataValues = (from assembly in referencedAssemblyList
                                                 let metadata = GetAssemblyMetadataFromCacheAsync(assemblyFiles, assemblyCompilation, assembly, outputFolder, forceRebuild, _filterConfigFile, assemblyExtension)
                                                 select metadata.Result.Item1).ToList();
                    var commentFiles = (from file in assemblyFiles
                                        select Path.ChangeExtension(file, SupportedCommentFileExtension) into xmlFile
                                        where File.Exists(xmlFile)
                                        select xmlFile).ToList();

                    MergeCommentsHelper.MergeComments(assemblyMetadataValues, commentFiles);

                    if (assemblyMetadataValues.Count > 0)
                    {
                        projectMetadataList.AddRange(assemblyMetadataValues);
                    }
                }
            }

            var allMemebers = MergeYamlProjectMetadata(projectMetadataList);
            var allReferences = MergeYamlProjectReferences(projectMetadataList);

            if (allMemebers == null || allMemebers.Count == 0)
            {
                var value = StringExtension.ToDelimitedString(projectMetadataList.Select(s => s.Name));
                Logger.Log(LogLevel.Warning, $"No metadata is generated for {value}.");
                applicationCache.SaveToCache(inputs, null, triggeredTime, outputFolder, null, _shouldSkipMarkup, _msbuildProperties);
            }
            else
            {
                // TODO: need an intermediate folder? when to clean it up?
                // Save output to output folder
                var outputFiles = ResolveAndExportYamlMetadata(allMemebers, allReferences, outputFolder, _validInput.IndexFileName, _validInput.TocFileName, _validInput.ApiFolderName, _preserveRawInlineComments, _shouldSkipMarkup, _rawInput.ExternalReferences, _useCompatibilityFileName);
                applicationCache.SaveToCache(inputs, documentCache.Cache, triggeredTime, outputFolder, outputFiles, _shouldSkipMarkup, _msbuildProperties);
            }
        }

        private static void FillProjectDependencyGraph(ConcurrentDictionary<string, Project> projectCache, ConcurrentDictionary<string, List<string>> projectDependencyGraph, Project project)
        {
            projectDependencyGraph.GetOrAdd(StringExtension.ToNormalizedFullPath(project.FilePath), _ => GetTransitiveProjectReferences(projectCache, project).Distinct().ToList());
        }

        private static IEnumerable<string> GetTransitiveProjectReferences(ConcurrentDictionary<string, Project> projectCache, Project project)
        {
            var solution = project.Solution;
            foreach (var pr in project.ProjectReferences)
            {
                var refProject = solution.GetProject(pr.ProjectId);
                var path = StringExtension.ToNormalizedFullPath(refProject.FilePath);
                if (projectCache.ContainsKey(path))
                {
                    yield return path;
                }
                else
                {
                    foreach (var rpr in GetTransitiveProjectReferences(projectCache, refProject))
                    {
                        yield return rpr;
                    }
                }
            }
        }

        private static async Task<ConcurrentDictionary<string, Compilation>> GetProjectCompilationAsync(ConcurrentDictionary<string, Project> projectCache)
        {
            var compilations = new ConcurrentDictionary<string, Compilation>();
            foreach (var project in projectCache)
            {
                try
                {
                    var compilation = await project.Value.GetCompilationAsync();
                    compilations.TryAdd(project.Key, compilation);
                }
                catch (Exception e)
                {
                    throw new ExtractMetadataException($"Error extracting metadata for project \"{project.Key}\": {e.Message}", e);
                }
            }
            return compilations;
        }

        private static IEnumerable<IMethodSymbol> GetExtensionMethodPerNamespace(INamespaceSymbol space)
        {
            var typesWithExtensionMethods = space.GetTypeMembers().Where(t => t.MightContainExtensionMethods);
            foreach (var type in typesWithExtensionMethods)
            {
                var members = type.GetMembers();
                foreach (var member in members)
                {
                    if (member.Kind == SymbolKind.Method)
                    {
                        var method = (IMethodSymbol)member;
                        if (method.IsExtensionMethod)
                        {
                            yield return method;
                        }
                    }
                }
            }
        }

        private static IEnumerable<INamespaceSymbol> GetAllNamespaceMembers(IAssemblySymbol assembly)
        {
            var queue = new Queue<INamespaceSymbol>();
            queue.Enqueue(assembly.GlobalNamespace);
            while (queue.Count > 0)
            {
                var space = queue.Dequeue();
                yield return space;
                var childSpaces = space.GetNamespaceMembers();
                foreach (var child in childSpaces)
                {
                    queue.Enqueue(child);
                }
            }
        }

        private static void CopyFromCachedResult(BuildInfo buildInfo, IEnumerable<string> inputs, string outputFolder)
        {
            var outputFolderSource = buildInfo.OutputFolder;
            var relativeFiles = buildInfo.RelatvieOutputFiles;
            if (relativeFiles == null)
            {
                Logger.Log(LogLevel.Warning, $"No metadata is generated for '{StringExtension.ToDelimitedString(inputs)}'.");
                return;
            }

            Logger.Log(LogLevel.Info, $"'{StringExtension.ToDelimitedString(inputs)}' keep up-to-date since '{buildInfo.TriggeredUtcTime.ToString()}', cached result from '{buildInfo.OutputFolder}' is used.");
            PathUtility.CopyFilesToFolder(relativeFiles.Select(s => Path.Combine(outputFolderSource, s)), outputFolderSource, outputFolder, true, s => Logger.Log(LogLevel.Info, s), null);
        }

        private Task<Tuple<MetadataItem, bool>> GetProjectMetadataFromCacheAsync(Project project, Compilation compilation, string outputFolder, ProjectDocumentCache documentCache, bool forceRebuild, bool shouldSkipMarkup, bool preserveRawInlineComments, string filterConfigFile, IReadOnlyDictionary<Compilation, IEnumerable<IMethodSymbol>> extensionMethods, bool isReferencedProjectRebuilt)
        {
            var projectFilePath = project.FilePath;
            var k = documentCache.GetDocuments(projectFilePath);
            return GetMetadataFromProjectLevelCacheAsync(
                project,
                new[] { projectFilePath, filterConfigFile },
                s => Task.FromResult(forceRebuild || s.AreFilesModified(k.Concat(new string[] { filterConfigFile })) || isReferencedProjectRebuilt || s.MSBuildPropertiesUpdated(_msbuildProperties)),
                s => Task.FromResult(compilation),
                s => Task.FromResult(compilation.Assembly),
                s =>
                {
                    return new Dictionary<string, List<string>> { { StringExtension.ToNormalizedFullPath(s.FilePath), k.ToList() } };
                },
                outputFolder,
                preserveRawInlineComments,
                shouldSkipMarkup,
                filterConfigFile,
                extensionMethods);
        }

        private Task<Tuple<MetadataItem, bool>> GetAssemblyMetadataFromCacheAsync(IEnumerable<string> files, Compilation compilation, IAssemblySymbol assembly, string outputFolder, bool forceRebuild, string filterConfigFile, IReadOnlyDictionary<Compilation, IEnumerable<IMethodSymbol>> extensionMethods)
        {
            if (files == null || !files.Any()) return null;
            return GetMetadataFromProjectLevelCacheAsync(
                files,
                files.Concat(new string[] { filterConfigFile }), s => Task.FromResult(forceRebuild || s.AreFilesModified(files.Concat(new string[] { filterConfigFile }))),
                s => Task.FromResult(compilation),
                s => Task.FromResult(assembly),
                s => null,
                outputFolder,
                false,
                false,
                filterConfigFile,
                extensionMethods);
        }

        private Task<Tuple<MetadataItem, bool>> GetFileMetadataFromCacheAsync(IEnumerable<string> files, Compilation compilation, string outputFolder, bool forceRebuild, bool shouldSkipMarkup, bool preserveRawInlineComments, string filterConfigFile, IReadOnlyDictionary<Compilation, IEnumerable<IMethodSymbol>> extensionMethods)
        {
            if (files == null || !files.Any()) return null;
            return GetMetadataFromProjectLevelCacheAsync(
                files,
                files.Concat(new string[] { filterConfigFile }), s => Task.FromResult(forceRebuild || s.AreFilesModified(files.Concat(new string[] { filterConfigFile })) || s.MSBuildPropertiesUpdated(_msbuildProperties)),
                s => Task.FromResult(compilation),
                s => Task.FromResult(compilation.Assembly),
                s => null,
                outputFolder,
                preserveRawInlineComments,
                shouldSkipMarkup,
                filterConfigFile,
                extensionMethods);
        }

        private async Task<Tuple<MetadataItem, bool>> GetMetadataFromProjectLevelCacheAsync<T>(
            T input,
            IEnumerable<string> inputKey,
            Func<IncrementalCheck, Task<bool>> rebuildChecker,
            Func<T, Task<Compilation>> compilationProvider,
            Func<T, Task<IAssemblySymbol>> assemblyProvider,
            Func<T, IDictionary<string, List<string>>> containedFilesProvider,
            string outputFolder,
            bool preserveRawInlineComments,
            bool shouldSkipMarkup,
            string filterConfigFile,
            IReadOnlyDictionary<Compilation, IEnumerable<IMethodSymbol>> extensionMethods)
        {
            DateTime triggeredTime = DateTime.UtcNow;
            var projectLevelCache = ProjectLevelCache.Get(inputKey);
            var projectConfig = projectLevelCache.GetValidConfig(inputKey);
            var rebuildProject = true;
            if (projectConfig != null)
            {
                var projectCheck = new IncrementalCheck(projectConfig);
                rebuildProject = await rebuildChecker(projectCheck);
            }

            MetadataItem projectMetadata;
            if (!rebuildProject)
            {
                // Load from cache
                var cacheFile = Path.Combine(projectConfig.OutputFolder, projectConfig.RelatvieOutputFiles.First());
                Logger.Log(LogLevel.Info, $"'{projectConfig.InputFilesKey}' keep up-to-date since '{projectConfig.TriggeredUtcTime.ToString()}', cached intermediate result '{cacheFile}' is used.");
                if (TryParseYamlMetadataFile(cacheFile, out projectMetadata))
                {
                    return Tuple.Create(projectMetadata, rebuildProject);
                }
                else
                {
                    Logger.Log(LogLevel.Info, $"'{projectConfig.InputFilesKey}' is invalid, rebuild needed.");
                }
            }

            var compilation = await compilationProvider(input);
            var assembly = await assemblyProvider(input);

            projectMetadata = GenerateYamlMetadata(compilation, assembly, preserveRawInlineComments, filterConfigFile, extensionMethods);
            var file = Path.GetRandomFileName();
            var cacheOutputFolder = projectLevelCache.OutputFolder;
            var path = Path.Combine(cacheOutputFolder, file);
            YamlUtility.Serialize(path, projectMetadata);
            Logger.Log(LogLevel.Verbose, $"Successfully generated metadata {cacheOutputFolder} for {projectMetadata.Name}");

            IDictionary<string, List<string>> containedFiles = null;

            if (containedFilesProvider != null)
            {
                containedFiles = containedFilesProvider(input);
            }

            // Save to cache
            projectLevelCache.SaveToCache(inputKey, containedFiles, triggeredTime, cacheOutputFolder, new List<string>() { file }, shouldSkipMarkup, _msbuildProperties);

            return Tuple.Create(projectMetadata, rebuildProject);
        }

        private static IList<string> ResolveAndExportYamlMetadata(
            Dictionary<string, MetadataItem> allMembers,
            Dictionary<string, ReferenceItem> allReferences,
            string folder,
            string indexFileName,
            string tocFileName,
            string apiFolder,
            bool preserveRawInlineComments,
            bool shouldSkipMarkup,
            IEnumerable<string> externalReferencePackages,
            bool useCompatibilityFileName)
        {
            var outputFiles = new List<string>();
            var model = YamlMetadataResolver.ResolveMetadata(allMembers, allReferences, apiFolder, preserveRawInlineComments, externalReferencePackages);

            // 1. generate toc.yml
            outputFiles.Add(tocFileName);
            model.TocYamlViewModel.Type = MemberType.Toc;

            // TOC do not change
            var tocViewModel = model.TocYamlViewModel.ToTocViewModel();
            string tocFilePath = Path.Combine(folder, tocFileName);

            YamlUtility.Serialize(tocFilePath, tocViewModel, YamlMime.TableOfContent);

            ApiReferenceViewModel indexer = new ApiReferenceViewModel();

            // 2. generate each item's yaml
            var members = model.Members;
            foreach (var memberModel in members)
            {
                var outputPath = memberModel.Name + Constants.YamlExtension;
                if (!useCompatibilityFileName)
                {
                    outputPath = outputPath.Replace('`', '-');
                }
                outputFiles.Add(Path.Combine(apiFolder, outputPath));
                string itemFilePath = Path.Combine(folder, apiFolder, outputPath);
                Directory.CreateDirectory(Path.GetDirectoryName(itemFilePath));
                var memberViewModel = memberModel.ToPageViewModel();
                memberViewModel.ShouldSkipMarkup = shouldSkipMarkup;
                YamlUtility.Serialize(itemFilePath, memberViewModel, YamlMime.ManagedReference);
                Logger.Log(LogLevel.Verbose, $"Metadata file for {memberModel.Name} is saved to {itemFilePath}.");
                AddMemberToIndexer(memberModel, outputPath, indexer);
            }

            // 3. generate manifest file
            outputFiles.Add(indexFileName);
            string indexFilePath = Path.Combine(folder, indexFileName);

            JsonUtility.Serialize(indexFilePath, indexer);

            return outputFiles;
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
                    string path;
                    if (indexer.TryGetValue(member.Name, out path))
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

            Dictionary<string, MetadataItem> namespaceMapping = new Dictionary<string, MetadataItem>();
            Dictionary<string, MetadataItem> allMembers = new Dictionary<string, MetadataItem>();

            foreach (var project in projectMetadataList)
            {
                if (project.Items != null)
                {
                    foreach (var ns in project.Items)
                    {
                        if (ns.Type == MemberType.Namespace)
                        {
                            MetadataItem nsOther;
                            if (namespaceMapping.TryGetValue(ns.Name, out nsOther))
                            {
                                if (ns.Items != null)
                                {
                                    if (nsOther.Items == null)
                                    {
                                        nsOther.Items = new List<MetadataItem>();
                                    }

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
                            MetadataItem existingMetadata;
                            if (allMembers.TryGetValue(s.Name, out existingMetadata))
                            {
                                Logger.Log(LogLevel.Warning, $"Duplicate member {s.Name} is found from {existingMetadata.Source.Path} and {s.Source.Path}, use the one in {existingMetadata.Source.Path} and ignore the one from {s.Source.Path}");
                            }
                            else
                            {
                                allMembers.Add(s.Name, s);
                            }

                            s.Items?.ForEach(s1 =>
                            {
                                MetadataItem existingMetadata1;
                                if (allMembers.TryGetValue(s1.Name, out existingMetadata1))
                                {
                                    Logger.Log(LogLevel.Warning, $"Duplicate member {s1.Name} is found from {existingMetadata1.Source.Path} and {s1.Source.Path}, use the one in {existingMetadata1.Source.Path} and ignore the one from {s1.Source.Path}");
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

        private static bool TryParseYamlMetadataFile(string metadataFileName, out MetadataItem projectMetadata)
        {
            projectMetadata = null;
            try
            {
                using (StreamReader reader = new StreamReader(metadataFileName))
                {
                    projectMetadata = YamlUtility.Deserialize<MetadataItem>(reader);
                    return true;
                }
            }
            catch (Exception e)
            {
                Logger.LogInfo($"Error parsing yaml metadata file: {e.Message}");
                return false;
            }
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

        private async Task<Solution> GetSolutionAsync(string path)
        {
            try
            {
                return await _workspace.Value.OpenSolutionAsync(path);
            }
            catch (Exception e)
            {
                Logger.Log(LogLevel.Warning, $"Error opening solution {path}: {e.Message}. Ignored.");
                return null;
            }
        }

        private async Task<Project> GetProjectAsync(string path)
        {
            try
            {
                string name = Path.GetFileName(path);

                if (name.Equals("project.json", StringComparison.OrdinalIgnoreCase))
                {
                    var workspace = new ProjectJsonWorkspace(path);
                    return workspace.CurrentSolution.Projects.FirstOrDefault(p => p.FilePath == Path.GetFullPath(path));
                }

                return await _workspace.Value.OpenProjectAsync(path);
            }
            catch (Exception e)
            {
                Logger.Log(LogLevel.Warning, $"Error opening project {path}: {e.Message}. Ignored.");
                return null;
            }
        }

        /// <summary>
        /// use DFS to get topological sorted items
        /// </summary>
        private static IEnumerable<string> GetTopologicalSortedItems(IDictionary<string, List<string>> graph)
        {
            var visited = new HashSet<string>();
            var result = new List<string>();
            foreach (var node in graph.Keys)
            {
                DepthFirstTraverse(graph, node, visited, result);
            }
            return result;
        }

        private static void DepthFirstTraverse(IDictionary<string, List<string>> graph, string start, HashSet<string> visited, List<string> result)
        {
            if (!visited.Add(start))
            {
                return;
            }
            foreach (var presequisite in graph[start])
            {
                DepthFirstTraverse(graph, presequisite, visited, result);
            }
            result.Add(start);
        }

        public void Dispose()
        {
            if (_workspace.IsValueCreated)
            {
                _workspace.Value.Dispose();
            }
        }

        #endregion
    }
}
