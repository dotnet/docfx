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

        private const string ManifestFileName = ".manifest";

        private const string RawModelExtension = ".raw.model.json";
        private const string ViewModelExtension = ".view.model.json";

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
                    parameters.ExternalReferencePackages,
                    parameters.TemplateCollection
                    );
                Logger.LogVerbose("Start building document...");
                IEnumerable<InnerBuildContext> innerContexts = Enumerable.Empty<InnerBuildContext>();
                try
                {
                    innerContexts = GetInnerContexts(parameters, Processors).ToList();
                    foreach (var item in innerContexts)
                    {
                        BuildCore(item.HostService, item.Processor, context);
                    }

                    UpdateContext(context);

                    foreach (var item in innerContexts)
                    {
                        UpdateHref(item.HostService, item.Processor, context);
                    }

                    if (parameters.ExportRawModel)
                    {
                        Logger.LogInfo($"Exporting {context.Manifest.Count} raw model(s)...");
                        foreach(var item in context.Manifest)
                        {
                            var model = item.Model;
                            if (model.Content != null)
                            {
                                var rawModelPath = Path.Combine(model.BaseDir, Path.ChangeExtension(model.File, RawModelExtension));
                                JsonUtility.Serialize(rawModelPath, model.Content);
                            }
                        }
                    }

                    using (new LoggerPhaseScope("Apply Templates"))
                    {
                        Logger.LogInfo($"Applying templates to {context.Manifest.Count} model(s)...");
                        Transform(context, parameters.TemplateCollection, parameters.ExportViewModel);
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

                Logger.LogInfo($"Building {context.Manifest.Count} file(s) completed.");
            }
        }

        private void Cleanup(HostService hostService)
        {
            hostService.Models.RunAll(m => m.Dispose());
        }

        private void Transform(DocumentBuildContext context, TemplateCollection templateCollection, bool exportMetadata)
        {
            if (templateCollection == null || templateCollection.Count == 0)
            {
                Logger.LogWarning("No template is found.");
            }
            else
            {
                Logger.LogVerbose("Start applying template...");
            }

            var outputDirectory = context.BuildOutputFolder;

            List<TemplateManifestItem> manifest = new List<TemplateManifestItem>();

            // Model can apply multiple template with different extension, so append the view model extension instead of change extension
            Func<string, string> metadataPathProvider = (s) => { return s + ViewModelExtension; };
            foreach (var item in context.Manifest)
            {
                var manifestItem = TemplateProcessor.Transform(context, item, templateCollection, outputDirectory, exportMetadata, metadataPathProvider);
                manifest.Add(manifestItem);
            }

            // Save manifest
            var manifestPath = Path.Combine(outputDirectory, ManifestFileName);
            JsonUtility.Serialize(manifestPath, manifest);
            Logger.Log(LogLevel.Verbose, $"Manifest file saved to {manifestPath}.");
        }

        private void BuildCore(HostService hostService, IDocumentProcessor processor, DocumentBuildContext context)
        {
            Logger.LogVerbose($"Plug-in {processor.Name}: Loading document...");
            hostService.SourceFiles = context.AllSourceFiles;
            foreach (var m in hostService.Models)
            {
                if (m.LocalPathFromRepoRoot == null)
                {
                    m.LocalPathFromRepoRoot = Path.Combine(m.BaseDir, m.File);
                }
            }
            Logger.LogInfo($"Building {hostService.Models.Count} file(s) with {processor.Name}...");
            Logger.LogVerbose($"Plug-in {processor.Name}: Preprocessing...");
            Prebuild(processor, hostService);
            Logger.LogVerbose($"Plug-in {processor.Name}: Building...");
            BuildArticle(processor, hostService);
            Logger.LogVerbose($"Plug-in {processor.Name}: Postprocessing...");
            Postbuild(processor, hostService);
            Logger.LogVerbose($"Plug-in {processor.Name}: Saving...");
            Save(processor, hostService, context);
        }

        private void UpdateHref(HostService hostService, IDocumentProcessor processor, DocumentBuildContext context)
        {
            hostService.Models.RunAll(
                m =>
                {
                    using (new LoggerFileScope(m.OriginalFileAndType.File))
                    {
                        Logger.LogVerbose($"Plug-in {processor.Name}: Updating href...");
                        processor.UpdateHref(m, context);
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
                        using (new LoggerFileScope(m.OriginalFileAndType.File))
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
                    });
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

        private void Save(IDocumentProcessor processor, HostService hostService, DocumentBuildContext context)
        {
            hostService.Models.RunAll(
                m =>
                {
                    if (m.Type != DocumentType.Override)
                    {
                        using (new LoggerFileScope(m.OriginalFileAndType.File))
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
                                m.File = TemplateProcessor.UpdateFilePath(m.File, result.DocumentType, context.TemplateCollection);
                                result.ModelFile = TemplateProcessor.UpdateFilePath(result.ModelFile, result.DocumentType, context.TemplateCollection);
                                HandleSaveResult(context, hostService, m, result);
                            }
                        }
                    }
                });
        }

        private void UpdateContext(DocumentBuildContext context)
        {
            context.SetExternalXRefSpec();
        }

        private void HandleSaveResult(
            DocumentBuildContext context,
            HostService hostService,
            FileModel model,
            SaveResult result)
        {
            context.FileMap[((RelativePath)model.OriginalFileAndType.File).GetPathFromWorkingFolder()] = ((RelativePath)model.File).GetPathFromWorkingFolder();
            DocumentException.RunAll(
                () => CheckFileLink(hostService, model, result),
                () => HandleUids(context, model, result),
                () => HandleToc(context, result),
                () => RegisterXRefSpec(context, model, result),
                () => RegisterManifest(context, model, result));
        }

        private static void CheckFileLink(HostService hostService, FileModel model, SaveResult result)
        {
            result.LinkToFiles.RunAll(
                fileLink =>
                {
                    if (!hostService.SourceFiles.ContainsKey(fileLink))
                    {
                        var message = $"Invalid file link({fileLink}) in file \"{model.LocalPathFromRepoRoot}\"";
                        Logger.LogWarning(message, file: model.LocalPathFromRepoRoot);
                    }
                });
        }

        private static void HandleUids(DocumentBuildContext context, FileModel model, SaveResult result)
        {
            if (result.LinkToUids.Count > 0)
            {
                context.XRef.UnionWith(result.LinkToUids);
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

        private void RegisterXRefSpec(DocumentBuildContext context, FileModel model, SaveResult result)
        {
            foreach (var spec in result.XRefSpecs)
            {
                if (!string.IsNullOrWhiteSpace(spec?.Uid))
                {
                    XRefSpec xref;
                    if (context.XRefSpecMap.TryGetValue(spec.Uid, out xref))
                    {
                        Logger.LogWarning($"Uid({spec.Uid}) has already been defined in {((RelativePath)xref.Href).RemoveWorkingFolder()}.", file: model.LocalPathFromRepoRoot);
                    }
                    else
                    {
                        context.XRefSpecMap[spec.Uid] = spec.ToReadOnly();
                    }
                }
            }
        }

        private static void RegisterManifest(DocumentBuildContext context, FileModel model, SaveResult result)
        {
            context.Manifest.Add(new ManifestItem
            {
                DocumentType = result.DocumentType,
                ModelFile = result.ModelFile,
                ResourceFile = result.ResourceFile,
                OriginalFile = model.OriginalFileAndType.File,
                // TODO: What is API doc's LocalPathToRepo? => defined in ManagedReferenceDocumentProcessor
                LocalPathFromRepoRoot = model.LocalPathFromRepoRoot,
                Model = model
            });
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

        private static IEnumerable<InnerBuildContext> GetInnerContexts(DocumentBuildParameters parameters, IEnumerable<IDocumentProcessor> processors)
        {
            var filesGroupedByProcessor =
                    from file in parameters.Files.EnumerateFiles()
                    group file by (from processor in processors
                                   let priority = processor.GetProcessingPriority(file)
                                   where priority != ProcessingPriority.NotSupportted
                                   orderby priority descending
                                   select processor).FirstOrDefault();
            var toHandleItems = filesGroupedByProcessor.Where(s => s.Key != null);
            var notToHandleItems = filesGroupedByProcessor.Where(s => s.Key == null);
            foreach (var item in notToHandleItems)
            {
                var sb = new StringBuilder();
                sb.AppendLine("Cannot handle following file:");
                foreach (var f in item)
                {
                    sb.Append("\t");
                    sb.AppendLine(f.File);
                }
                Logger.LogWarning(sb.ToString());
            }

            foreach (var item in toHandleItems)
            {
                yield return new InnerBuildContext(new HostService(
                    from file in item
                    select Load(item.Key, parameters.Metadata, parameters.FileMetadata, file)),
                    item.Key);
            }
        }

        private sealed class InnerBuildContext
        {
            public HostService HostService { get; }
            public IDocumentProcessor Processor { get; }

            public InnerBuildContext(HostService hostService, IDocumentProcessor processor)
            {
                HostService = hostService;
                Processor = processor;
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
    }
}
