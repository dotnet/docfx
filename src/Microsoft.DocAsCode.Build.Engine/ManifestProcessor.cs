// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.Engine
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.IO;
    using System.Linq;

    using Newtonsoft.Json.Linq;

    using Microsoft.DocAsCode.Common;
    using Microsoft.DocAsCode.Plugins;

    internal class ManifestProcessor
    {
        private List<ManifestItemWithContext> _manifestWithContext;
        private DocumentBuildContext _context;
        private TemplateProcessor _templateProcessor;

        public ManifestProcessor(List<ManifestItemWithContext> manifestWithContext, DocumentBuildContext context, TemplateProcessor templateProcessor)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _templateProcessor = templateProcessor ?? throw new ArgumentNullException(nameof(templateProcessor));
            _manifestWithContext = manifestWithContext ?? throw new ArgumentNullException(nameof(manifestWithContext));
        }

        public void Process()
        {
            using (new LoggerPhaseScope("UpdateContext", LogLevel.Verbose))
            {
                UpdateContext();
            }

            // Run getOptions from Template
            using (new LoggerPhaseScope("FeedOptions", LogLevel.Verbose))
            {
                FeedOptions();
            }

            // Template can feed back xref map, actually, the anchor # location can only be determined in template
            using (new LoggerPhaseScope("FeedXRefMap", LogLevel.Verbose))
            {
                FeedXRefMap();
            }

            using (new LoggerPhaseScope("UpdateHref", LogLevel.Verbose))
            {
                UpdateHref();
            }

            // Afterwards, m.Item.Model.Content is always IDictionary
            using (new LoggerPhaseScope("ApplySystemMetadata", LogLevel.Verbose))
            {
                ApplySystemMetadata();
            }

            foreach (var item in ProcessTemplate())
            {
                _context.ManifestItems.Add(item);
            }
        }

        #region Private

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
            },
            _context.MaxParallelism);
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
                    if (m.Options.Bookmarks == null) return;
                    foreach (var pair in m.Options.Bookmarks)
                    {
                        _context.RegisterInternalXrefSpecBookmark(pair.Key, pair.Value);
                    }
                }
            },
            _context.MaxParallelism);
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
            },
            _context.MaxParallelism);
        }

        private void ApplySystemMetadata()
        {
            Logger.LogVerbose("Applying system metadata to manifest...");

            // Add system attributes
            var systemMetadataGenerator = new SystemMetadataGenerator(_context);

            _manifestWithContext.RunAll(m =>
            {
                if (m.FileModel.Type == DocumentType.Resource)
                {
                    return;
                }
                using (new LoggerFileScope(m.FileModel.LocalPathFromRoot))
                {
                    Logger.LogDiagnostic("Generating system metadata...");

                    // TODO: use weak type for system attributes from the beginning
                    var systemAttrs = systemMetadataGenerator.Generate(m.Item);
                    var metadata = (JObject)ConvertToObjectHelper.ConvertStrongTypeToJObject(systemAttrs);
                    // Change file model to weak type
                    var model = m.Item.Model.Content;
                    var modelAsObject = (JToken)ConvertToObjectHelper.ConvertStrongTypeToJObject(model);
                    if (modelAsObject is JObject)
                    {
                        foreach (var pair in (JObject)modelAsObject)
                        {
                            // Overwrites the existing system metadata if the same key is defined in document model
                            metadata[pair.Key] = pair.Value;
                        }
                    }
                    else
                    {
                        Logger.LogWarning("Input model is not an Object model, it will be wrapped into an Object model. Please use --exportRawModel to view the wrapped model");
                        metadata["model"] = modelAsObject;
                    }

                    // Append system metadata to model
                    m.Item.Model.Serializer = null;
                    m.Item.Model.Content = metadata;
                }
            },
            _context.MaxParallelism);
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
            var sharedObjects = new ConcurrentDictionary<string, object>();
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
            },
            _context.MaxParallelism);

            metadata["_shared"] = sharedObjects;
            return metadata;
        }

        private List<ManifestItem> ProcessTemplate()
        {
            // Register global variables after href are all updated
            IDictionary<string, object> globalVariables;
            using (new LoggerPhaseScope("FeedGlobalVariables", LogLevel.Verbose))
            {
                globalVariables = FeedGlobalVariables();
            }

            // processor to add global variable to the model
            return _templateProcessor.Process(_manifestWithContext.Select(s => s.Item).ToList(), _context.ApplyTemplateSettings, globalVariables);
        }

        #endregion
    }
}
