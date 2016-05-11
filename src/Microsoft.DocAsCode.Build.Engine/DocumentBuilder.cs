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
    using System.Text;

    using Microsoft.DocAsCode.Common;
    using Microsoft.DocAsCode.Exceptions;
    using Microsoft.DocAsCode.Plugins;
    using Microsoft.DocAsCode.Utility;

    public class DocumentBuilder : IDisposable
    {
        public const string PhaseName = "Build Document";
        public const string XRefMapFileName = "xrefmap.yml";

        private readonly CompositionHost _container;

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
                _container = GetContainer(assemblies);
                _container.SatisfyImports(this);
                Logger.LogInfo($"{Processors.Count()} plug-in(s) loaded.");
                foreach (var processor in Processors)
                {
                    Logger.LogVerbose($"\t{processor.Name} with build steps ({string.Join(", ", from bs in processor.BuildSteps orderby bs.BuildOrder select bs.Name)})");
                }
            }
        }

        [ImportMany]
        internal IEnumerable<IDocumentProcessor> Processors { get; set; }

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

            using (new LoggerPhaseScope(PhaseName))
            {
                Logger.LogInfo($"Max parallelism is {parameters.MaxParallelism.ToString()}.");
                var markdownService = CreateMarkdownService(parameters);
                Directory.CreateDirectory(parameters.OutputBaseDir);
                var context = new DocumentBuildContext(
                    Path.Combine(Environment.CurrentDirectory, parameters.OutputBaseDir),
                    parameters.Files.EnumerateFiles(),
                    parameters.ExternalReferencePackages,
                    parameters.XRefMaps,
                    parameters.MaxParallelism);
                Logger.LogVerbose("Start building document...");
                List<InnerBuildContext> innerContexts = null;
                try
                {
                    using (var processor = parameters.TemplateManager?.GetTemplateProcessor(parameters.MaxParallelism) ?? TemplateProcessor.DefaultProcessor)
                    {
                        innerContexts = GetInnerContexts(parameters, Processors, processor, markdownService).ToList();
                        var manifest = new List<ManifestItemWithContext>();
                        foreach (var item in innerContexts)
                        {
                            manifest.AddRange(BuildCore(item, context));
                        }

                        // Use manifest from now on
                        UpdateContext(context);
                        UpdateHref(manifest, context);

                        var generatedManifest = processor.Process(manifest.Select(s => s.Item).ToList(), context, parameters.ApplyTemplateSettings);

                        ExportXRefMap(parameters, context);

                        // todo : move to plugin.
                        object value;
                        if (parameters.Metadata.TryGetValue("_enableSearch", out value))
                        {
                            var isSearchable = value as bool?;
                            if (isSearchable.HasValue && isSearchable.Value)
                            {
                                ExtractSearchData.ExtractSearchIndexFromHtml.GenerateFile(generatedManifest, parameters.OutputBaseDir);
                            }
                        }
                        Logger.LogInfo($"Building {manifest.Count} file(s) completed.");
                    }
                }
                finally
                {
                    if (innerContexts != null)
                    {
                        foreach (var item in innerContexts)
                        {
                            if (item.HostService != null)
                            {
                                Cleanup(item.HostService);
                                item.HostService.Dispose();
                            }
                        }
                    }
                }
            }
        }

        public static ImmutableList<FileModel> Build(IDocumentProcessor processor, DocumentBuildParameters parameters, IMarkdownService markdownService)
        {
            var hostService = new HostService(
                 parameters.Files.DefaultBaseDir,
                 from file in parameters.Files.EnumerateFiles()
                 select Load(processor, parameters.Metadata, parameters.FileMetadata, file)
                 into model
                 where model != null
                 select model);
            hostService.MarkdownService = markdownService;
            BuildCore(processor, hostService, parameters.MaxParallelism);
            return hostService.Models;
        }

        private void Cleanup(HostService hostService)
        {
            hostService.Models.RunAll(m => m.Dispose());
        }

        private IEnumerable<ManifestItemWithContext> BuildCore(InnerBuildContext buildContext, DocumentBuildContext context)
        {
            var processor = buildContext.Processor;
            buildContext.HostService.SourceFiles = context.AllSourceFiles;
            BuildCore(processor, buildContext.HostService, context.MaxParallelism);
            return ExportManifest(buildContext, context);
        }

        private static void BuildCore(IDocumentProcessor processor, HostService hostService, int maxParallelism)
        {
            Logger.LogVerbose($"Processor {processor.Name}: Loading document...");
            using (new LoggerPhaseScope(processor.Name))
            {
                foreach (var m in hostService.Models)
                {
                    if (m.LocalPathFromRepoRoot == null)
                    {
                        m.LocalPathFromRepoRoot = Path.Combine(m.BaseDir, m.File).ToDisplayPath();
                    }
                }
                var steps = string.Join("=>", processor.BuildSteps.OrderBy(step => step.BuildOrder).Select(s => s.Name));
                Logger.LogInfo($"Building {hostService.Models.Count} file(s) in {processor.Name}({steps})...");
                Logger.LogVerbose($"Processor {processor.Name}: Preprocessing...");
                Prebuild(processor, hostService);
                Logger.LogVerbose($"Processor {processor.Name}: Building...");
                BuildArticle(processor, hostService, maxParallelism);
                Logger.LogVerbose($"Processor {processor.Name}: Postprocessing...");
                Postbuild(processor, hostService);
                Logger.LogVerbose($"Processor {processor.Name}: Generating manifest...");
            }
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

        private static void Prebuild(IDocumentProcessor processor, HostService hostService)
        {
            RunBuildSteps(
                processor.BuildSteps,
                buildStep =>
                {
                    Logger.LogVerbose($"Processor {processor.Name}, step {buildStep.Name}: Preprocessing...");
                    using (new LoggerPhaseScope(buildStep.Name))
                    {
                        var models = buildStep.Prebuild(hostService.Models, hostService);
                        if (!object.ReferenceEquals(models, hostService.Models))
                        {
                            Logger.LogVerbose($"Processor {processor.Name}, step {buildStep.Name}: Reloading models...");
                            hostService.Reload(models);
                        }
                    }
                });
        }

        private static void BuildArticle(IDocumentProcessor processor, HostService hostService, int maxParallelism)
        {
            hostService.Models.RunAll(
                m =>
                {
                    using (new LoggerFileScope(m.LocalPathFromRepoRoot))
                    {
                        Logger.LogVerbose($"Processor {processor.Name}: Building...");
                        RunBuildSteps(
                            processor.BuildSteps,
                            buildStep =>
                            {
                                Logger.LogVerbose($"Processor {processor.Name}, step {buildStep.Name}: Building...");
                                using (new LoggerPhaseScope(buildStep.Name))
                                {
                                    buildStep.Build(m, hostService);
                                }
                            });
                    }
                },
                maxParallelism);
        }

        private static void Postbuild(IDocumentProcessor processor, HostService hostService)
        {
            RunBuildSteps(
                processor.BuildSteps,
                buildStep =>
                {
                    Logger.LogVerbose($"Processor {processor.Name}, step {buildStep.Name}: Postprocessing...");
                    using (new LoggerPhaseScope(buildStep.Name))
                    {
                        buildStep.Postbuild(hostService.Models, hostService);
                    }
                });
        }

        private IEnumerable<ManifestItemWithContext> ExportManifest(InnerBuildContext buildContext, DocumentBuildContext context)
        {
            var hostService = buildContext.HostService;
            var processor = buildContext.Processor;
            var templateProcessor = buildContext.TemplateProcessor;
            var manifestItems = new List<ManifestItemWithContext>();
            hostService.Models.RunAll(m =>
            {
                if (m.Type != DocumentType.Overwrite)
                {
                    using (new LoggerFileScope(m.LocalPathFromRepoRoot))
                    {
                        Logger.LogVerbose($"Plug-in {processor.Name}: Saving...");
                        m.BaseDir = context.BuildOutputFolder;
                        if (m.PathRewriter != null)
                        {
                            m.File = m.PathRewriter(m.File);
                        }
                        var result = processor.Save(m);
                        if (result != null)
                        {
                            string extension = string.Empty;
                            if (templateProcessor != null)
                            {
                                if (templateProcessor.TryGetFileExtension(result.DocumentType, out extension))
                                {
                                    // For backward-compatibility, will remove ModelFile in v1.9
                                    if (string.IsNullOrEmpty(result.FileWithoutExtension))
                                    {
                                        result.FileWithoutExtension = Path.ChangeExtension(result.ModelFile, null);
                                    }

                                    m.File = result.FileWithoutExtension + extension;
                                }
                            }

                            var item = HandleSaveResult(context, hostService, m, result);
                            item.Extension = extension;

                            manifestItems.Add(new ManifestItemWithContext(item, m, processor, templateProcessor?.GetTemplateBundle(result.DocumentType)));
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

        private ManifestItem HandleSaveResult(
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

        private static ManifestItem GetManifestItem(DocumentBuildContext context, FileModel model, SaveResult result)
        {
            return new ManifestItem
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

        private static IEnumerable<InnerBuildContext> GetInnerContexts(
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
                   select new InnerBuildContext(
                       new HostService(
                           parameters.Files.DefaultBaseDir,
                           from file in item
                           select Load(item.Key, parameters.Metadata, parameters.FileMetadata, file.file)
                           into model
                           where model != null
                           select model)
                       {
                           MarkdownService = markdownService,
                       },
                       item.Key,
                       templateProcessor);
        }

        /// <summary>
        /// Export xref map file.
        /// </summary>
        private static void ExportXRefMap(DocumentBuildParameters parameters, DocumentBuildContext context)
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
            YamlUtility.Serialize(
                Path.Combine(parameters.OutputBaseDir, XRefMapFileName),
                xrefMap);
            Logger.LogInfo("XRef map exported.");
        }

        private IMarkdownService CreateMarkdownService(DocumentBuildParameters parameters)
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
                });
        }

        private sealed class InnerBuildContext
        {
            public HostService HostService { get; }
            public IDocumentProcessor Processor { get; }
            public TemplateProcessor TemplateProcessor { get; }

            public InnerBuildContext(HostService hostService, IDocumentProcessor processor, TemplateProcessor templateProcessor)
            {
                HostService = hostService;
                Processor = processor;
                TemplateProcessor = templateProcessor;
            }
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
            public ManifestItem Item { get; }
            public FileModel FileModel { get; }
            public IDocumentProcessor Processor { get; }
            public TemplateBundle TemplateBundle { get; }
            public ManifestItemWithContext(ManifestItem item, FileModel model, IDocumentProcessor processor, TemplateBundle bundle)
            {
                Item = item;
                FileModel = model;
                Processor = processor;
                TemplateBundle = bundle;
            }
        }
    }
}
