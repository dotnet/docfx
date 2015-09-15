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

    public class DocumentBuilder
    {
        private static readonly CompositionHost Container = GetContainer();

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
                    BuildCore(item.Key, item, outputBaseDir);
                }
                else
                {
                    // todo : log warning: Cannot handle following file: ...
                }
            }
        }

        private void BuildCore(IDocumentProcessor processor, IEnumerable<FileAndType> files, string outputBaseDir)
        {
            using (var hostService = new HostService(
                from file in files
                let m = processor.Load(file)
                let _ = m.Serialize()
                select m))
            {
                Prebuild(processor, hostService);
                BuildArticle(processor, hostService);
                Postbuild(processor, hostService);
                Save(processor, hostService, outputBaseDir);
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

        private void Save(IDocumentProcessor processor, HostService hostService, string outputBaseDir)
        {
            foreach (var m in hostService.Models)
            {
                try
                {
                    if (m.Type != DocumentType.Override)
                    {
                        m.BaseDir = outputBaseDir;
                        processor.Save(m);
                    }
                }
                finally
                {
                    m.Dispose();
                }
            }
        }
    }
}
