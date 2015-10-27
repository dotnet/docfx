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

    using Microsoft.DocAsCode.Plugins;
    using Microsoft.DocAsCode.Utility;

    public class DocumentBuilder
    {
        private const string Phase = "Build Document";
        private static readonly RelativePath Root = (RelativePath)"~/";

        private CompositionHost GetContainer(IEnumerable<Assembly> assemblies)
        {
            var configuration = new ContainerConfiguration();
            foreach (var assembly in assemblies)
            {
                configuration.WithAssembly(assembly);
            }
            return configuration.CreateContainer();
        }

        public DocumentBuilder()
            : this(new[] { typeof(DocumentBuilder).Assembly })
        {
        }

        public DocumentBuilder(IEnumerable<Assembly> assemblies)
        {
            using (new LoggerPhaseScope(Phase))
            {
                Logger.LogInfo("Loading plug-in...");
                GetContainer(assemblies).SatisfyImports(this);
                Logger.LogInfo($"Plug-in loaded ({string.Join(", ", from p in Processors select p.Name)})");
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

            using (new LoggerPhaseScope(Phase))
            {
                Directory.CreateDirectory(parameters.OutputBaseDir);
                var context = new DocumentBuildContext(
                    Path.Combine(Environment.CurrentDirectory, parameters.OutputBaseDir),
                    parameters.Files.EnumerateFiles(),
                    parameters.ExternalReferencePackages);
                Logger.LogInfo("Start building document ...");
                foreach (var item in
                    from file in parameters.Files.EnumerateFiles()
                    group file by (from processor in Processors
                                   let priority = processor.GetProcessingPriority(file)
                                   where priority != ProcessingPriority.NotSupportted
                                   orderby priority descending
                                   select processor).FirstOrDefault())
                {
                    if (item.Key != null)
                    {
                        BuildCore(item.Key, item, parameters.Metadata ?? ImmutableDictionary<string, object>.Empty, context);
                    }
                    else
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
                }

                context.SerializeTo(parameters.OutputBaseDir);
                Logger.LogInfo("Building document completed.");
            }
        }

        private void BuildCore(
            IDocumentProcessor processor,
            IEnumerable<FileAndType> files,
            ImmutableDictionary<string, object> metadata,
            DocumentBuildContext context)
        {
            Logger.LogInfo($"Plug-in {processor.Name}: Loading document...");
            using (var hostService = new HostService(
                from file in files
                select processor.Load(file, metadata)))
            {
                hostService.SourceFiles = context.AllSourceFiles;
                foreach (var m in hostService.Models)
                {
                    if (m.LocalPathFromRepoRoot == null)
                    {
                        m.LocalPathFromRepoRoot = Path.Combine(m.BaseDir, m.File);
                    }
                }
                Logger.LogInfo($"Plug-in {processor.Name}: Document loaded (count = {hostService.Models.Count}).");
                Logger.LogInfo($"Plug-in {processor.Name}: Preprocessing...");
                Prebuild(processor, hostService);
                Logger.LogInfo($"Plug-in {processor.Name}: Building...");
                BuildArticle(processor, hostService);
                Logger.LogInfo($"Plug-in {processor.Name}: Postprocessing...");
                Postbuild(processor, hostService);
                Logger.LogInfo($"Plug-in {processor.Name}: Saving...");
                Save(processor, hostService, context);
            }
        }

        private void Prebuild(IDocumentProcessor processor, HostService hostService)
        {
            var models = processor.Prebuild(hostService.Models, hostService);
            if (!object.ReferenceEquals(models, hostService.Models))
            {
                hostService.Reload(models);
            }
        }

        private void BuildArticle(IDocumentProcessor processor, HostService hostService)
        {
            hostService.Models.RunAll(m => processor.Build(m, hostService));
        }

        private void Postbuild(IDocumentProcessor processor, HostService hostService)
        {
            var models = processor.Postbuild(hostService.Models, hostService);
            if (!object.ReferenceEquals(models, hostService.Models))
            {
                hostService.Reload(models);
            }
        }

        private void Save(IDocumentProcessor processor, HostService hostService, DocumentBuildContext context)
        {
            hostService.Models.RunAll(
                m =>
                {
                    try
                    {
                        if (m.Type != DocumentType.Override)
                        {
                            m.BaseDir = context.BuildOutputFolder;
                            var result = processor.Save(m);
                            if (result != null)
                            {
                                HandleSaveResult(context, hostService, m, result);
                            }
                        }
                    }
                    finally
                    {
                        m.Dispose();
                    }
                });
        }

        private void HandleSaveResult(
            DocumentBuildContext context,
            HostService hostService,
            FileModel model,
            SaveResult result)
        {
            context.FileMap[Root + (RelativePath)model.OriginalFileAndType.File] = Root + (RelativePath)model.File;
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
                    if (!hostService.SourceFiles.Contains(fileLink))
                    {
                        if (fileLink.StartsWith("~/")) fileLink = fileLink.Substring(2);
                        var message = $"Invalid file link({fileLink}) in file \"{model.LocalPathFromRepoRoot}\"";
                        Logger.LogError(message, file: model.LocalPathFromRepoRoot);
                        throw new DocumentException(message);
                    }
                });
        }

        private static void HandleUids(DocumentBuildContext context, FileModel model, SaveResult result)
        {
            foreach (var uid in model.Uids)
            {
                context.UidMap[uid] = Root + (RelativePath)model.File;
            }
            if (result.LinkToUids.Length > 0)
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
                    if (context.XRefSpecMap.ContainsKey(spec.Uid))
                    {
                        Logger.LogWarning($"Uid({spec.Uid}) duplicated.", file: model.LocalPathFromRepoRoot);
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
                LocalPathFromRepoRoot = model.LocalPathFromRepoRoot
            });
        }
    }
}
