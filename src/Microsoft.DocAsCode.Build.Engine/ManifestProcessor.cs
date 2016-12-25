// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.Engine
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.IO;
    using System.Linq;

    using Microsoft.DocAsCode.Common;
    using Microsoft.DocAsCode.Plugins;

    internal class ManifestProcessor
    {
        private List<ManifestItemWithContext> _manifestWithContext;
        private DocumentBuildContext _context;
        private TemplateProcessor _templateProcessor;

        public ManifestProcessor(IEnumerable<HostService> hostServices, DocumentBuildContext context, TemplateProcessor templateProcessor)
        {
            if (hostServices == null)
            {
                throw new ArgumentNullException(nameof(hostServices));
            }
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }
            if (templateProcessor == null)
            {
                throw new ArgumentNullException(nameof(templateProcessor));
            }
            _context = context;
            _templateProcessor = templateProcessor;
            Init(hostServices);
        }

        public void Process()
        {
            using (new LoggerPhaseScope("UpdateContext", true))
            {
                UpdateContext();
            }

            // Run getOptions from Template
            using (new LoggerPhaseScope("FeedOptions", true))
            {
                FeedOptions();
            }

            // Template can feed back xref map, actually, the anchor # location can only be determined in template
            using (new LoggerPhaseScope("FeedXRefMap", true))
            {
                FeedXRefMap();
            }

            using (new LoggerPhaseScope("UpdateHref", true))
            {
                UpdateHref();
            }

            // Afterwards, m.Item.Model.Content is always IDictionary
            using (new LoggerPhaseScope("ApplySystemMetadata", true))
            {
                ApplySystemMetadata();
            }

            foreach (var item in ProcessTemplate())
            {
                _context.ManifestItems.Add(item);
            }
        }

        #region Private

        private void Init(IEnumerable<HostService> hostServices)
        {
            _manifestWithContext = new List<ManifestItemWithContext>();
            foreach (var hostService in hostServices)
            {
                using (new LoggerPhaseScope(hostService.Processor.Name, true))
                {
                    _manifestWithContext.AddRange(ExportManifest(hostService, _context));
                }
            }
        }

        private static IEnumerable<ManifestItemWithContext> ExportManifest(HostService hostService, DocumentBuildContext context)
        {
            var manifestItems = new List<ManifestItemWithContext>();
            using (new LoggerPhaseScope("Save", true))
            {
                hostService.Models.RunAll(m =>
                {
                    if (m.Type != DocumentType.Overwrite)
                    {
                        using (new LoggerFileScope(m.LocalPathFromRoot))
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

        private static InternalManifestItem HandleSaveResult(
            DocumentBuildContext context,
            HostService hostService,
            FileModel model,
            SaveResult result)
        {
            context.SetFilePath(model.Key, ((RelativePath)model.File).GetPathFromWorkingFolder());
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
                    ImmutableList<LinkSourceInfo> list;
                    if (result.FileLinkSources.TryGetValue(fileLink, out list))
                    {
                        foreach (var fileLinkSourceFile in list)
                        {
                            Logger.LogWarning($"Invalid file link:({fileLinkSourceFile.Target}{fileLinkSourceFile.Anchor}).", null, fileLinkSourceFile.SourceFile, fileLinkSourceFile.LineNumber.ToString());
                        }
                    }
                    else
                    {
                        Logger.LogWarning($"Invalid file link:({fileLink}).");
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

        private static void RegisterXRefSpec(DocumentBuildContext context, SaveResult result)
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
                        context.RegisterInternalXrefSpec(spec);
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

        private void UpdateContext()
        {
            _context.ResolveExternalXRefSpec();
        }

        private void FeedOptions()
        {
            Logger.LogVerbose("Feeding options from template...");
            _manifestWithContext.RunAll(m =>
            {
                if (m.TemplateBundle == null)
                {
                    return;
                }

                using (new LoggerFileScope(m.FileModel.LocalPathFromRoot))
                {
                    Logger.LogDiagnostic($"Feed options from template for {m.Item.DocumentType}...");
                    m.Options = m.TemplateBundle.GetOptions(m.Item, _context);
                }
            });
        }

        private void FeedXRefMap()
        {
            Logger.LogVerbose("Feeding xref map...");
            _manifestWithContext.RunAll(m =>
            {
                if (m.TemplateBundle == null)
                {
                    return;
                }

                using (new LoggerFileScope(m.FileModel.LocalPathFromRoot))
                {
                    Logger.LogDiagnostic($"Feed xref map from template for {m.Item.DocumentType}...");
                    // TODO: use m.Options.Bookmarks directly after all templates report bookmarks
                    var bookmarks = m.Options.Bookmarks ?? m.FileModel.Bookmarks;
                    foreach (var pair in bookmarks)
                    {
                        _context.RegisterInternalXrefSpecBookmark(pair.Key, pair.Value);
                    }
                }
            });
        }

        private void UpdateHref()
        {
            Logger.LogVerbose("Updating href...");
            _manifestWithContext.RunAll(m =>
            {
                using (new LoggerFileScope(m.FileModel.LocalPathFromRoot))
                {
                    Logger.LogDiagnostic($"Plug-in {m.Processor.Name}: Updating href...");
                    m.Processor.UpdateHref(m.FileModel, _context);

                    // reset model after updating href
                    m.Item.Model = m.FileModel.ModelWithCache;
                }
            });
        }

        private void ApplySystemMetadata()
        {
            Logger.LogVerbose("Applying system metadata to manifest...");

            // Add system attributes
            var systemMetadataGenerator = new SystemMetadataGenerator(_context);

            _manifestWithContext.RunAll(m =>
            {
                using (new LoggerFileScope(m.FileModel.LocalPathFromRoot))
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

        private IDictionary<string, object> FeedGlobalVariables()
        {
            Logger.LogVerbose("Feeding global variables from template...");

            // E.g. we can set TOC model to be globally shared by every data model
            // Make sure it is single thread
            var initialGlobalVariables = _templateProcessor.Tokens;
            IDictionary<string, object> metadata = initialGlobalVariables == null ?
                new Dictionary<string, object>() :
                initialGlobalVariables.ToDictionary(pair => pair.Key, pair => (object)pair.Value);
            var sharedObjects = new Dictionary<string, object>();
            _manifestWithContext.RunAll(m =>
            {
                if (m.TemplateBundle == null)
                {
                    return;
                }

                using (new LoggerFileScope(m.FileModel.LocalPathFromRoot))
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

        private List<ManifestItem> ProcessTemplate()
        {
            // Register global variables after href are all updated
            IDictionary<string, object> globalVariables;
            using (new LoggerPhaseScope("FeedGlobalVariables", true))
            {
                globalVariables = FeedGlobalVariables();
            }

            // processor to add global variable to the model
            return _templateProcessor.Process(_manifestWithContext.Select(s => s.Item).ToList(), _context, _context.ApplyTemplateSettings, globalVariables);
        }

        #endregion

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
