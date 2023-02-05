// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Metadata.ManagedReference
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;

    using Microsoft.Build.Construction;
    using Microsoft.Build.Framework;
    using Microsoft.Build.Logging;

    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.MSBuild;

    using Microsoft.DocAsCode.Common;
    using Microsoft.DocAsCode.DataContracts.Common;
    using Microsoft.DocAsCode.DataContracts.ManagedReference;
    using Microsoft.DocAsCode.Plugins;

    public sealed class ExtractMetadataWorker : IDisposable
    {
        private readonly Dictionary<FileType, List<FileInformation>> _files;
        private readonly List<string> _references;
        private readonly bool _rebuild;
        private readonly bool _useCompatibilityFileName;
        private readonly string _outputFolder;
        private readonly ExtractMetadataOptions _options;
        private readonly ConsoleLogger _msbuildLogger;

        //Lacks UT for shared workspace
        private readonly MSBuildWorkspace _workspace;

        internal const string IndexFileName = ".manifest";

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
            _references = input.References?.Select(s => new FileInformation(s))
                .Select(f => f.NormalizedPath)
                .Distinct()
                .ToList();
            _rebuild = input.ForceRebuild;

            var msbuildProperties = input.MSBuildProperties ?? new Dictionary<string, string>();
            if (!msbuildProperties.ContainsKey("Configuration"))
            {
                msbuildProperties["Configuration"] = "Release";
            }

            _options = new ExtractMetadataOptions
            {
                ShouldSkipMarkup = input.ShouldSkipMarkup,
                PreserveRawInlineComments = input.PreserveRawInlineComments,
                FilterConfigFile = input.FilterConfigFile != null ? new FileInformation(input.FilterConfigFile).NormalizedPath : null,
                MSBuildProperties = msbuildProperties,
                CodeSourceBasePath = input.CodeSourceBasePath,
                DisableDefaultFilter = input.DisableDefaultFilter,
                TocNamespaceStyle = input.TocNamespaceStyle
            };

            _useCompatibilityFileName = input.UseCompatibilityFileName;
            _outputFolder = StringExtension.ToNormalizedFullPath(Path.Combine(EnvironmentContext.OutputDirectory, input.OutputFolder));

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

            var forceRebuild = _rebuild;
            var outputFolder = _outputFolder;

            var projectCache = new ConcurrentDictionary<string, Project>();

            // Project<=>Documents
            var documentCache = new ProjectDocumentCache();
            var projectDependencyGraph = new ConcurrentDictionary<string, List<string>>();
            DateTime triggeredTime = DateTime.UtcNow;

            // Exclude not supported files from inputs
            var cacheKey = GetCacheKey(_files.SelectMany(s => s.Value));

            if (_files.TryGetValue(FileType.Solution, out var sln))
            {
                // No matter is incremental or not, we have to load solutions into memory
                foreach (var path in sln.Select(s => s.NormalizedPath))
                {
                    documentCache.AddDocument(path, path);
                    foreach (var project in SolutionFile.Parse(path).ProjectsInOrder)
                    {
                        if (project.ProjectType is not SolutionProjectType.KnownToBeMSBuildFormat)
                            continue;

                        var projectFile = new FileInformation(project.AbsolutePath);
                        if (projectFile.Type is not FileType.Project)
                        {
                            Logger.LogInfo($"Skip unsupported project {project.AbsolutePath}.");
                            continue;
                        }

                        projectCache.GetOrAdd(projectFile.NormalizedPath, LoadProject);
                    }
                }
            }

            if (_files.TryGetValue(FileType.Project, out var p))
            {
                foreach (var pp in p)
                {
                    projectCache.GetOrAdd(pp.NormalizedPath, LoadProject);
                }
            }

            foreach (var item in projectCache)
            {
                var path = item.Key;
                var project = item.Value;
                documentCache.AddDocument(path, path);
                documentCache.AddDocuments(path, project.Documents.Select(s => s.FilePath));
                documentCache.AddDocuments(path, project.MetadataReferences.OfType<PortableExecutableReference>().Select(r => r.FilePath));
                FillProjectDependencyGraph(projectCache, projectDependencyGraph, project);
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

            var options = _options;

            if (!forceRebuild)
            {
                var buildInfo = applicationCache.GetValidConfig(cacheKey);
                if (buildInfo != null)
                {
                    IncrementalCheck check = new IncrementalCheck(buildInfo);
                    if (!options.HasChanged(check, true))
                    {
                        // 1. Check if sln files/ project files and its contained documents/ source files are modified
                        var projectModified = check.AreFilesModified(documentCache.Documents);

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
            }

            // Build all the projects to get the output and save to cache
            List<MetadataItem> projectMetadataList = new List<MetadataItem>();
            ConcurrentDictionary<string, bool> projectRebuildInfo = new ConcurrentDictionary<string, bool>();
            ConcurrentDictionary<string, Compilation> compilationCache = await GetProjectCompilationAsync(projectCache);
            var roslynProjects = compilationCache.Values;
            options.RoslynExtensionMethods = RoslynIntermediateMetadataExtractor.GetAllExtensionMethodsFromCompilation(roslynProjects);
            foreach (var key in GetTopologicalSortedItems(projectDependencyGraph))
            {
                var dependencyRebuilt = projectDependencyGraph[key].Any(r => projectRebuildInfo[r]);
                var k = documentCache.GetDocuments(key);
                var input = new ProjectFileInputParameters(options, k, key, dependencyRebuilt);
                var projectMetadataResult = GetMetadataFromProjectLevelCache(compilationCache[key], null, input);
                var projectMetadata = projectMetadataResult.Item1;
                if (projectMetadata != null) projectMetadataList.Add(projectMetadata);
                projectRebuildInfo[key] = projectMetadataResult.Item2;
            }

            if (csFiles.Count > 0)
            {
                var csContent = string.Join(Environment.NewLine, csFiles.Select(File.ReadAllText));
                var csCompilation = CompilationUtility.CreateCompilationFromCsharpCode(csContent);
                var input = new SourceFileInputParameters(options, csFiles);
                var csMetadata = GetMetadataFromProjectLevelCache(csCompilation, null, input);
                if (csMetadata != null) projectMetadataList.Add(csMetadata.Item1);
            }

            if (vbFiles.Count > 0)
            {
                var vbContent = string.Join(Environment.NewLine, vbFiles.Select(File.ReadAllText));
                var vbCompilation = CompilationUtility.CreateCompilationFromVBCode(vbContent);
                var input = new SourceFileInputParameters(options, vbFiles);
                var vbMetadata = GetMetadataFromProjectLevelCache(vbCompilation, null, input);
                if (vbMetadata != null) projectMetadataList.Add(vbMetadata.Item1);
            }

            if (assemblyFiles.Count > 0)
            {
                Logger.LogInfo($"Processing assembly files");
                var assemblyCompilation = CompilationUtility.CreateCompilationFromAssembly(assemblyFiles, _references);
                if (assemblyCompilation != null)
                {
                    var referencedAssemblyList = CompilationUtility.GetAssemblyFromAssemblyComplation(assemblyCompilation, assemblyFiles).ToList();
                    // TODO: why not merge with compilation's extension methods?
                    var assemblyExtension = RoslynIntermediateMetadataExtractor.GetAllExtensionMethodsFromAssembly(assemblyCompilation, referencedAssemblyList.Select(s => s.assembly));
                    options.RoslynExtensionMethods = assemblyExtension;
                    foreach (var (reference, assembly) in referencedAssemblyList)
                    {
                        var input = new AssemblyFileInputParameters(options, reference.Display);
                        var mta = GetMetadataFromProjectLevelCache(assemblyCompilation, assembly, input);
                        if (mta != null)
                        {
                            projectMetadataList.Add(mta.Item1);
                        }
                    }
                }
            }

            if (projectMetadataList.Count <= 0)
            {
                Logger.LogWarning("No .NET API project detected.");
                return;
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
                applicationCache.SaveToCache(cacheKey, null, triggeredTime, outputFolder, null, options);
            }
            else
            {
                // TODO: need an intermediate folder? when to clean it up?
                // Save output to output folder
                List<string> outputFiles;
                using (new PerformanceScope("ResolveAndExport"))
                {
                    outputFiles = ResolveAndExportYamlMetadata(allMembers, allReferences, outputFolder, options.PreserveRawInlineComments, options.ShouldSkipMarkup, _useCompatibilityFileName, options.TocNamespaceStyle).ToList();
                }

                applicationCache.SaveToCache(cacheKey, documentCache.Cache, triggeredTime, outputFolder, outputFiles, options);
            }
        }

        public Project LoadProject(string path)
        {
            var project = _workspace.CurrentSolution.Projects.FirstOrDefault(
                p => FilePathComparer.OSPlatformSensitiveRelativePathComparer.Equals(p.FilePath, path));
            if (project is null)
            {
                Logger.LogInfo($"Loading project {path}");
                project = _workspace.OpenProjectAsync(path, _msbuildLogger).Result;
            }
            return project;
        }

        private static void FillProjectDependencyGraph(ConcurrentDictionary<string, Project> projectCache, ConcurrentDictionary<string, List<string>> projectDependencyGraph, Project project)
        {
            projectDependencyGraph.GetOrAdd(project.FilePath.ToNormalizedFullPath(), _ => GetTransitiveProjectReferences(projectCache, project).Distinct().ToList());
        }

        private static IEnumerable<string> GetTransitiveProjectReferences(ConcurrentDictionary<string, Project> projectCache, Project project)
        {
            foreach (var pr in project.ProjectReferences)
            {
                var projectReference = project.Solution.GetProject(pr.ProjectId);
                var path = StringExtension.ToNormalizedFullPath(projectReference.FilePath);
                if (projectCache.ContainsKey(path))
                {
                    yield return path;
                }
                else
                {
                    foreach (var rpr in GetTransitiveProjectReferences(projectCache, projectReference))
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
                Logger.LogInfo($"Building project {project.Key}");
                var compilation = await project.Value.GetCompilationAsync();
                LogDeclarationDiagnostics(compilation);
                compilations.TryAdd(project.Key, compilation);
            }
            return compilations;
        }

        private static void LogDeclarationDiagnostics(Compilation compilation)
        {
            foreach (var diagnostic in compilation.GetDeclarationDiagnostics())
            {
                if (diagnostic.IsSuppressed || IsKnownError(diagnostic))
                    continue;

                var level = diagnostic.Severity switch
                {
                    DiagnosticSeverity.Error => LogLevel.Error,
                    DiagnosticSeverity.Warning => LogLevel.Warning,
                    DiagnosticSeverity.Info => LogLevel.Info,
                    _ => LogLevel.Verbose,
                };

                Logger.Log(level, $"{diagnostic}");
            }

            static bool IsKnownError(Diagnostic diagnostic)
            {
                // Ignore these VB errors on non-Windows platform:
                //   error BC30002: Type 'Global.Microsoft.VisualBasic.Devices.Computer' is not defined.
                //   error BC30002: Type 'Global.Microsoft.VisualBasic.ApplicationServices.ApplicationBase' is not defined.
                //   error BC30002: Type 'Global.Microsoft.VisualBasic.MyServices.Internal.ContextValue' is not defined.
                //   error BC30002: Type 'Global.Microsoft.VisualBasic.ApplicationServices.User' is not defined.
                //   error BC30002: Type 'Global.Microsoft.VisualBasic.ApplicationServices.User' is not defined.
                if (!OperatingSystem.IsWindows() && diagnostic.Id == "BC30002" &&
                    diagnostic.GetMessage().Contains("Global.Microsoft.VisualBasic."))
                {
                    return true;
                }

                return false;
            }
        }

        private static void CopyFromCachedResult(BuildInfo buildInfo, IEnumerable<string> inputs, string outputFolder)
        {
            var outputFolderSource = buildInfo.OutputFolder;
            var relativeFiles = buildInfo.RelativeOutputFiles;
            if (relativeFiles == null)
            {
                Logger.Log(LogLevel.Warning, $"No metadata is generated for '{StringExtension.ToDelimitedString(inputs)}'.");
                return;
            }

            Logger.Log(LogLevel.Info, $"'{StringExtension.ToDelimitedString(inputs)}' keep up-to-date since '{buildInfo.TriggeredUtcTime.ToString()}', cached result from '{buildInfo.OutputFolder}' is used.");
            PathUtility.CopyFilesToFolder(relativeFiles.Select(s => Path.Combine(outputFolderSource, s)), outputFolderSource, outputFolder, true, s => Logger.Log(LogLevel.Info, s), null);
        }

        private Tuple<MetadataItem, bool> GetMetadataFromProjectLevelCache(Compilation compilation, IAssemblySymbol assembly, IInputParameters key)
        {
            DateTime triggeredTime = DateTime.UtcNow;
            var projectLevelCache = key.Cache;
            var projectConfig = key.BuildInfo;
            var rebuildProject = _rebuild || projectConfig == null || key.HasChanged(projectConfig);

            MetadataItem projectMetadata;
            if (!rebuildProject)
            {
                // Load from cache
                var cacheFile = Path.Combine(projectConfig.OutputFolder, projectConfig.RelativeOutputFiles.First());
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

            projectMetadata = RoslynIntermediateMetadataExtractor.GenerateYamlMetadata(compilation, assembly, key.Options);
            var file = Path.GetRandomFileName();
            var cacheOutputFolder = projectLevelCache.OutputFolder;
            var path = Path.Combine(cacheOutputFolder, file);
            YamlUtility.Serialize(path, projectMetadata);
            Logger.Log(LogLevel.Verbose, $"Successfully generated metadata {cacheOutputFolder} for {projectMetadata.Name}");

            // Save to cache
            projectLevelCache.SaveToCache(key.Key, key.Files, triggeredTime, cacheOutputFolder, new List<string> { file }, key.Options);

            return Tuple.Create(projectMetadata, rebuildProject);
        }

        private static IEnumerable<string> ResolveAndExportYamlMetadata(
            Dictionary<string, MetadataItem> allMembers,
            Dictionary<string, ReferenceItem> allReferences,
            string folder,
            bool preserveRawInlineComments,
            bool shouldSkipMarkup,
            bool useCompatibilityFileName,
            TocNamespaceStyle tocNamespaceStyle)
        {
            var outputFileNames = new Dictionary<string, int>(FilePathComparer.OSPlatformSensitiveStringComparer);
            var model = YamlMetadataResolver.ResolveMetadata(allMembers, allReferences, preserveRawInlineComments, tocNamespaceStyle);

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
                var memberViewModel = memberModel.ToPageViewModel();
                memberViewModel.ShouldSkipMarkup = shouldSkipMarkup;
                YamlUtility.Serialize(itemFilePath, memberViewModel, YamlMime.ManagedReference);
                Logger.Log(LogLevel.Diagnostic, $"Metadata file for {memberModel.Name} is saved to {itemFilePath}.");
                AddMemberToIndexer(memberModel, outputFileName, indexer);
                yield return outputFileName;
            }

            // 3. generate manifest file
            string indexFilePath = Path.Combine(folder, IndexFileName);

            JsonUtility.Serialize(indexFilePath, indexer, Newtonsoft.Json.Formatting.Indented);
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
                index = JsonUtility.Deserialize<ApiReferenceViewModel>(indexFilePath);
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
                            if (namespaceMapping.TryGetValue(ns.Name, out MetadataItem nsOther))
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
                            if (allMembers.TryGetValue(s.Name, out MetadataItem existingMetadata))
                            {
                                Logger.Log(LogLevel.Warning, $"Duplicate member {s.Name} is found from {existingMetadata.Source.Path} and {s.Source.Path}, use the one in {existingMetadata.Source.Path} and ignore the one from {s.Source.Path}");
                            }
                            else
                            {
                                allMembers.Add(s.Name, s);
                            }

                            s.Items?.ForEach(s1 =>
                            {
                                if (allMembers.TryGetValue(s1.Name, out MetadataItem existingMetadata1))
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
                using StreamReader reader = new StreamReader(metadataFileName);
                projectMetadata = YamlUtility.Deserialize<MetadataItem>(reader);
                return true;
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

        private static IEnumerable<string> GetCacheKey(IEnumerable<FileInformation> files)
        {
            return files.Where(s => s.Type != FileType.NotSupported).OrderBy(s => s.Type).Select(s => s.NormalizedPath);
        }
    }
}
