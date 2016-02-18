// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.EntityModel.Builders
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
    using Microsoft.DocAsCode.Plugins;
    using Microsoft.DocAsCode.Utility;

    public class DocumentBuilder : IDisposable
    {
        public const string PhaseName = "Build Document";

        private const int Parallelism = 16;

        private static readonly Assembly[] DefaultAssemblies = { typeof(DocumentBuilder).Assembly };

        private CompositionHost GetContainer(IEnumerable<Assembly> assemblies)
        {
            var configuration = new ContainerConfiguration();
            foreach (var assembly in assemblies)
            {
                configuration.WithAssembly(assembly);
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
                GetContainer(DefaultAssemblies.Union(assemblies ?? new Assembly[0])).SatisfyImports(this);
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
            if (parameters.Metadata == null)
            {
                parameters.Metadata = ImmutableDictionary<string, object>.Empty;
            }

            using (new LoggerPhaseScope(PhaseName))
            {
                Directory.CreateDirectory(parameters.OutputBaseDir);
                var context = new DocumentBuildContext(
                    Path.Combine(Environment.CurrentDirectory, parameters.OutputBaseDir),
                    parameters.Files.EnumerateFiles(),
                    parameters.ExternalReferencePackages
                    );
                Logger.LogVerbose("Start building document...");
                IEnumerable<InnerBuildContext> innerContexts = Enumerable.Empty<InnerBuildContext>();
                try
                {
                    using (var processor = parameters.TemplateManager?.GetTemplateProcessor())
                    {
                        innerContexts = GetInnerContexts(parameters, Processors, processor).ToList();
                        var manifest = new List<ManifestItemWithContext>();
                        foreach (var item in innerContexts)
                        {
                            manifest.AddRange(BuildCore(item, context));
                        }

                        // Use manifest from now on
                        UpdateContext(context);
                        UpdateHref(manifest, context);

                        TemplateProcessor.Transform(processor, manifest.Select(s => s.Item).ToList(), context, parameters.ApplyTemplateSettings);

                        Logger.LogInfo($"Building {manifest.Count} file(s) completed.");
                    }
                }
                finally
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

        private void Cleanup(HostService hostService)
        {
            hostService.Models.RunAll(m => m.Dispose());
        }

        private IEnumerable<ManifestItemWithContext> BuildCore(InnerBuildContext buildContext, DocumentBuildContext context)
        {
            var processor = buildContext.Processor;
            var hostService = buildContext.HostService;
            Logger.LogVerbose($"Plug-in {processor.Name}: Loading document...");
            hostService.SourceFiles = context.AllSourceFiles;
            foreach (var m in hostService.Models)
            {
                if (m.LocalPathFromRepoRoot == null)
                {
                    m.LocalPathFromRepoRoot = Path.Combine(m.BaseDir, m.File).ToDisplayPath();
                }
            }
            var steps = string.Join("=>", processor.BuildSteps.OrderBy(step => step.BuildOrder).Select(s => s.Name));
            Logger.LogInfo($"Building {hostService.Models.Count} file(s) in {processor.Name}({steps})...");
            Logger.LogVerbose($"Plug-in {processor.Name}: Preprocessing...");
            Prebuild(processor, hostService);
            Logger.LogVerbose($"Plug-in {processor.Name}: Building...");
            BuildArticle(processor, hostService);
            Logger.LogVerbose($"Plug-in {processor.Name}: Postprocessing...");
            Postbuild(processor, hostService);
            Logger.LogVerbose($"Plug-in {processor.Name}: Generating manifest...");
            return ExportManifest(buildContext, context);
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
                Logger.LogVerbose($"Plug-in {processor.Name}: Loading...");

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

        private void Prebuild(IDocumentProcessor processor, HostService hostService)
        {
            RunBuildSteps(
                processor.BuildSteps,
                buildStep =>
                {
                    Logger.LogVerbose($"Plug-in {processor.Name}, build step {buildStep.Name}: Preprocessing...");
                    var models = buildStep.Prebuild(hostService.Models, hostService);
                    if (!object.ReferenceEquals(models, hostService.Models))
                    {
                        Logger.LogVerbose($"Plug-in {processor.Name}, build step {buildStep.Name}: Reloading models...");
                        hostService.Reload(models);
                    }
                });
        }

        private void BuildArticle(IDocumentProcessor processor, HostService hostService)
        {
            hostService.Models.RunAll(
                m =>
                {
                    using (new LoggerFileScope(m.LocalPathFromRepoRoot))
                    {
                        Logger.LogVerbose($"Plug-in {processor.Name}: Building...");
                        RunBuildSteps(
                            processor.BuildSteps,
                            buildStep =>
                            {
                                Logger.LogVerbose($"Plug-in {processor.Name}, build step {buildStep.Name}: Building...");
                                buildStep.Build(m, hostService);
                            });
                    }
                },
                Parallelism);
        }

        private void Postbuild(IDocumentProcessor processor, HostService hostService)
        {
            RunBuildSteps(
                processor.BuildSteps,
                buildStep =>
                {
                    Logger.LogVerbose($"Plug-in {processor.Name}, build step {buildStep.Name}: Postprocessing...");
                    buildStep.Postbuild(hostService.Models, hostService);
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
                if (m.Type != DocumentType.Override)
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
                            if (templateProcessor != null)
                            {
                                m.File = templateProcessor.UpdateFileExtension(m.File, result.DocumentType);
                                result.ModelFile = templateProcessor.UpdateFileExtension(result.ModelFile, result.DocumentType);
                            }

                            var item = HandleSaveResult(context, hostService, m, result);
                            manifestItems.Add(new ManifestItemWithContext(item, m, processor));
                        }
                    }
                }
            });
            return manifestItems;
        }

        private void UpdateContext(DocumentBuildContext context)
        {
            context.SetExternalXRefSpec();
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
        }

        private static ManifestItem GetManifestItem(DocumentBuildContext context, FileModel model, SaveResult result)
        {
            return new ManifestItem
            {
                DocumentType = result.DocumentType,
                ModelFile = result.ModelFile,
                ResourceFile = result.ResourceFile,
                Key = model.Key,
                // TODO: What is API doc's LocalPathToRepo? => defined in ManagedReferenceDocumentProcessor
                LocalPathFromRepoRoot = model.LocalPathFromRepoRoot,
                Model = model.ModelWithCache,
                InputFolder = model.OriginalFileAndType.BaseDir
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

        private static IEnumerable<InnerBuildContext> GetInnerContexts(DocumentBuildParameters parameters, IEnumerable<IDocumentProcessor> processors, TemplateProcessor templateProcessor)
        {
            var k = from fileItem in (
                from file in parameters.Files.EnumerateFiles()
                from p in (from processor in processors
                           let priority = processor.GetProcessingPriority(file)
                           where priority != ProcessingPriority.NotSupportted
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

            return from item in toHandleItems.AsParallel().WithDegreeOfParallelism(Parallelism)
                   select new InnerBuildContext(
                       new HostService(
                           from file in item
                           select Load(item.Key, parameters.Metadata, parameters.FileMetadata, file.file)
                           into model
                           where model != null
                           select model),
                       item.Key,
                       templateProcessor);
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
            public ManifestItemWithContext(ManifestItem item, FileModel model, IDocumentProcessor processor)
            {
                Item = item;
                FileModel = model;
                Processor = processor;
            }
        }
    }
}
