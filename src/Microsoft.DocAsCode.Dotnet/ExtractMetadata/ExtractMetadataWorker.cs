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

            var outputFolder = _outputFolder;

            var projectCache = new ConcurrentDictionary<string, Project>();

            if (_files.TryGetValue(FileType.Solution, out var sln))
            {
                // No matter is incremental or not, we have to load solutions into memory
                foreach (var path in sln.Select(s => s.NormalizedPath))
                {
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

            var csFiles = new List<string>();
            var vbFiles = new List<string>();
            var assemblyFiles = new List<string>();

            if (_files.TryGetValue(FileType.CSSourceCode, out var cs))
            {
                csFiles.AddRange(cs.Select(s => s.NormalizedPath));
            }

            if (_files.TryGetValue(FileType.VBSourceCode, out var vb))
            {
                vbFiles.AddRange(vb.Select(s => s.NormalizedPath));
            }

            if (_files.TryGetValue(FileType.Assembly, out var asm))
            {
                assemblyFiles.AddRange(asm.Select(s => s.NormalizedPath));
            }

            var options = _options;

            // Build all the projects to get the output and save to cache
            List<MetadataItem> projectMetadataList = new List<MetadataItem>();
            ConcurrentDictionary<string, bool> projectRebuildInfo = new ConcurrentDictionary<string, bool>();
            ConcurrentDictionary<string, Compilation> compilationCache = await GetProjectCompilationAsync(projectCache);
            var roslynProjects = compilationCache.Values;
            options.RoslynExtensionMethods = RoslynIntermediateMetadataExtractor.GetAllExtensionMethodsFromCompilation(roslynProjects);
            foreach (var key in projectCache.Keys)
            {
                var projectMetadata = GetMetadataFromProjectLevelCache(compilationCache[key], null);
                if (projectMetadata != null) projectMetadataList.Add(projectMetadata);
            }

            if (csFiles.Count > 0)
            {
                var compilation = CompilationUtility.CreateCompilationFromCSharpFiles(csFiles);
                var metadata = GetMetadataFromProjectLevelCache(compilation, null);
                if (metadata != null) projectMetadataList.Add(metadata);
            }

            if (vbFiles.Count > 0)
            {
                var compilation = CompilationUtility.CreateCompilationFromVBFiles(vbFiles);
                var metadata = GetMetadataFromProjectLevelCache(compilation, null);
                if (metadata != null) projectMetadataList.Add(metadata);
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
                        var mta = GetMetadataFromProjectLevelCache(assemblyCompilation, assembly);
                        if (mta != null)
                        {
                            projectMetadataList.Add(mta);
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

        private MetadataItem GetMetadataFromProjectLevelCache(Compilation compilation, IAssemblySymbol assembly)
        {
            return RoslynIntermediateMetadataExtractor.GenerateYamlMetadata(compilation, assembly, _options);
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
    }
}
