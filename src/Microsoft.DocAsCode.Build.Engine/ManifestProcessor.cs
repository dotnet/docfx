// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.Engine
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;


    using Microsoft.DocAsCode.Common;
    using Microsoft.DocAsCode.Plugins;

    internal class ManifestProcessor
    {
        private readonly List<ManifestItemWithContext> _manifestWithContext;
        private readonly DocumentBuildContext _context;
        private readonly TemplateProcessor _templateProcessor;
        private readonly IDictionary<string, object> _globalMetadata;

        public ManifestProcessor(List<ManifestItemWithContext> manifestWithContext, DocumentBuildContext context, TemplateProcessor templateProcessor)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _templateProcessor = templateProcessor ?? throw new ArgumentNullException(nameof(templateProcessor));
            _manifestWithContext = manifestWithContext ?? throw new ArgumentNullException(nameof(manifestWithContext));

            // E.g. we can set TOC model to be globally shared by every data model
            // Make sure it is single thread
            _globalMetadata = _templateProcessor.Tokens?.ToDictionary(pair => pair.Key, pair => (object)pair.Value)
                ?? new Dictionary<string, object>();
        }

        public void Process()
        {
            using (new LoggerPhaseScope("UpdateContext", LogLevel.Verbose))
            {
                UpdateContext();
            }

            // Afterwards, m.Item.Model.Content is always IDictionary
            using (new LoggerPhaseScope("NormalizeToObject", LogLevel.Verbose))
            {
                NormalizeToObject();
            }

            // Run getOptions from Template and feed options back to context
            // Template can feed back xref map, actually, the anchor # location can only be determined in template
            using (new LoggerPhaseScope("FeedOptions", LogLevel.Verbose))
            {
                FeedOptions();
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

        private void NormalizeToObject()
        {
            Logger.LogVerbose("Normalizing all the object to week type");

            _manifestWithContext.RunAll(m =>
            {
                using (new LoggerFileScope(m.FileModel.LocalPathFromRoot))
                {
                    var model = m.Item.Model.Content;
                    // Change file model to weak type
                    // Go through the convert even if it is IDictionary as the inner object might be of strong type
                    var modelAsObject = model == null ? new Dictionary<string, object>() : ConvertToObjectHelper.ConvertStrongTypeToObject(model);
                    if (modelAsObject is IDictionary<string, object>)
                    {
                        m.Item.Model.Content = modelAsObject;
                    }
                    else
                    {
                        Logger.LogWarning("Input model is not an Object model, it will be wrapped into an Object model. Please use --exportRawModel to view the wrapped model");
                        m.Item.Model.Content = new Dictionary<string, object>
                        {
                            ["model"] = modelAsObject
                        };
                    }
                }
            },
            _context.MaxParallelism);
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
                    if (m.Options?.Bookmarks != null)
                    {
                        foreach (var pair in m.Options.Bookmarks)
                        {
                            _context.RegisterInternalXrefSpecBookmark(pair.Key, pair.Value);
                        }
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

            var sharedObjects = new ConcurrentDictionary<string, object>();

            _manifestWithContext.RunAll(m =>
            {
                if (m.FileModel.Type != DocumentType.Article)
                {
                    return;
                }
                using (new LoggerFileScope(m.FileModel.LocalPathFromRoot))
                {
                    Logger.LogDiagnostic("Generating system metadata...");

                    // TODO: use weak type for system attributes from the beginning
                    var systemAttrs = systemMetadataGenerator.Generate(m.Item);
                    var metadata = (IDictionary<string, object>)ConvertToObjectHelper.ConvertStrongTypeToObject(systemAttrs);

                    var model = (IDictionary<string, object>)m.Item.Model.Content;

                    foreach (var pair in metadata)
                    {
                        if (!model.ContainsKey(pair.Key))
                        {
                            model[pair.Key] = pair.Value;
                        }
                    }

                    Logger.LogDiagnostic($"Load shared model from template for {m.Item.DocumentType}...");
                    if (m.Options?.IsShared == true)
                    {
                        // Take a snapshot of current model as shared object
                        sharedObjects[m.Item.Key] = new Dictionary<string, object>(model);
                    }
                    
                }
            },
            _context.MaxParallelism);

            _globalMetadata["_shared"] = sharedObjects;
        }

        private List<ManifestItem> ProcessTemplate()
        {
            // processor to add global variable to the model
            return _templateProcessor.Process(_manifestWithContext.Select(s => s.Item).ToList(), _context.ApplyTemplateSettings, _globalMetadata);
        }

        #endregion
    }
}
