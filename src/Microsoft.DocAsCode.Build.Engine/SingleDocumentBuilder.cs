// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.Engine
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Composition.Hosting;
    using System.Collections.Immutable;
    using System.Text;

    using Microsoft.DocAsCode.Build.Engine.Incrementals;
    using Microsoft.DocAsCode.Common;
    using Microsoft.DocAsCode.Exceptions;
    using Microsoft.DocAsCode.Plugins;
    using Microsoft.DocAsCode.Utility;

    using Newtonsoft.Json;

    public class SingleDocumentBuilder : IDisposable
    {
        private const string PhaseName = "Build Document";
        private const string XRefMapFileName = "xrefmap.yml";

        public CompositionHost Container { get; set; }
        public IEnumerable<IDocumentProcessor> Processors { get; set; }
        public IEnumerable<IInputMetadataValidator> MetadataValidators { get; set; }

        internal BuildInfo CurrentBuildInfo { get; set; }
        internal BuildInfo LastBuildInfo { get; set; }
        internal string IntermediateFolder { get; set; }

        private bool ShouldTraceIncrementalInfo => IntermediateFolder != null;
        private bool _canIncremental;

        public Manifest Build(DocumentBuildParameters parameters)
        {
            if (parameters == null)
            {
                throw new ArgumentNullException(nameof(parameters));
            }
            if (parameters.OutputBaseDir == null)
            {
                throw new ArgumentException("Output folder cannot be null.", nameof(parameters) + "." + nameof(parameters.OutputBaseDir));
            }
            if (parameters.Files == null)
            {
                throw new ArgumentException("Source files cannot be null.", nameof(parameters) + "." + nameof(parameters.Files));
            }
            if (parameters.MaxParallelism <= 0)
            {
                parameters.MaxParallelism = Environment.ProcessorCount;
            }
            if (parameters.Metadata == null)
            {
                parameters.Metadata = ImmutableDictionary<string, object>.Empty;
            }

            return BuildCore(parameters);
        }

        private Manifest BuildCore(DocumentBuildParameters parameters)
        {
            using (new LoggerPhaseScope(PhaseName, true))
            {
                _canIncremental = false;
                Logger.LogInfo($"Max parallelism is {parameters.MaxParallelism}.");
                Directory.CreateDirectory(parameters.OutputBaseDir);
                var context = new DocumentBuildContext(
                    Path.Combine(Directory.GetCurrentDirectory(), parameters.OutputBaseDir),
                    parameters.Files.EnumerateFiles(),
                    parameters.ExternalReferencePackages,
                    parameters.XRefMaps,
                    parameters.MaxParallelism,
                    parameters.Files.DefaultBaseDir);
                if (ShouldTraceIncrementalInfo)
                {
                    string configHash = ComputeConfigHash(parameters);
                    var fileAttributes = ComputeFileAttributes(parameters);
                    CurrentBuildInfo.Versions.Add(new BuildVersionInfo
                    {
                        VersionName = parameters.VersionName,
                        ConfigHash = configHash,
                        AttributesFile = "attributes",
                        DependencyFile = "dependency",
                        ManifestFile = "manifest",
                        XRefSpecMapFile = "xrefspecmap",
                        BuildModelManifestFile = "buildmodelmanifest",
                        PostBuildModelManifestFile = "postbuildmodelmanifest",
                        Attributes = fileAttributes,
                        Dependency = context.DependencyGraph,
                        Manifest = context.ManifestItems,
                        XRefSpecMap = context.XRefSpecMap,
                        BuildModelManifest = new ModelManifest { BaseDir = CreateRandomDir(IntermediateFolder) },
                        PostBuildModelManifest = new ModelManifest { BaseDir = CreateRandomDir(IntermediateFolder) },
                    });
                    _canIncremental = GetCanIncremental(configHash, parameters.VersionName);
                    if (_canIncremental)
                    {
                        LoadChanges(parameters, context, fileAttributes);
                        var dependencyGraph = LastBuildInfo.Versions.Single(v => v.VersionName == parameters.VersionName).Dependency;
                        ExpandDependency(dependencyGraph, context, d => dependencyGraph.DependencyTypes[d.Type].TriggerBuild);
                    }
                }

                Logger.LogVerbose("Start building document...");

                // Start building document...
                List<HostService> hostServices = null;
                try
                {
                    using (var processor = parameters.TemplateManager?.GetTemplateProcessor(context, parameters.MaxParallelism) ?? TemplateProcessor.DefaultProcessor)
                    {
                        IMarkdownService markdownService;
                        using (new LoggerPhaseScope("CreateMarkdownService", true))
                        {
                            markdownService = CreateMarkdownService(parameters, processor.Tokens.ToImmutableDictionary());
                        }

                        using (new LoggerPhaseScope("Load", true))
                        {
                            hostServices = GetInnerContexts(parameters, Processors, processor, markdownService, context).ToList();
                        }

                        var manifest = new List<ManifestItemWithContext>(BuildCore(hostServices, context, parameters.VersionName));

                        // Use manifest from now on
                        using (new LoggerPhaseScope("UpdateContext", true))
                        {
                            UpdateContext(context);
                        }

                        // Run getOptions from Template
                        using (new LoggerPhaseScope("FeedOptions", true))
                        {
                            FeedOptions(manifest, context);
                        }

                        // Template can feed back xref map, actually, the anchor # location can only be determined in template
                        using (new LoggerPhaseScope("FeedXRefMap", true))
                        {
                            FeedXRefMap(manifest, context);
                        }

                        using (new LoggerPhaseScope("UpdateHref", true))
                        {
                            UpdateHref(manifest, context);
                        }

                        // Afterwards, m.Item.Model.Content is always IDictionary
                        using (new LoggerPhaseScope("ApplySystemMetadata", true))
                        {
                            ApplySystemMetadata(manifest, context);
                        }

                        // Register global variables after href are all updated
                        IDictionary<string, object> globalVariables;
                        using (new LoggerPhaseScope("FeedGlobalVariables", true))
                        {
                            globalVariables = FeedGlobalVariables(processor.Tokens, manifest, context);
                        }

                        // processor to add global variable to the model
                        foreach (var m in processor.Process(manifest.Select(s => s.Item).ToList(), context, parameters.ApplyTemplateSettings, globalVariables))
                        {
                            context.ManifestItems.Add(m);
                        }
                        return new Manifest
                        {
                            Files = context.ManifestItems.ToList(),
                            Homepages = GetHomepages(context),
                            XRefMap = ExportXRefMap(parameters, context),
                            SourceBasePath = EnvironmentContext.BaseDirectory?.ToNormalizedPath()
                        };
                    }
                }
                finally
                {
                    if (hostServices != null)
                    {
                        foreach (var item in hostServices)
                        {
                            Cleanup(item);
                            item.Dispose();
                        }
                    }
                }
            }
        }

        private static string CreateRandomDir(string baseDir)
        {
            string folderName;
            do
            {
                folderName = Path.GetRandomFileName();
            } while (Directory.Exists(Path.Combine(baseDir, folderName)));
            Directory.CreateDirectory(Path.Combine(baseDir, folderName));
            return folderName;
        }

        private bool GetCanIncremental(string configHash, string versionName)
        {
            if (LastBuildInfo == null)
            {
                return false;
            }
            var version = LastBuildInfo.Versions.SingleOrDefault(v => v.VersionName == versionName);
            if (version == null)
            {
                Logger.LogVerbose($"Cannot build incrementally because last build didn't contain version {versionName}.");
                return false;
            }
            if (configHash != version.ConfigHash)
            {
                Logger.LogVerbose("Cannot build incrementally because config changed.");
                return false;
            }
            if (CurrentBuildInfo.DocfxVersion != LastBuildInfo.DocfxVersion)
            {
                Logger.LogVerbose($"Cannot build incrementally because docfx version changed from {LastBuildInfo.DocfxVersion} to {CurrentBuildInfo.DocfxVersion}.");
                return false;
            }
            if (CurrentBuildInfo.PluginHash != LastBuildInfo.PluginHash)
            {
                Logger.LogVerbose("Cannot build incrementally because plugin changed.");
                return false;
            }
            if (CurrentBuildInfo.TemplateHash != LastBuildInfo.TemplateHash)
            {
                Logger.LogVerbose("Cannot build incrementally because template changed.");
                return false;
            }
            return true;
        }

        private static IEnumerable<string> ExpandDependency(DependencyGraph dependencyGraph, DocumentBuildContext context, Func<DependencyItem, bool> triggerBuild)
        {
            var changeItems = context.ChangeDict;

            if (dependencyGraph != null)
            {
                foreach (var from in dependencyGraph.FromNodes)
                {
                    if (dependencyGraph.GetAllDependencyFrom(from).Any(d => triggerBuild(d) && changeItems.ContainsKey(d.To) && changeItems[d.To] != ChangeKindWithDependency.None))
                    {
                        if (!changeItems.ContainsKey(from))
                        {
                            changeItems[from] = ChangeKindWithDependency.DependencyUpdated;
                            yield return from;
                        }
                        else
                        {
                            if (changeItems[from] == ChangeKindWithDependency.None)
                            {
                                yield return from;
                            }
                            changeItems[from] |= ChangeKindWithDependency.DependencyUpdated;
                        }
                    }
                }
            }
        }

        private Dictionary<string, FileAttributeItem> ComputeFileAttributes(DocumentBuildParameters parameters)
        {
            return (from f in parameters.Files.EnumerateFiles()
                    let fileKey = ((RelativePath)f.File).GetPathFromWorkingFolder().ToString()
                    group f by fileKey into g
                    select new FileAttributeItem
                    {
                        File = g.Key,
                        LastModifiedTime = File.GetLastWriteTimeUtc(g.First().FullPath),
                        MD5 = File.ReadAllText(g.First().FullPath).GetMd5String(),
                    }).ToDictionary(a => a.File);
        }

        private void LoadChanges(DocumentBuildParameters parameter, DocumentBuildContext context, IReadOnlyDictionary<string, FileAttributeItem> fileAttributes)
        {
            var changeItems = context.ChangeDict;
            if (parameter.Changes != null)
            {
                // use user-provided changelist
                foreach (var pair in parameter.Changes)
                {
                    changeItems[pair.Key] = pair.Value;
                }
            }
            else
            {
                // get changelist from lastBuildInfo if user doesn't provide changelist
                var lastFileAttributes = LastBuildInfo.Versions.Single(v => v.VersionName == parameter.VersionName).Attributes;
                DateTime checkTime = LastBuildInfo.BuildStartTime;
                foreach (var file in fileAttributes.Keys.Intersect(lastFileAttributes.Keys))
                {
                    var last = lastFileAttributes[file];
                    var current = fileAttributes[file];
                    if (current.LastModifiedTime > checkTime || current.MD5 != last.MD5)
                    {
                        changeItems[file] = ChangeKindWithDependency.Updated;
                    }
                    else
                    {
                        changeItems[file] = ChangeKindWithDependency.None;
                    }
                }

                foreach (var file in lastFileAttributes.Keys.Except(fileAttributes.Keys))
                {
                    changeItems[file] = ChangeKindWithDependency.Deleted;
                }
                foreach (var file in fileAttributes.Keys.Except(lastFileAttributes.Keys))
                {
                    changeItems[file] = ChangeKindWithDependency.Created;
                }
            }
        }

        private static void UpdateUidDependency(IEnumerable<HostService> hostServices, DocumentBuildContext context)
        {
            foreach (var hostService in hostServices)
            {
                foreach (var m in hostService.Models)
                {
                    if (m.Type == DocumentType.Overwrite)
                    {
                        continue;
                    }
                    if (m.LinkToUids.Count != 0)
                    {
                        string fromNode = ((RelativePath)m.OriginalFileAndType.File).GetPathFromWorkingFolder().ToString();
                        var dps = from f in GetFilesFromUids(context, m.LinkToUids)
                                  select new DependencyItem(fromNode, f, fromNode, DependencyTypeName.Uid);
                        context.DependencyGraph.ReportDependency(dps);
                    }
                }
            }
        }

        private static IEnumerable<string> GetFilesFromUids(DocumentBuildContext context, IEnumerable<string> uids)
        {
            foreach (var uid in uids)
            {
                if (string.IsNullOrEmpty(uid))
                {
                    continue;
                }
                XRefSpec spec;
                if (!context.XRefSpecMap.TryGetValue(uid, out spec))
                {
                    continue;
                }
                if (spec.Href != null)
                {
                    yield return spec.Href;
                }
            }
        }

        public static ImmutableList<FileModel> Build(IDocumentProcessor processor, DocumentBuildParameters parameters, IMarkdownService markdownService)
        {
            var hostService = new HostService(
                 parameters.Files.DefaultBaseDir,
                 from file in parameters.Files.EnumerateFiles()
                 select Load(processor, parameters.Metadata, parameters.FileMetadata, file, false, null, null, null, null)
                 into model
                 where model != null
                 select model)
            {
                Processor = processor,
                MarkdownService = markdownService,
                DependencyGraph = new DependencyGraph(),
            };
            BuildCore(new List<HostService> { hostService }, parameters.MaxParallelism, null, null, null, null);
            return hostService.Models;
        }

        private void Cleanup(HostService hostService)
        {
            hostService.Models.RunAll(m => m.Dispose());
        }

        private IEnumerable<ManifestItemWithContext> BuildCore(IEnumerable<HostService> hostServices, DocumentBuildContext context, string versionName)
        {
            //preparation
            foreach (var hostService in hostServices)
            {
                hostService.SourceFiles = context.AllSourceFiles;
                hostService.DependencyGraph = context.DependencyGraph;
                foreach (var m in hostService.Models)
                {
                    if (m.LocalPathFromRepoRoot == null)
                    {
                        m.LocalPathFromRepoRoot = Path.Combine(m.BaseDir, m.File).ToDisplayPath();
                    }
                    if (m.LocalPathFromRoot == null)
                    {
                        m.LocalPathFromRoot = Path.Combine(m.BaseDir, m.File).ToDisplayPath();
                    }
                }
                using (new LoggerPhaseScope(hostService.Processor.Name, true))
                {
                    RegisterDependencyType(hostService, context);
                }
            }

            Action<HostService> buildSaver = null;
            Action<HostService> postBuildSaver = null;
            Action loader = null;
            Action postLoader = null;
            if (ShouldTraceIncrementalInfo)
            {
                var lbv = LastBuildInfo?.Versions?.SingleOrDefault(v => v.VersionName == versionName);
                var cbv = CurrentBuildInfo.Versions.Single(v => v.VersionName == versionName);
                buildSaver = h => h.SaveIntermediateModel(IntermediateFolder, lbv?.BuildModelManifest, cbv.BuildModelManifest);
                postBuildSaver = h => h.SaveIntermediateModel(IntermediateFolder, lbv?.PostBuildModelManifest, cbv.PostBuildModelManifest);
                loader = () => UpdateHostServices(hostServices, context, lbv != null ? Path.Combine(IntermediateFolder, lbv.BuildModelManifest.BaseDir) : null, _canIncremental);
                postLoader = () => UpdateHostServices(hostServices, lbv != null ? Path.Combine(IntermediateFolder, lbv.PostBuildModelManifest.BaseDir) : null, _canIncremental);
            }

            BuildCore(hostServices, context.MaxParallelism, buildSaver, postBuildSaver, loader, postLoader);

            // export manifest
            return from h in hostServices
                   from m in ExportManifest(h, context)
                   select m;
        }

        private static void BuildCore(IEnumerable<HostService> hostServices, int maxParallelism, Action<HostService> buildSaver, Action<HostService> postBuildSaver, Action loader, Action postLoader)
        {
            // prebuild and build
            foreach (var hostService in hostServices)
            {
                using (new LoggerPhaseScope(hostService.Processor.Name, true))
                {
                    var steps = string.Join("=>", hostService.Processor.BuildSteps.OrderBy(step => step.BuildOrder).Select(s => s.Name));
                    Logger.LogInfo($"Building {hostService.Models.Count} file(s) in {hostService.Processor.Name}({steps})...");
                    Logger.LogVerbose($"Processor {hostService.Processor.Name}: Prebuilding...");
                    using (new LoggerPhaseScope("Prebuild", true))
                    {
                        Prebuild(hostService);
                    }
                    Logger.LogVerbose($"Processor {hostService.Processor.Name}: Building...");
                    using (new LoggerPhaseScope("Build", true))
                    {
                        BuildArticle(hostService, maxParallelism);
                    }

                    // save models
                    if (buildSaver != null)
                    {
                        buildSaver(hostService);
                    }
                }
            }

            // load models according to changes introduced from dependencygraph
            if (loader != null)
            {
                loader();
            }

            // postbuild
            foreach (var hostService in hostServices)
            {
                using (new LoggerPhaseScope(hostService.Processor.Name, true))
                {
                    Logger.LogVerbose($"Processor {hostService.Processor.Name}: Postbuilding...");
                    using (new LoggerPhaseScope("Postbuild", true))
                    {
                        Postbuild(hostService);
                    }

                    // save models
                    if (postBuildSaver != null)
                    {
                        postBuildSaver(hostService);
                    }
                }
            }

            // load nonloaded models
            if (postLoader != null)
            {
                postLoader();
            }
        }

        private static void UpdateHostServices(IEnumerable<HostService> hostServices, DocumentBuildContext context, string cacheFolder, bool canIncremental)
        {
            UpdateUidDependency(hostServices, context);
            if (canIncremental && cacheFolder != null)
            {
                var newChanges = ExpandDependency(context.DependencyGraph, context, d => true);
                foreach (var hostService in hostServices)
                {
                    hostService.ReloadModelsPerIncrementalChanges(newChanges, cacheFolder, LoadPhase.PostBuild);
                }
            }
        }

        private static void UpdateHostServices(IEnumerable<HostService> hostServices, string cacheFolder, bool canIncremental)
        {
            if (canIncremental && cacheFolder != null)
            {
                foreach (var hostService in hostServices)
                {
                    hostService.ReloadUnloadedModels(cacheFolder, LoadPhase.PostPostBuild);
                }
            }
        }

        private void FeedXRefMap(List<ManifestItemWithContext> manifest, IDocumentBuildContext context)
        {
            Logger.LogVerbose("Feeding xref map...");
            manifest.RunAll(m =>
            {
                if (m.TemplateBundle == null)
                {
                    return;
                }

                using (new LoggerFileScope(m.FileModel.LocalPathFromRepoRoot))
                {
                    Logger.LogDiagnostic($"Feed xref map from template for {m.Item.DocumentType}...");
                    var bookmarks = m.Options.Bookmarks;
                    // TODO: Add bookmarks to xref
                }
            });
        }

        private void FeedOptions(List<ManifestItemWithContext> manifest, IDocumentBuildContext context)
        {
            Logger.LogVerbose("Feeding options from template...");
            manifest.RunAll(m =>
            {
                if (m.TemplateBundle == null)
                {
                    return;
                }

                using (new LoggerFileScope(m.FileModel.LocalPathFromRepoRoot))
                {
                    Logger.LogDiagnostic($"Feed options from template for {m.Item.DocumentType}...");
                    m.Options = m.TemplateBundle.GetOptions(m.Item, context);
                }
            });
        }

        private void ApplySystemMetadata(List<ManifestItemWithContext> manifest, IDocumentBuildContext context)
        {
            Logger.LogVerbose("Applying system metadata to manifest...");

            // Add system attributes
            var systemMetadataGenerator = new SystemMetadataGenerator(context);

            manifest.RunAll(m =>
            {
                using (new LoggerFileScope(m.FileModel.LocalPathFromRepoRoot))
                {
                    Logger.LogDiagnostic("Generating system metadata...");

                    // TODO: use weak type for system attributes from the beginning
                    var systemAttrs = systemMetadataGenerator.Generate(m.Item);
                    var metadata = (IDictionary<string, object>)ConvertToObjectHelper.ConvertStrongTypeToObject(systemAttrs);
                    // Change file model to weak type
                    var model = m.Item.Model.Content;
                    var modelAsObject = ConvertToObjectHelper.ConvertStrongTypeToObject(model) as IDictionary<string, object>;
                    if (modelAsObject != null)
                    {
                        foreach (var token in modelAsObject)
                        {
                            // Overwrites the existing system metadata if the same key is defined in document model
                            metadata[token.Key] = token.Value;
                        }
                    }
                    else
                    {
                        Logger.LogWarning("Input model is not an Object model, it will be wrapped into an Object model. Please use --exportRawModel to view the wrapped model");
                        metadata["model"] = model;
                    }

                    // Append system metadata to model
                    m.Item.Model.Content = metadata;
                }
            });
        }

        private IDictionary<string, object> FeedGlobalVariables(IDictionary<string, string> initialGlobalVariables, List<ManifestItemWithContext> manifest, IDocumentBuildContext context)
        {
            Logger.LogVerbose("Feeding global variables from template...");

            // E.g. we can set TOC model to be globally shared by every data model
            // Make sure it is single thread
            IDictionary<string, object> metadata = initialGlobalVariables == null ?
                new Dictionary<string, object>() :
                initialGlobalVariables.ToDictionary(pair => pair.Key, pair => (object)pair.Value);
            var sharedObjects = new Dictionary<string, object>();
            manifest.RunAll(m =>
            {
                if (m.TemplateBundle == null)
                {
                    return;
                }

                using (new LoggerFileScope(m.FileModel.LocalPathFromRepoRoot))
                {
                    Logger.LogDiagnostic($"Load shared model from template for {m.Item.DocumentType}...");
                    if (m.Options.IsShared)
                    {
                        sharedObjects[m.Item.Key] = m.Item.Model.Content;
                    }
                }
            });

            metadata["_shared"] = sharedObjects;
            return metadata;
        }

        private void UpdateHref(List<ManifestItemWithContext> manifest, IDocumentBuildContext context)
        {
            Logger.LogVerbose("Updating href...");
            manifest.RunAll(m =>
            {
                using (new LoggerFileScope(m.FileModel.LocalPathFromRepoRoot))
                {
                    Logger.LogDiagnostic($"Plug-in {m.Processor.Name}: Updating href...");
                    m.Processor.UpdateHref(m.FileModel, context);

                    // reset model after updating href
                    m.Item.Model = m.FileModel.ModelWithCache;
                }
            });
        }

        private static FileModel Load(
            IDocumentProcessor processor,
            ImmutableDictionary<string, object> metadata,
            FileMetadata fileMetadata,
            FileAndType file,
            bool canProcessorIncremental,
            IDictionary<string, XRefSpec> xrefSpecMap,
            IEnumerable<ManifestItem> manifestItems,
            DependencyGraph dg,
            DocumentBuildContext context)
        {
            using (new LoggerFileScope(file.File))
            {
                Logger.LogDiagnostic($"Processor {processor.Name}, File {file.FullPath}: Loading...");

                if (canProcessorIncremental)
                {
                    // reregister dependency types from last dependency graph
                    if (dg != null)
                    {
                        using (new LoggerPhaseScope("RegisterDependencyTypeFromLastBuild", true))
                        {
                            context.DependencyGraph.RegisterDependencyType(dg.DependencyTypes.Values);
                        }
                    }
                    ChangeKindWithDependency ck;
                    string fileKey = ((RelativePath)file.File).GetPathFromWorkingFolder().ToString();
                    if (context.ChangeDict.TryGetValue(fileKey, out ck))
                    {
                        Logger.LogDiagnostic($"Processor {processor.Name}, File {file.FullPath}, ChangeType {ck}.");
                        if (ck == ChangeKindWithDependency.Deleted)
                        {
                            return null;
                        }
                        if (ck == ChangeKindWithDependency.None)
                        {
                            Logger.LogDiagnostic($"Processor {processor.Name}, File {file.FullPath}: Check incremental...");
                            if (processor.BuildSteps.Cast<ISupportIncrementalBuildStep>().All(step => step.CanIncrementalBuild(file)))
                            {
                                Logger.LogDiagnostic($"Processor {processor.Name}, File {file.FullPath}: Skip build by incremental.");

                                // to-do: remove filemap/xrefmap/manifestitem restore after processor.LoadIntermediateModel is implemented
                                // restore filemap
                                context.FileMap[fileKey] = ((RelativePath)file.File).GetPathFromWorkingFolder();

                                // restore xrefspec
                                var specs = xrefSpecMap?.Values?.Where(spec => spec.Href == fileKey);
                                if (specs != null)
                                {
                                    foreach (var spec in specs)
                                    {
                                        context.XRefSpecMap[spec.Uid] = spec;
                                    }
                                }

                                // restore manifestitem
                                ManifestItem item = manifestItems?.SingleOrDefault(i => i.SourceRelativePath == file.File);
                                if (item != null)
                                {
                                    context.ManifestItems.Add(item);
                                }

                                // restore dependency graph
                                if (dg.HasDependencyReportedBy(fileKey))
                                {
                                    using (new LoggerPhaseScope("ReportDependencyFromLastBuild", true))
                                    {
                                        foreach (var l in dg.GetDependencyReportedBy(fileKey))
                                        {
                                            context.DependencyGraph.ReportDependency(l);
                                        }
                                    }
                                }

                                return null;
                            }
                            Logger.LogDiagnostic($"Processor {processor.Name}, File {file.FullPath}: Incremental not available.");
                        }
                    }
                }

                var path = Path.Combine(file.BaseDir, file.File);
                metadata = ApplyFileMetadata(path, metadata, fileMetadata);
                try
                {
                    return processor.Load(file, metadata);
                }
                catch (Exception)
                {
                    Logger.LogError($"Unable to load file: {file.File} via processor: {processor.Name}.");
                    throw;
                }
            }
        }

        private static ImmutableDictionary<string, object> ApplyFileMetadata(
            string file,
            ImmutableDictionary<string, object> metadata,
            FileMetadata fileMetadata)
        {
            if (fileMetadata == null || fileMetadata.Count == 0) return metadata;
            var result = new Dictionary<string, object>(metadata);
            var baseDir = string.IsNullOrEmpty(fileMetadata.BaseDir) ? Directory.GetCurrentDirectory() : fileMetadata.BaseDir;
            var relativePath = PathUtility.MakeRelativePath(baseDir, file);
            foreach (var item in fileMetadata)
            {
                // As the latter one overrides the former one, match the pattern from latter to former
                for (int i = item.Value.Length - 1; i >= 0; i--)
                {
                    if (item.Value[i].Glob.Match(relativePath))
                    {
                        // override global metadata if metadata is defined in file metadata
                        result[item.Value[i].Key] = item.Value[i].Value;
                        Logger.LogVerbose($"{relativePath} matches file metadata with glob pattern {item.Value[i].Glob.Raw} for property {item.Value[i].Key}");
                        break;
                    }
                }
            }
            return result.ToImmutableDictionary();
        }

        private static void RegisterDependencyType(HostService hostService, DocumentBuildContext context)
        {
            RunBuildSteps(
                hostService.Processor.BuildSteps,
                buildStep =>
                {
                    if (buildStep is ISupportIncrementalBuildStep)
                    {
                        Logger.LogVerbose($"Processor {hostService.Processor.Name}, step {buildStep.Name}: Registering DependencyType...");
                        using (new LoggerPhaseScope(buildStep.Name, true))
                        {
                            var types = (buildStep as ISupportIncrementalBuildStep).GetDependencyTypesToRegister();
                            if (types == null)
                            {
                                return;
                            }
                            context.DependencyGraph.RegisterDependencyType(types);
                        }
                    }
                });
        }

        private static void Prebuild(HostService hostService)
        {
            RunBuildSteps(
                hostService.Processor.BuildSteps,
                buildStep =>
                {
                    Logger.LogVerbose($"Processor {hostService.Processor.Name}, step {buildStep.Name}: Prebuilding...");
                    using (new LoggerPhaseScope(buildStep.Name, true))
                    {
                        var models = buildStep.Prebuild(hostService.Models, hostService);
                        if (!object.ReferenceEquals(models, hostService.Models))
                        {
                            Logger.LogVerbose($"Processor {hostService.Processor.Name}, step {buildStep.Name}: Reloading models...");
                            hostService.Reload(models);
                        }
                    }
                });
        }

        private static void BuildArticle(HostService hostService, int maxParallelism)
        {
            hostService.Models.RunAll(
                m =>
                {
                    using (new LoggerFileScope(m.LocalPathFromRepoRoot))
                    {
                        Logger.LogDiagnostic($"Processor {hostService.Processor.Name}: Building...");
                        RunBuildSteps(
                            hostService.Processor.BuildSteps,
                            buildStep =>
                            {
                                Logger.LogDiagnostic($"Processor {hostService.Processor.Name}, step {buildStep.Name}: Building...");
                                using (new LoggerPhaseScope(buildStep.Name, true))
                                {
                                    buildStep.Build(m, hostService);
                                }
                            });
                    }
                },
                maxParallelism);
        }

        private static void Postbuild(HostService hostService)
        {
            RunBuildSteps(
                hostService.Processor.BuildSteps,
                buildStep =>
                {
                    Logger.LogVerbose($"Processor {hostService.Processor.Name}, step {buildStep.Name}: Postbuilding...");
                    using (new LoggerPhaseScope(buildStep.Name, true))
                    {
                        buildStep.Postbuild(hostService.Models, hostService);
                    }
                });
        }

        private IEnumerable<ManifestItemWithContext> ExportManifest(HostService hostService, DocumentBuildContext context)
        {
            var manifestItems = new List<ManifestItemWithContext>();
            using (new LoggerPhaseScope("Save", true))
            {
                hostService.Models.RunAll(m =>
                {
                    if (m.Type != DocumentType.Overwrite)
                    {
                        using (new LoggerFileScope(m.LocalPathFromRepoRoot))
                        {
                            Logger.LogDiagnostic($"Processor {hostService.Processor.Name}: Saving...");
                            m.BaseDir = context.BuildOutputFolder;
                            if (m.FileAndType.SourceDir != m.FileAndType.DestinationDir)
                            {
                                m.File = (RelativePath)m.FileAndType.DestinationDir + (((RelativePath)m.File) - (RelativePath)m.FileAndType.SourceDir);
                            }
                            var result = hostService.Processor.Save(m);
                            if (result != null)
                            {
                                string extension = string.Empty;
                                if (hostService.Template != null)
                                {
                                    if (hostService.Template.TryGetFileExtension(result.DocumentType, out extension))
                                    {
                                        m.File = result.FileWithoutExtension + extension;
                                    }
                                }

                                var item = HandleSaveResult(context, hostService, m, result);
                                item.Extension = extension;

                                manifestItems.Add(new ManifestItemWithContext(item, m, hostService.Processor, hostService.Template?.GetTemplateBundle(result.DocumentType)));
                            }
                        }
                    }
                });
            }
            return manifestItems;
        }

        private void UpdateContext(DocumentBuildContext context)
        {
            context.ResolveExternalXRefSpec();
        }

        private InternalManifestItem HandleSaveResult(
            DocumentBuildContext context,
            HostService hostService,
            FileModel model,
            SaveResult result)
        {
            context.FileMap[model.Key] = ((RelativePath)model.File).GetPathFromWorkingFolder();
            DocumentException.RunAll(
                () => CheckFileLink(hostService, result),
                () => HandleUids(context, result),
                () => HandleToc(context, result),
                () => RegisterXRefSpec(context, result));

            return GetManifestItem(context, model, result);
        }

        private static void CheckFileLink(HostService hostService, SaveResult result)
        {
            result.LinkToFiles.RunAll(fileLink =>
            {
                if (!hostService.SourceFiles.ContainsKey(fileLink))
                {
                    var message = $"Invalid file link:({fileLink}).";
                    ImmutableList<LinkSourceInfo> list;
                    if (result.FileLinkSources.TryGetValue(fileLink, out list))
                    {
                        foreach (var fileLinkSourceFile in list)
                        {
                            message += $" Referenced by file: {fileLinkSourceFile.SourceFile} at line: {fileLinkSourceFile.LineNumber}.";
                            Logger.LogWarning(message);
                        }
                    }
                }
            });
        }

        private static void HandleUids(DocumentBuildContext context, SaveResult result)
        {
            if (result.LinkToUids.Count > 0)
            {
                context.XRef.UnionWith(result.LinkToUids.Where(s => s != null));
            }
        }

        private static void HandleToc(DocumentBuildContext context, SaveResult result)
        {
            if (result.TocMap?.Count > 0)
            {
                foreach (var toc in result.TocMap)
                {
                    HashSet<string> list;
                    if (context.TocMap.TryGetValue(toc.Key, out list))
                    {
                        foreach (var item in toc.Value)
                        {
                            list.Add(item);
                        }
                    }
                    else
                    {
                        context.TocMap[toc.Key] = toc.Value;
                    }
                }
            }
        }

        private void RegisterXRefSpec(DocumentBuildContext context, SaveResult result)
        {
            foreach (var spec in result.XRefSpecs)
            {
                if (!string.IsNullOrWhiteSpace(spec?.Uid))
                {
                    XRefSpec xref;
                    if (context.XRefSpecMap.TryGetValue(spec.Uid, out xref))
                    {
                        Logger.LogWarning($"Uid({spec.Uid}) has already been defined in {((RelativePath)xref.Href).RemoveWorkingFolder()}.");
                    }
                    else
                    {
                        context.XRefSpecMap[spec.Uid] = spec.ToReadOnly();
                    }
                }
            }
            foreach (var spec in result.ExternalXRefSpecs)
            {
                if (!string.IsNullOrWhiteSpace(spec?.Uid))
                {
                    context.ReportExternalXRefSpec(spec);
                }
            }
        }

        private static InternalManifestItem GetManifestItem(DocumentBuildContext context, FileModel model, SaveResult result)
        {
            return new InternalManifestItem
            {
                DocumentType = result.DocumentType,
                FileWithoutExtension = result.FileWithoutExtension,
                ResourceFile = result.ResourceFile,
                Key = model.Key,
                LocalPathFromRoot = model.LocalPathFromRoot,
                Model = model.ModelWithCache,
                InputFolder = model.OriginalFileAndType.BaseDir,
                Metadata = new Dictionary<string, object>((IDictionary<string, object>)model.ManifestProperties),
            };
        }

        private static void RunBuildSteps(IEnumerable<IDocumentBuildStep> buildSteps, Action<IDocumentBuildStep> action)
        {
            if (buildSteps != null)
            {
                foreach (var buildStep in buildSteps.OrderBy(step => step.BuildOrder))
                {
                    action(buildStep);
                }
            }
        }

        private IEnumerable<HostService> GetInnerContexts(
            DocumentBuildParameters parameters,
            IEnumerable<IDocumentProcessor> processors,
            TemplateProcessor templateProcessor,
            IMarkdownService markdownService,
            DocumentBuildContext context)
        {
            var k = from fileItem in (
                    from file in parameters.Files.EnumerateFiles()
                    from p in (from processor in processors
                               let priority = processor.GetProcessingPriority(file)
                               where priority != ProcessingPriority.NotSupported
                               group processor by priority into ps
                               orderby ps.Key descending
                               select ps.ToList()).FirstOrDefault() ?? new List<IDocumentProcessor> { null }
                    select new { file, p })
                    group fileItem by fileItem.p;

            var toHandleItems = k.Where(s => s.Key != null);
            var notToHandleItems = k.Where(s => s.Key == null);
            foreach (var item in notToHandleItems)
            {
                var sb = new StringBuilder();
                sb.AppendLine("Cannot handle following file:");
                foreach (var f in item)
                {
                    sb.Append("\t");
                    sb.AppendLine(f.file.File);
                }
                Logger.LogWarning(sb.ToString());
            }

            // todo : revert until PreProcessor ready
            var pairs = from processor in processors
                        join item in toHandleItems on processor equals item.Key into g
                        from item in g.DefaultIfEmpty()
                        select new
                        {
                            processor,
                            item,
                            canProcessorIncremental = CanProcessorIncremental(processor, parameters.VersionName),
                        };

            // load last xrefspecmap and manifestitems
            BuildVersionInfo lbvi = LastBuildInfo?.Versions.SingleOrDefault(v => v.VersionName == parameters.VersionName);
            var lastXRefSpecMap = lbvi?.XRefSpecMap;
            var lastManifest = lbvi?.Manifest;
            var lastDependencyGraph = lbvi?.Dependency;

            foreach (var pair in pairs.AsParallel().WithDegreeOfParallelism(parameters.MaxParallelism))
            {
                var hostService = new HostService(
                       parameters.Files.DefaultBaseDir,
                       pair.item == null
                            ? new FileModel[0]
                            : from file in pair.item
                              let canIncremental = _canIncremental ? pair.canProcessorIncremental : _canIncremental
                              select Load(pair.processor, parameters.Metadata, parameters.FileMetadata, file.file, canIncremental, lastXRefSpecMap, lastManifest, lastDependencyGraph, context) into model
                              where model != null
                              select model)
                {
                    MarkdownService = markdownService,
                    Processor = pair.processor,
                    Template = templateProcessor,
                    Validators = MetadataValidators.ToImmutableList(),
                };

                if (pair.item != null)
                {
                    var allFiles = pair.item.Select(f => f.file);
                    var loadedFiles = hostService.Models.Select(m => m.FileAndType);
                    hostService.ReportModelLoadInfo(allFiles.Except(loadedFiles), LoadPhase.None);
                    hostService.ReportModelLoadInfo(loadedFiles, LoadPhase.PreBuild);
                }
                yield return hostService;
            }
        }

        private bool CanProcessorIncremental(IDocumentProcessor processor, string versionName)
        {
            if (!ShouldTraceIncrementalInfo)
            {
                return false;
            }
            if (!(processor is ISupportIncrementalDocumentProcessor))
            {
                Logger.LogVerbose($"Processor {processor.Name} cannot suppport incremental build because the processor doesn't implement {nameof(ISupportIncrementalDocumentProcessor)} interface.");
                return false;
            }
            if (!processor.BuildSteps.All(step => step is ISupportIncrementalBuildStep))
            {
                Logger.LogVerbose($"Processor {processor.Name} cannot suppport incremental build because the following steps don't implement {nameof(ISupportIncrementalBuildStep)} interface: {string.Join(",", processor.BuildSteps.Where(step => !(step is ISupportIncrementalBuildStep)).Select(s => s.Name))}.");
                return false;
            }

            var cpi = GetProcessorInfo(processor, versionName);
            var lpi = LastBuildInfo
                ?.Versions
                ?.Find(v => v.VersionName == versionName)
                ?.Processors
                ?.Find(p => p.Name == processor.Name);
            if (lpi == null)
            {
                Logger.LogVerbose($"Processor {processor.Name} cannot support incremental build because last build doesn't contain version {versionName}.");
                return false;
            }
            if (cpi.IncrementalContextHash != lpi.IncrementalContextHash)
            {
                Logger.LogVerbose($"Processor {processor.Name} cannot support incremental build because incremental context hash changed.");
                return false;
            }
            if (!new HashSet<ProcessorStepInfo>(cpi.Steps).SetEquals(lpi.Steps))
            {
                Logger.LogVerbose($"Processor {processor.Name} cannot support incremental build because steps changed.");
                return false;
            }
            return true;
        }

        private ProcessorInfo GetProcessorInfo(IDocumentProcessor processor, string versionName)
        {
            var cpi = new ProcessorInfo
            {
                Name = processor.Name,
                IncrementalContextHash = ((ISupportIncrementalDocumentProcessor)processor).GetIncrementalContextHash(),
            };
            foreach (var step in processor.BuildSteps)
            {
                cpi.Steps.Add(new ProcessorStepInfo
                {
                    Name = step.Name,
                    IncrementalContextHash = ((ISupportIncrementalBuildStep)step).GetIncrementalContextHash(),
                });
            }
            var cvi = CurrentBuildInfo.Versions.Find(v => v.VersionName == versionName);
            if (cvi == null)
            {
                cvi = new BuildVersionInfo { VersionName = versionName };
                CurrentBuildInfo.Versions.Add(cvi);
            }
            cvi.Processors.Add(cpi);
            return cpi;
        }

        private static List<HomepageInfo> GetHomepages(DocumentBuildContext context)
        {
            return context.GetTocInfo()
                .Where(s => !string.IsNullOrEmpty(s.Homepage))
                .Select(s => new HomepageInfo
                {
                    Homepage = RelativePath.GetPathWithoutWorkingFolderChar(s.Homepage),
                    TocPath = RelativePath.GetPathWithoutWorkingFolderChar(context.GetFilePath(s.TocFileKey))
                }).ToList();
        }

        /// <summary>
        /// Export xref map file.
        /// </summary>
        private static string ExportXRefMap(DocumentBuildParameters parameters, DocumentBuildContext context)
        {
            Logger.LogVerbose("Exporting xref map...");
            var xrefMap = new XRefMap();
            xrefMap.References =
                (from xref in context.XRefSpecMap.Values.AsParallel().WithDegreeOfParallelism(parameters.MaxParallelism)
                 select new XRefSpec(xref)
                 {
                     Href = ((RelativePath)context.FileMap[xref.Href]).RemoveWorkingFolder().ToString() + "#" + XRefDetails.GetHtmlId(xref.Uid),
                 }).ToList();
            xrefMap.Sort();
            string xrefMapFileNameWithVersion = string.IsNullOrEmpty(parameters.VersionName) ?
                XRefMapFileName :
                parameters.VersionName + "." + XRefMapFileName;
            YamlUtility.Serialize(
                Path.Combine(parameters.OutputBaseDir, xrefMapFileNameWithVersion),
                xrefMap,
                YamlMime.XRefMap);
            Logger.LogInfo("XRef map exported.");
            return xrefMapFileNameWithVersion;
        }

        private IMarkdownService CreateMarkdownService(DocumentBuildParameters parameters, ImmutableDictionary<string, string> tokens)
        {
            var provider = (IMarkdownServiceProvider)Container.GetExport(
                typeof(IMarkdownServiceProvider),
                parameters.MarkdownEngineName);
            if (provider == null)
            {
                Logger.LogError($"Unable to find markdown engine: {parameters.MarkdownEngineName}");
                throw new DocfxException($"Unable to find markdown engine: {parameters.MarkdownEngineName}");
            }
            Logger.LogInfo($"Markdown engine is {parameters.MarkdownEngineName}");
            return provider.CreateMarkdownService(
                new MarkdownServiceParameters
                {
                    BasePath = parameters.Files.DefaultBaseDir,
                    TemplateDir = parameters.TemplateDir,
                    Extensions = parameters.MarkdownEngineParameters,
                    Tokens = tokens,
                });
        }

        private static string ComputeConfigHash(DocumentBuildParameters parameter)
        {
            return JsonConvert.SerializeObject(
                parameter,
                new JsonSerializerSettings
                {
                    ContractResolver = new IncrementalCheckPropertiesResolver()
                }).GetMd5String();
        }

        public void Dispose()
        {
            foreach (var processor in Processors)
            {
                Logger.LogVerbose($"Disposing processor {processor.Name} ...");
                (processor as IDisposable)?.Dispose();
            }
        }

        private sealed class ManifestItemWithContext
        {
            public InternalManifestItem Item { get; }
            public FileModel FileModel { get; }
            public IDocumentProcessor Processor { get; }
            public TemplateBundle TemplateBundle { get; }

            public TransformModelOptions Options { get; set; }
            public ManifestItemWithContext(InternalManifestItem item, FileModel model, IDocumentProcessor processor, TemplateBundle bundle)
            {
                Item = item;
                FileModel = model;
                Processor = processor;
                TemplateBundle = bundle;
            }
        }
    }
}

