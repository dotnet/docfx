// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Metadata.ManagedReference
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.MSBuild;
    using Microsoft.DotNet.ProjectModel.Workspaces;

    using Microsoft.DocAsCode.Common;
    using Microsoft.DocAsCode.DataContracts.Common;
    using Microsoft.DocAsCode.DataContracts.ManagedReference;
    using Microsoft.DocAsCode.Exceptions;

    public sealed class ExtractMetadataWorker : IDisposable
    {
        private const string XmlCommentFileExtension = "xml";
        private const string IndexFileName = ".manifest";
        private readonly Dictionary<FileType, List<FileInformation>> _files;
        private readonly bool _rebuild;
        private readonly bool _shouldSkipMarkup;
        private readonly bool _preserveRawInlineComments;
        private readonly bool _useCompatibilityFileName;
        private readonly string _filterConfigFile;
        private readonly string _outputFolder;
        private readonly Dictionary<string, string> _msbuildProperties;

        //Lacks UT for shared workspace
        private readonly Lazy<MSBuildWorkspace> _workspace;

        public ExtractMetadataWorker(ExtractMetadataInputModel input)
        {
            if (input == null)
            {
                throw new ArgumentNullException(nameof(input));
            }

            if (string.IsNullOrEmpty(input.OutputFolder))
            {
                throw new ArgumentNullException(nameof(input.OutputFolder), "Output folder must be specified");
            }

            _files = input.Files?.Select(s => new FileInformation(s))
                .GroupBy(f => f.Type)
                .ToDictionary(s => s.Key, s => s.Distinct().ToList());
            _rebuild = input.ForceRebuild;
            _shouldSkipMarkup = input.ShouldSkipMarkup;
            _preserveRawInlineComments = input.PreserveRawInlineComments;
            _useCompatibilityFileName = input.UseCompatibilityFileName;
            _outputFolder = input.OutputFolder;

            if (input.FilterConfigFile != null)
            {
                _filterConfigFile = new FileInformation(input.FilterConfigFile).NormalizedPath;
            }

            _msbuildProperties = input.MSBuildProperties ?? new Dictionary<string, string>();
            if (!_msbuildProperties.ContainsKey("Configuration"))
            {
                _msbuildProperties["Configuration"] = "Release";
            }

            _workspace = new Lazy<MSBuildWorkspace>(() =>
            {
                var workspace = MSBuildWorkspace.Create(_msbuildProperties);
                workspace.WorkspaceFailed += (s, e) =>
                {
                    Logger.LogWarning($"Workspace failed with: {e.Diagnostic}");
                };
                return workspace;
            });
        }

        public async Task ExtractMetadataAsync()
        {
            if (_files == null || _files.Count == 0)
            {
                Logger.Log(LogLevel.Warning, "No source project or file to process, exiting...");
                return;
            }

            try
            {
                if (_files.TryGetValue(FileType.NotSupported, out List<FileInformation> unsupportedFiles))
                {
                    Logger.LogWarning($"Projects {GetPrintableFileList(unsupportedFiles)} are not supported");
                }
                await SaveAllMembersFromCacheAsync();
            }
            catch (AggregateException e)
            {
                throw new ExtractMetadataException($"Error extracting metadata for {GetPrintableFileList(_files.SelectMany(s => s.Value))}: {e.GetBaseException()?.Message}", e);
            }
            catch (Exception e)
            {
                var files = GetPrintableFileList(_files.SelectMany(s => s.Value));
                throw new ExtractMetadataException($"Error extracting metadata for {files}: {e.Message}", e);
            }
        }

        public void Dispose()
        {
        }

        #region Internal For UT
        internal static MetadataItem GenerateYamlMetadata(Compilation compilation, IAssemblySymbol assembly = null, bool preserveRawInlineComments = false, string filterConfigFile = null, IReadOnlyDictionary<Compilation, IEnumerable<IMethodSymbol>> extensionMethods = null)
        {
            if (compilation == null)
            {
                return null;
            }
            var options = new ExtractMetadataOptions
            {
                PreserveRawInlineComments = preserveRawInlineComments,
                FilterConfigFile = filterConfigFile,
                ExtensionMethods = extensionMethods,
            };

            return new MetadataExtractor(compilation, assembly).Extract(options);
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

        private async Task SaveAllMembersFromCacheAsync()
        {
            var forceRebuild = _rebuild;
            var outputFolder = _outputFolder;

            var projectCache = new ConcurrentDictionary<string, Project>();

            // Project<=>Documents
            var documentCache = new ProjectDocumentCache();
            var projectDependencyGraph = new ConcurrentDictionary<string, List<string>>();
            DateTime triggeredTime = DateTime.UtcNow;

            // Exclude not supported files from inputs
            var cacheKey = GetCacheKey(_files.SelectMany(s => s.Value));

            // Add filter config file into inputs and cache
            if (!string.IsNullOrEmpty(_filterConfigFile))
            {
                cacheKey = cacheKey.Concat(new string[] { _filterConfigFile });
                documentCache.AddDocument(_filterConfigFile, _filterConfigFile);
            }

            if (_files.TryGetValue(FileType.Solution, out var sln))
            {
                var solutions = sln.Select(s => s.NormalizedPath);
                // No matter is incremental or not, we have to load solutions into memory
                foreach (var path in solutions)
                {
                    documentCache.AddDocument(path, path);
                    var solution = await GetSolutionAsync(path);
                    if (solution != null)
                    {
                        foreach (var project in solution.Projects)
                        {
                            var projectFile = new FileInformation(project.FilePath);

                            // If the project is csproj/vbproj, add to project dictionary, otherwise, ignore
                            if (projectFile.IsSupportedProject())
                            {
                                projectCache.GetOrAdd(projectFile.NormalizedPath, s => project);
                            }
                            else
                            {
                                Logger.LogWarning($"Project {projectFile.RawPath} inside solution {path} is ignored, supported projects are csproj and vbproj.");
                            }
                        }
                    }
                }
            }

            if (_files.TryGetValue(FileType.Project, out var p))
            {
                foreach (var pp in p)
                {
                    GetProject(projectCache, pp.NormalizedPath);
                }
            }

            if (_files.TryGetValue(FileType.ProjectJsonProject, out var pjp))
            {
                await pjp.Select(s => s.NormalizedPath).ForEachInParallelAsync(path =>
                {
                    projectCache.GetOrAdd(path, s => GetProjectJsonProject(s));
                    return Task.CompletedTask;
                }, 60);
            }

            foreach (var item in projectCache)
            {
                var path = item.Key;
                var project = item.Value;
                documentCache.AddDocument(path, path);
                if (project.HasDocuments)
                {
                    documentCache.AddDocuments(path, project.Documents.Select(s => s.FilePath));
                }
                else
                {
                    Logger.Log(LogLevel.Warning, $"Project '{project.FilePath}' does not contain any documents.");
                }
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

            var csFiles = new List<string>();
            var vbFiles = new List<string>();
            var assemblyFiles = new List<string>();

            if (_files.TryGetValue(FileType.CSSourceCode, out var cs))
            {
                csFiles.AddRange(cs.Select(s => s.NormalizedPath));
                documentCache.AddDocuments(csFiles);
            }

            if (_files.TryGetValue(FileType.VBSourceCode, out var vb))
            {
                vbFiles.AddRange(vb.Select(s => s.NormalizedPath));
                documentCache.AddDocuments(vbFiles);
            }

            if (_files.TryGetValue(FileType.Assembly, out var asm))
            {
                assemblyFiles.AddRange(asm.Select(s => s.NormalizedPath));
                documentCache.AddDocuments(assemblyFiles);
            }

            // Incremental check for inputs as a whole:
            var applicationCache = ApplicationLevelCache.Get(cacheKey);
            if (!forceRebuild)
            {
                var buildInfo = applicationCache.GetValidConfig(cacheKey);
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
                            CopyFromCachedResult(buildInfo, cacheKey, outputFolder);
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

            if (csFiles.Count > 0)
            {
                var csContent = string.Join(Environment.NewLine, csFiles.Select(s => File.ReadAllText(s)));
                var csCompilation = CompilationUtility.CreateCompilationFromCsharpCode(csContent);
                if (csCompilation != null)
                {
                    var csMetadata = await GetFileMetadataFromCacheAsync(csFiles, csCompilation, outputFolder, forceRebuild, _shouldSkipMarkup, _preserveRawInlineComments, _filterConfigFile, extensionMethods);
                    if (csMetadata != null) projectMetadataList.Add(csMetadata.Item1);
                }
            }

            if (vbFiles.Count > 0)
            {
                var vbContent = string.Join(Environment.NewLine, vbFiles.Select(s => File.ReadAllText(s)));
                var vbCompilation = CompilationUtility.CreateCompilationFromVBCode(vbContent);
                if (vbCompilation != null)
                {
                    var vbMetadata = await GetFileMetadataFromCacheAsync(vbFiles, vbCompilation, outputFolder, forceRebuild, _preserveRawInlineComments, _shouldSkipMarkup, _filterConfigFile, extensionMethods);
                    if (vbMetadata != null) projectMetadataList.Add(vbMetadata.Item1);
                }
            }

            if (assemblyFiles.Count > 0)
            {
                var assemblyCompilation = CompilationUtility.CreateCompilationFromAssembly(assemblyFiles);
                if (assemblyCompilation != null)
                {
                    var commentFiles = (from file in assemblyFiles
                                        select Path.ChangeExtension(file, XmlCommentFileExtension) into xmlFile
                                        where File.Exists(xmlFile)
                                        select xmlFile).ToList();

                    var referencedAssemblyList = CompilationUtility.GetAssemblyFromAssemblyComplation(assemblyCompilation);
                    var assemblyExtension = GetAllExtensionMethodsFromAssembly(assemblyCompilation, referencedAssemblyList);

                    foreach (var assembly in referencedAssemblyList)
                    {
                        var mta = await GetAssemblyMetadataFromCacheAsync(assemblyFiles, assemblyCompilation, assembly, outputFolder, forceRebuild, _filterConfigFile, assemblyExtension);
                        if (mta != null)
                        {
                            MergeCommentsHelper.MergeComments(mta.Item1, commentFiles);
                            projectMetadataList.Add(mta.Item1);
                        }
                    }
                }
            }

            var allMemebers = MergeYamlProjectMetadata(projectMetadataList);
            var allReferences = MergeYamlProjectReferences(projectMetadataList);

            if (allMemebers == null || allMemebers.Count == 0)
            {
                var value = StringExtension.ToDelimitedString(projectMetadataList.Select(s => s.Name));
                Logger.Log(LogLevel.Warning, $"No metadata is generated for {value}.");
                applicationCache.SaveToCache(cacheKey, null, triggeredTime, outputFolder, null, _shouldSkipMarkup, _msbuildProperties);
            }
            else
            {
                // TODO: need an intermediate folder? when to clean it up?
                // Save output to output folder
                var outputFiles = ResolveAndExportYamlMetadata(allMemebers, allReferences, outputFolder, _preserveRawInlineComments, _shouldSkipMarkup, _useCompatibilityFileName).ToList();
                applicationCache.SaveToCache(cacheKey, documentCache.Cache, triggeredTime, outputFolder, outputFiles, _shouldSkipMarkup, _msbuildProperties);
            }
        }

        private static void FillProjectDependencyGraph(ConcurrentDictionary<string, Project> projectCache, ConcurrentDictionary<string, List<string>> projectDependencyGraph, Project project)
        {
            projectDependencyGraph.GetOrAdd(project.FilePath.ToNormalizedFullPath(), _ => GetTransitiveProjectReferences(projectCache, project).Distinct().ToList());
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
            var sb = new StringBuilder();
            foreach (var project in projectCache)
            {
                try
                {
                    var compilation = await project.Value.GetCompilationAsync();
                    compilations.TryAdd(project.Key, compilation);
                }
                catch (Exception e)
                {
                    if (sb.Length > 0)
                    {
                        sb.AppendLine();
                    }
                    sb.Append($"Error extracting metadata for project \"{project.Key}\": {e.Message}");
                }
            }
            if (sb.Length > 0)
            {
                throw new ExtractMetadataException(sb.ToString());
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

        private static IEnumerable<string> ResolveAndExportYamlMetadata(
            Dictionary<string, MetadataItem> allMembers,
            Dictionary<string, ReferenceItem> allReferences,
            string folder,
            bool preserveRawInlineComments,
            bool shouldSkipMarkup,
            bool useCompatibilityFileName)
        {
            var outputFileNames = new Dictionary<string, int>(FilePathComparer.OSPlatformSensitiveStringComparer);
            var model = YamlMetadataResolver.ResolveMetadata(allMembers, allReferences, preserveRawInlineComments);

            var tocFileName = Constants.TocYamlFileName;
            // 0. load last Manifest and remove files
            CleanupHistoricalFile(folder);

            // 1. generate toc.yml
            model.TocYamlViewModel.Type = MemberType.Toc;

            // TOC do not change
            var tocViewModel = model.TocYamlViewModel.ToTocViewModel();
            string tocFilePath = Path.Combine(folder, tocFileName);

            YamlUtility.Serialize(tocFilePath, tocViewModel, YamlMime.TableOfContent);
            outputFileNames.Add(tocFilePath, 1);
            yield return tocFileName;

            ApiReferenceViewModel indexer = new ApiReferenceViewModel();

            // 2. generate each item's yaml
            var members = model.Members;
            foreach (var memberModel in members)
            {
                var fileName = useCompatibilityFileName ? memberModel.Name : memberModel.Name.Replace('`', '-');
                var outputFileName = GetUniqueFileNameWithSuffix(fileName + Constants.YamlExtension, outputFileNames);
                string itemFilePath = Path.Combine(folder, outputFileName);
                Directory.CreateDirectory(Path.GetDirectoryName(itemFilePath));
                var memberViewModel = memberModel.ToPageViewModel();
                memberViewModel.ShouldSkipMarkup = shouldSkipMarkup;
                YamlUtility.Serialize(itemFilePath, memberViewModel, YamlMime.ManagedReference);
                Logger.Log(LogLevel.Diagnostic, $"Metadata file for {memberModel.Name} is saved to {itemFilePath}.");
                AddMemberToIndexer(memberModel, outputFileName, indexer);
                yield return outputFileName;
            }

            // 3. generate manifest file
            string indexFilePath = Path.Combine(folder, IndexFileName);

            JsonUtility.Serialize(indexFilePath, indexer);
            yield return IndexFileName;
        }

        private static void CleanupHistoricalFile(string outputFolder)
        {
            var indexFilePath = Path.Combine(outputFolder, IndexFileName);
            ApiReferenceViewModel index;
            if (!File.Exists(indexFilePath))
            {
                return;
            }
            try
            {
                index = YamlUtility.Deserialize<ApiReferenceViewModel>(indexFilePath);
            }
            catch (Exception e)
            {
                Logger.LogInfo($"{indexFilePath} is not in a valid metadata manifest file format, ignored: {e.Message}.");
                return;
            }

            foreach (var pair in index)
            {
                var filePath = Path.Combine(outputFolder, pair.Value);
                try
                {
                    File.Delete(filePath);
                }
                catch (Exception e)
                {
                    Logger.LogDiagnostic($"Error deleting file {filePath}: {e.Message}");
                }
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
                var extension = Path.GetExtension(fileName);
                var name = Path.GetFileNameWithoutExtension(fileName);
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
                Logger.LogVerbose($"Loading solution {path}", file: path);
                var solution = await _workspace.Value.OpenSolutionAsync(path);
                _workspace.Value.CloseSolution();
                return solution;
            }
            catch (Exception e)
            {
                Logger.Log(LogLevel.Warning, $"Error opening solution {path}: {e.Message}. Ignored.");
                return null;
            }
        }

        private Project GetProject(ConcurrentDictionary<string, Project> cache, string path)
        {
            return cache.GetOrAdd(path.ToNormalizedFullPath(), s =>
            {
                try
                {
                    Logger.LogVerbose($"Loading project {s}", file: s);
                    var project = _workspace.Value.OpenProjectAsync(s).Result;
                    foreach (var p in _workspace.Value.CurrentSolution.Projects)
                    {
                        cache.TryAdd(p.FilePath.ToNormalizedFullPath(), p);
                    }
                    return project;
                }
                catch (Exception e)
                {
                    Logger.Log(LogLevel.Warning, $"Error opening project {path}: {e.Message}. Ignored.");
                    return null;
                }
            });
        }

        private Project GetProjectJsonProject(string path)
        {
            try
            {
                Logger.LogVerbose($"Loading project {path}", file: path);
                var workspace = new ProjectJsonWorkspace(path);
                return workspace.CurrentSolution.Projects.FirstOrDefault(p => p.FilePath == Path.GetFullPath(path));
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

        private static string GetPrintableFileList(IEnumerable<FileInformation> files)
        {
            return files?.Select(s => s.RawPath).ToDelimitedString();
        }

        private static IEnumerable<string> GetCacheKey(IEnumerable<FileInformation> files)
        {
            return files.Where(s => s.Type != FileType.NotSupported).OrderBy(s => s.Type).Select(s => s.NormalizedPath);
        }

        #endregion
    }
}
