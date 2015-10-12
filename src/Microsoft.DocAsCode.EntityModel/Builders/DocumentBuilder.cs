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

    using Microsoft.DocAsCode.EntityModel.ViewModels;
    using Microsoft.DocAsCode.Plugins;
    using Microsoft.DocAsCode.Utility;

    public class DocumentBuilder
    {
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
            GetContainer(assemblies).SatisfyImports(this);
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

            Directory.CreateDirectory(parameters.OutputBaseDir);
            var context = new DocumentBuildContext(
                Path.Combine(Environment.CurrentDirectory, parameters.OutputBaseDir),
                parameters.ExternalReferencePackages);
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
                    ParseResult.WriteToConsole(ResultLevel.Warning, sb.ToString());
                }
            }

            context.SerializeTo(parameters.OutputBaseDir);
        }

        private void BuildCore(
            IDocumentProcessor processor,
            IEnumerable<FileAndType> files,
            ImmutableDictionary<string, object> metadata,
            DocumentBuildContext context)
        {
            using (var hostService = new HostService(
                from file in files
                select processor.Load(file, metadata)))
            {
                Prebuild(processor, hostService);
                BuildArticle(processor, hostService);
                Postbuild(processor, hostService);
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
            foreach (var m in hostService.Models)
            {
                processor.Build(m, hostService);
                m.Serialize();
            }
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
            foreach (var m in hostService.Models)
            {
                try
                {
                    if (m.Type != DocumentType.Override)
                    {
                        m.BaseDir = context.BuildOutputFolder;
                        var result = processor.Save(m);
                        if (result != null)
                        {
                            HandleSaveResult(context, m, result);
                        }
                    }
                }
                finally
                {
                    m.Dispose();
                }
            }
        }

        private void HandleSaveResult(
            DocumentBuildContext context,
            FileModel model,
            SaveResult result)
        {
            context.FileMap[Root + (RelativePath)model.OriginalFileAndType.File] = Root + (RelativePath)model.File;
            foreach (var uid in model.Uids)
            {
                context.UidMap[uid] = Root + (RelativePath)model.File;
            }
            if (result.LinkToUids.Length > 0)
            {
                context.XRef.UnionWith(result.LinkToUids);
            }
            if ((result.TocMap?.Count ?? 0) > 0)
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

            context.Manifest.Add(new ManifestItem
            {
                DocumentType = result.DocumentType,
                ModelFile = result.ModelFile,
                ResourceFile = result.ResourceFile,
                // TODO: What is API doc's originalFile?
                OriginalFile = model.OriginalFileAndType.File,
                RelativeBaseDir = PathUtility.MakeRelativePath(model.OriginalFileAndType.RootDir, model.OriginalFileAndType.BaseDir),
            });
        }
    }
}
