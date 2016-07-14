// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.Engine
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Composition;
    using System.Composition.Hosting;
    using System.Collections.Immutable;
    using System.Reflection;
    using System.Security.Cryptography;
    using System.Text;

    using Microsoft.DocAsCode.Common;
    using Microsoft.DocAsCode.Exceptions;
    using Microsoft.DocAsCode.Plugins;
    using Microsoft.DocAsCode.Utility;

    public class DocumentBuilder : IDisposable
    {
        public const string PhaseName = "Build Document";
        public const string XRefMapFileName = "xrefmap.yml";

        private readonly List<ManifestItem> _manifest = new List<ManifestItem>();
        private readonly List<HomepageInfo> _homepages = new List<HomepageInfo>();
        private readonly List<string> _xrefMaps = new List<string>();
        private readonly BuildInfo _currentBuildInfo =
            new BuildInfo
            {
                BuildStartTime = DateTime.UtcNow,
                DocfxVersion = typeof(DocumentBuilder).Assembly.GetCustomAttribute<AssemblyFileVersionAttribute>()?.Version.ToString(),
            };
        private readonly CompositionHost _container;

        public string IntermediateFolder { get; set; }

        private static CompositionHost GetContainer(IEnumerable<Assembly> assemblies)
        {
            var configuration = new ContainerConfiguration();

            configuration.WithAssembly(typeof(DocumentBuilder).Assembly);

            if (assemblies != null)
            {
                foreach (var assembly in assemblies)
                {
                    if (assembly != null)
                    {
                        configuration.WithAssembly(assembly);
                    }
                }
            }

            try
            {
                return configuration.CreateContainer();
            }
            catch (ReflectionTypeLoadException ex)
            {
                Logger.LogError(
                    $"Error when get composition container: {ex.Message}, loader exceptions: {(ex.LoaderExceptions != null ? string.Join(", ", ex.LoaderExceptions.Select(e => e.Message)) : "none")}");
                throw;
            }
        }

        public DocumentBuilder(IEnumerable<Assembly> assemblies = null)
        {
            using (new LoggerPhaseScope(PhaseName))
            {
                Logger.LogVerbose("Loading plug-in...");
                var assemblyList = assemblies?.ToList();
                _container = GetContainer(assemblyList);
                _container.SatisfyImports(this);
                _currentBuildInfo.PluginHash = ComputePluginHash(assemblyList);
                Logger.LogInfo($"{Processors.Count()} plug-in(s) loaded.");
                foreach (var processor in Processors)
                {
                    Logger.LogVerbose($"\t{processor.Name} with build steps ({string.Join(", ", from bs in processor.BuildSteps orderby bs.BuildOrder select bs.Name)})");
                }
            }
        }

        [ImportMany]
        internal IEnumerable<IDocumentProcessor> Processors { get; set; }

        [ImportMany]
        internal IEnumerable<IInputMetadataValidator> MetadataValidators { get; set; }

        public void SaveManifest(string outputDirectory)
        {
            TemplateProcessor.SaveManifest(_manifest, _homepages, _xrefMaps, outputDirectory);
        }

        public void Build(DocumentBuildParameters parameters)
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

            BuildCore(parameters);
        }

        private void BuildCore(DocumentBuildParameters parameters)
        {
            using (new LoggerPhaseScope(PhaseName))
            {
                Logger.LogInfo($"Max parallelism is {parameters.MaxParallelism.ToString()}.");
                Directory.CreateDirectory(parameters.OutputBaseDir);
                var context = new DocumentBuildContext(
                    Path.Combine(Environment.CurrentDirectory, parameters.OutputBaseDir),
                    parameters.Files.EnumerateFiles(),
                    parameters.ExternalReferencePackages,
                    parameters.XRefMaps,
                    parameters.MaxParallelism,
                    parameters.Files.DefaultBaseDir);
                if (IntermediateFolder != null)
                {
                    _currentBuildInfo.Versions.Add(
                        new BuildVersionInfo
                        {
                            VersionName = parameters.VersionName,
                        });
                }
                Logger.LogVerbose("Start building document...");

                // Prepare for post process
                var postProcessorNames = parameters.PostProcessors;

                // For backward compatible, retain "_enableSearch" to globalMetadata though it's deprecated
                object value;
                if (parameters.Metadata.TryGetValue("_enableSearch", out value))
                {
                    var isSearchable = value as bool?;
                    if (isSearchable.HasValue && isSearchable.Value && !postProcessorNames.Contains("ExtractSearchIndex"))
                    {
                        postProcessorNames = postProcessorNames.Add("ExtractSearchIndex");
                    }
                }

                var postProcessors = GetPostProcessor(postProcessorNames);
                foreach (var postProcessor in postProcessors)
                {
                    using (new LoggerPhaseScope(postProcessor.Item1))
                    {
                        parameters.Metadata = postProcessor.Item2.PrepareMetadata(parameters.Metadata);
                        if (parameters.Metadata == null)
                        {
                            throw new DocfxException($"Plugin {postProcessor.Item1} should not return null metadata");
                        }
                    }
                }

                // Start building document...
                List<HostService> hostServices = null;
                try
                {
                    using (var processor = parameters.TemplateManager?.GetTemplateProcessor(parameters.MaxParallelism) ?? TemplateProcessor.DefaultProcessor)
                    {
                        var markdownService = CreateMarkdownService(parameters, processor.Tokens.ToImmutableDictionary());
                        hostServices = GetInnerContexts(parameters, Processors, processor, markdownService).ToList();
                        var manifest = new List<ManifestItemWithContext>();
                        foreach (var hostService in hostServices)
                        {
                            manifest.AddRange(BuildCore(hostService, context));
                        }

                        if (IntermediateFolder != null)
                        {
                            UpdateUidDependency(context, hostServices);
                            SaveDependency(context, parameters);
                        }

                        // Use manifest from now on
                        UpdateContext(context);

                        // Run getOptions from Template
                        FeedOptions(manifest, context);

                        // Template can feed back xref map, actually, the anchor # location can only be determined in template
                        FeedXRefMap(manifest, context);

                        UpdateHref(manifest, context);

                        // Afterwards, m.Item.Model.Content is always IDictionary
                        ApplySystemMetadata(manifest, context);

                        // Register global variables after href are all updated
                        IDictionary<string, object> globalVariables = FeedGlobalVariables(processor.Tokens, manifest, context);

                        // processor to add global variable to the model
                        var generatedManifest = new Manifest
                        {
                            Files = processor.Process(manifest.Select(s => s.Item).ToList(), context, parameters.ApplyTemplateSettings, globalVariables),
                            Homepages = GetHomepages(context),
                            XRefMap = ExportXRefMap(parameters, context)
                        };

                        RemoveDuplicateOutputFiles(generatedManifest.Files);

                        // post process
                        foreach (var postProcessor in postProcessors)
                        {
                            using (new LoggerPhaseScope(postProcessor.Item1))
                            {
                                generatedManifest = postProcessor.Item2.Process(generatedManifest, parameters.OutputBaseDir);
                                if (generatedManifest == null)
                                {
                                    throw new DocfxException($"Plugin {postProcessor.Item1} should not return null manifest");
                                }

                                // To make sure post processor won't generate duplicate output files
                                RemoveDuplicateOutputFiles(generatedManifest.Files);
                            }
                        }

                        _manifest.AddRange(generatedManifest.Files);
                        _homepages.AddRange(generatedManifest.Homepages);
                        _xrefMaps.Add((string)generatedManifest.XRefMap);

                        // Last step: save manifest file
                        var manifestJsonPath = Path.Combine(parameters.OutputBaseDir, Constants.ManifestFileName);
                        JsonUtility.Serialize(manifestJsonPath, generatedManifest);
                        Logger.LogInfo($"Manifest file saved to {manifestJsonPath}.");

                        Logger.LogInfo($"Completed building {generatedManifest.Files?.Count} file(s).");
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

        private void UpdateUidDependency(DocumentBuildContext context, List<HostService> hostServices)
        {
            foreach (var hostService in hostServices)
            {
                foreach (var m in hostService.Models)
                {
                    if (m.Type == DocumentType.Overwrite)
                    {
                        continue;
                    }
                    if (m.LinkToUids.Count == 0)
                    {
                        continue;
                    }
                    context.DependencyGraph.ReportDependency(
                        m.OriginalFileAndType.File,
                        GetFilesFromUids(context, m.LinkToUids));
                }
            }
        }

        private void SaveDependency(DocumentBuildContext context, DocumentBuildParameters parameters)
        {
            var vbi = _currentBuildInfo.Versions.Find(v => v.VersionName == parameters.VersionName);
            vbi.Dependency = Path.GetRandomFileName();
            using (var writer = File.CreateText(
                Path.Combine(IntermediateFolder, vbi.Dependency)))
            {
                context.DependencyGraph.Save(writer);
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

        public static void RemoveDuplicateOutputFiles(List<ManifestItem> manifestItems)
        {
            if (manifestItems == null)
            {
                throw new ArgumentNullException(nameof(manifestItems));
            }

            var itemsToRemove = new List<string>();
            foreach (var duplicates in from m in manifestItems
                                       from output in m.OutputFiles.Values
                                       group m.OriginalFile by output into g
                                       where g.Count() > 1
                                       select g)
            {
                Logger.LogWarning($"Overwrite occurs while input files \"{string.Join(", ", duplicates)}\" writing to the same output file \"{duplicates.Key}\"");
                itemsToRemove.AddRange(duplicates.Skip(1));
            }
            manifestItems.RemoveAll(m => itemsToRemove.Contains(m.OriginalFile));
        }

        public static ImmutableList<FileModel> Build(IDocumentProcessor processor, DocumentBuildParameters parameters, IMarkdownService markdownService)
        {
            var hostService = new HostService(
                 parameters.Files.DefaultBaseDir,
                 from file in parameters.Files.EnumerateFiles()
                 select Load(processor, parameters.Metadata, parameters.FileMetadata, file)
                 into model
                 where model != null
                 select model)
            {
                Processor = processor,
                MarkdownService = markdownService,
                DependencyGraph = new DependencyGraph(),
            };
            BuildCore(hostService, parameters.MaxParallelism);
            return hostService.Models;
        }

        private List<Tuple<string, IPostProcessor>> GetPostProcessor(ImmutableArray<string> processors)
        {
            var processorList = new List<Tuple<string, IPostProcessor>>();
            foreach (var processor in processors)
            {
                var p = GetExport(typeof(IPostProcessor), processor) as IPostProcessor;
                Logger.LogInfo($"Post processor {processor} loaded.");
                if (p != null)
                {
                    processorList.Add(new Tuple<string, IPostProcessor>(processor, p));
                }
                else
                {
                    Logger.LogWarning($"Can't find the post processor: {processor}");
                }
            }
            return processorList;
        }

        private object GetExport(Type type, string name)
        {
            object exportedObject = null;
            try
            {
                exportedObject = _container.GetExport(type, name);
            }
            catch (CompositionFailedException ex)
            {
                Logger.LogWarning($"Can't import: {name}, {ex}");
            }
            return exportedObject;
        }

        private void Cleanup(HostService hostService)
        {
            hostService.Models.RunAll(m => m.Dispose());
        }

        private IEnumerable<ManifestItemWithContext> BuildCore(HostService hostService, DocumentBuildContext context)
        {
            hostService.SourceFiles = context.AllSourceFiles;
            hostService.DependencyGraph = context.DependencyGraph;
            BuildCore(hostService, context.MaxParallelism);
            return ExportManifest(hostService, context);
        }

        private static void BuildCore(HostService hostService, int maxParallelism)
        {
            Logger.LogVerbose($"Processor {hostService.Processor.Name}: Loading document...");
            using (new LoggerPhaseScope(hostService.Processor.Name))
            {
                foreach (var m in hostService.Models)
                {
                    if (m.LocalPathFromRepoRoot == null)
                    {
                        m.LocalPathFromRepoRoot = Path.Combine(m.BaseDir, m.File).ToDisplayPath();
                    }
                }
                var steps = string.Join("=>", hostService.Processor.BuildSteps.OrderBy(step => step.BuildOrder).Select(s => s.Name));
                Logger.LogInfo($"Building {hostService.Models.Count} file(s) in {hostService.Processor.Name}({steps})...");
                Logger.LogVerbose($"Processor {hostService.Processor.Name}: Preprocessing...");
                Prebuild(hostService);
                Logger.LogVerbose($"Processor {hostService.Processor.Name}: Building...");
                BuildArticle(hostService, maxParallelism);
                Logger.LogVerbose($"Processor {hostService.Processor.Name}: Postprocessing...");
                Postbuild(hostService);
                Logger.LogVerbose($"Processor {hostService.Processor.Name}: Generating manifest...");
            }
        }

        private void FeedXRefMap(List<ManifestItemWithContext> manifest, IDocumentBuildContext context)
        {
            Logger.LogVerbose($"Feeding xref map...");
            manifest.RunAll(m =>
            {
                if (m.TemplateBundle == null)
                {
                    return;
                }

                using (new LoggerFileScope(m.FileModel.LocalPathFromRepoRoot))
                {
                    Logger.LogVerbose($"Feed xref map from template for {m.Item.DocumentType}...");
                    var bookmarks = m.Options.Bookmarks;
                    // TODO: Add bookmarks to xref
                }
            });
        }

        private void FeedOptions(List<ManifestItemWithContext> manifest, IDocumentBuildContext context)
        {
            Logger.LogVerbose($"Feeding options from template...");
            manifest.RunAll(m =>
            {
                if (m.TemplateBundle == null)
                {
                    return;
                }

                using (new LoggerFileScope(m.FileModel.LocalPathFromRepoRoot))
                {
                    Logger.LogVerbose($"Feed options from template for {m.Item.DocumentType}...");
                    m.Options = m.TemplateBundle.GetOptions(m.Item, context);
                }
            });
        }

        private void ApplySystemMetadata(List<ManifestItemWithContext> manifest, IDocumentBuildContext context)
        {
            Logger.LogVerbose($"Applying system metadata to manifest...");

            // Add system attributes
            var systemMetadataGenerator = new SystemMetadataGenerator(context);

            manifest.RunAll(m =>
            {
                using (new LoggerFileScope(m.FileModel.LocalPathFromRepoRoot))
                {
                    Logger.LogVerbose($"Generating system metadata...");

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
            Logger.LogVerbose($"Feeding global variables from template...");

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
                    Logger.LogVerbose($"Load shared model from template for {m.Item.DocumentType}...");
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
            Logger.LogVerbose($"Updating href...");
            manifest.RunAll(m =>
            {
                using (new LoggerFileScope(m.FileModel.LocalPathFromRepoRoot))
                {
                    Logger.LogVerbose($"Plug-in {m.Processor.Name}: Updating href...");
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
            FileAndType file)
        {
            using (new LoggerFileScope(file.File))
            {
                Logger.LogVerbose($"Processor {processor.Name}: Loading...");

                var path = Path.Combine(file.BaseDir, file.File);
                metadata = ApplyFileMetadata(path, metadata, fileMetadata);
                return processor.Load(file, metadata);
            }
        }

        private static ImmutableDictionary<string, object> ApplyFileMetadata(
            string file,
            ImmutableDictionary<string, object> metadata,
            FileMetadata fileMetadata)
        {
            if (fileMetadata == null || fileMetadata.Count == 0) return metadata;
            var result = new Dictionary<string, object>(metadata);
            var baseDir = string.IsNullOrEmpty(fileMetadata.BaseDir) ? Environment.CurrentDirectory : fileMetadata.BaseDir;
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

        private static void Prebuild(HostService hostService)
        {
            RunBuildSteps(
                hostService.Processor.BuildSteps,
                buildStep =>
                {
                    Logger.LogVerbose($"Processor {hostService.Processor.Name}, step {buildStep.Name}: Preprocessing...");
                    using (new LoggerPhaseScope(buildStep.Name))
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
                        Logger.LogVerbose($"Processor {hostService.Processor.Name}: Building...");
                        RunBuildSteps(
                            hostService.Processor.BuildSteps,
                            buildStep =>
                            {
                                Logger.LogVerbose($"Processor {hostService.Processor.Name}, step {buildStep.Name}: Building...");
                                using (new LoggerPhaseScope(buildStep.Name))
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
                    Logger.LogVerbose($"Processor {hostService.Processor.Name}, step {buildStep.Name}: Postprocessing...");
                    using (new LoggerPhaseScope(buildStep.Name))
                    {
                        buildStep.Postbuild(hostService.Models, hostService);
                    }
                });
        }

        private IEnumerable<ManifestItemWithContext> ExportManifest(HostService hostService, DocumentBuildContext context)
        {
            var manifestItems = new List<ManifestItemWithContext>();
            hostService.Models.RunAll(m =>
            {
                if (m.Type != DocumentType.Overwrite)
                {
                    using (new LoggerFileScope(m.LocalPathFromRepoRoot))
                    {
                        Logger.LogVerbose($"Plug-in {hostService.Processor.Name}: Saving...");
                        m.BaseDir = context.BuildOutputFolder;
                        if (m.PathRewriter != null)
                        {
                            m.File = m.PathRewriter(m.File);
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
                    var message = $"Invalid file link({fileLink})";
                    Logger.LogWarning(message);
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
                // TODO: What is API doc's LocalPathToRepo? => defined in ManagedReferenceDocumentProcessor
                LocalPathFromRepoRoot = model.LocalPathFromRepoRoot,
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
            IMarkdownService markdownService)
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

            return from item in toHandleItems.AsParallel().WithDegreeOfParallelism(parameters.MaxParallelism)
                   select new HostService(
                       parameters.Files.DefaultBaseDir,
                       from file in item
                       select Load(item.Key, parameters.Metadata, parameters.FileMetadata, file.file) into model
                       where model != null
                       select model)
                   {
                       MarkdownService = markdownService,
                       Processor = item.Key,
                       Template = templateProcessor,
                       Validators = MetadataValidators.ToImmutableList(),
                   };
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
            var provider = (IMarkdownServiceProvider)_container.GetExport(
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
                    Extensions = parameters.MarkdownEngineParameters,
                    Tokens = tokens,
                });
        }

        private static string ComputePluginHash(List<Assembly> assemblyList)
        {
            if (assemblyList?.Count > 0)
            {
                using (var ms = new MemoryStream())
                using (var writer = new StreamWriter(ms))
                {
                    foreach (var item in
                        from assembly in assemblyList
                        select assembly.FullName + "@" + assembly.GetCustomAttribute<AssemblyFileVersionAttribute>().Version.ToString()
                        into item
                        orderby item
                        select item)
                    {
                        writer.WriteLine(item);
                    }
                    writer.Flush();
                    ms.Seek(0, SeekOrigin.Begin);
                    return Convert.ToBase64String(MD5.Create().ComputeHash(ms));
                }
            }
            return string.Empty;
        }

        public void Dispose()
        {
            if (IntermediateFolder != null)
            {
                JsonUtility.Serialize(
                    Path.Combine(IntermediateFolder, BuildInfo.FileName),
                    _currentBuildInfo);
            }
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
