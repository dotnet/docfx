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
    using System.Reflection;

    using Microsoft.DocAsCode.Plugins;
    using Microsoft.DocAsCode.Utility;

    public class DocumentBuilder
    {
        private static readonly CompositionHost Container = GetContainer();
        private static readonly RelativePath Root = (RelativePath)"~/";

        private static CompositionHost GetContainer()
        {
            var configuration = new ContainerConfiguration();
            configuration.WithAssembly(typeof(DocumentBuilder).Assembly);
            var pluginDir = Path.Combine(Path.GetDirectoryName(typeof(DocumentBuilder).Assembly.Location), "plugins");
            if (Directory.Exists(pluginDir))
            {
                foreach (var file in Directory.EnumerateFiles(pluginDir, "*.dll"))
                {
                    configuration.WithAssembly(Assembly.LoadFile(file));
                }
            }
            return configuration.CreateContainer();
        }

        public DocumentBuilder()
        {
            Container.SatisfyImports(this);
        }

        [ImportMany]
        internal IEnumerable<IDocumentProcessor> Processors { get; set; }

        public void Build(FileCollection files, string outputBaseDir)
        {
            if (outputBaseDir == null)
            {
                throw new ArgumentNullException(nameof(outputBaseDir));
            }
            if (!Path.IsPathRooted(outputBaseDir))
            {
                throw new ArgumentException("Output base directory must be rooted.", nameof(outputBaseDir));
            }
            Directory.CreateDirectory(outputBaseDir);
            var context = new DocumentBuildContext(outputBaseDir);
            foreach (var item in
                from file in files.EnumerateFiles()
                group file by (from processor in Processors
                               let priority = processor.GetProcessingPriority(file)
                               where priority != ProcessingPriority.NotSupportted
                               orderby priority descending
                               select processor).FirstOrDefault())
            {
                if (item.Key != null)
                {
                    BuildCore(item.Key, item, context);
                }
                else
                {
                    // todo : log warning: Cannot handle following file: ...
                }
            }
            Merge(outputBaseDir, context);
        }

        private void BuildCore(
            IDocumentProcessor processor,
            IEnumerable<FileAndType> files,
            DocumentBuildContext context)
        {
            using (var hostService = new HostService(
                from file in files
                select processor.Load(file)))
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
                            RenderMap(context, m, result);
                        }
                    }
                }
                finally
                {
                    m.Dispose();
                }
            }
        }

        private void RenderMap(
            DocumentBuildContext context,
            FileModel model,
            SaveResult result)
        {
            context.FileMap[Root + (RelativePath)model.OriginalFileAndType.File] = Root + (RelativePath)model.File;
            foreach (var uid in model.Uids)
            {
                context.UidMap[uid] = Root + (RelativePath)model.File;
            }
            context.Manifest.Add(new ManifestItem
            {
                DocumentType = result.DocumentType,
                ModelFile = result.ModelFile,
                ResourceFile = result.ResourceFile,
            });
        }

        private void Merge(string outputBaseDir, DocumentBuildContext context)
        {
            YamlUtility.Serialize(Path.Combine(outputBaseDir, ".docfx.manifest"), context.Manifest);
        }
    }
}
